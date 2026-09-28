using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FishingIdle.Texts;
using Newtonsoft.Json;

namespace FishingIdle.GameService.Config
{
    /// <summary>The outcome of loading the balance files: a usable config, or the reasons it is not.</summary>
    public sealed class ConfigLoadResult
    {
        internal ConfigLoadResult(GameConfig config, IReadOnlyList<string> errors)
        {
            Config = config;
            Errors = errors;
        }

        public GameConfig Config { get; }

        /// <summary>Every problem found, in PT-BR. Empty when <see cref="Succeeded"/>.</summary>
        public IReadOnlyList<string> Errors { get; }

        public bool Succeeded => Config != null && Errors.Count == 0;
    }

    /// <summary>
    /// Reads the balance files from /config, validates them, and builds a <see cref="GameConfig"/>.
    /// </summary>
    /// <remarks>
    /// Invalid balance never reaches the game (GDD section 5, "Invalid values must not be
    /// deployable"): if any file fails to parse or validate, no config is produced at all.
    /// </remarks>
    public static class GameConfigLoader
    {
        public const string FishCatalogFile = "fish_catalog.json";
        public const string MapsFile = "maps.json";
        public const string ProgressionFile = "progression.json";
        public const string RodsFile = "rods.json";
        public const string EconomyFile = "economy.json";
        public const string ArenaFile = "arena.json";

        /// <summary>The files the implemented milestones need, in a stable order.</summary>
        public static readonly IReadOnlyList<string> RequiredFiles = new[]
        {
            FishCatalogFile, MapsFile, ProgressionFile, RodsFile, EconomyFile, ArenaFile,
        };

        public static ConfigLoadResult LoadFromDirectory(string directory)
        {
            var texts = new Dictionary<string, string>(StringComparer.Ordinal);
            var errors = new List<string>();

            if (!Directory.Exists(directory))
            {
                errors.Add(GameTexts.Validation.ConfigFolderMissing(directory));
                return new ConfigLoadResult(null, errors);
            }

            foreach (var file in RequiredFiles)
            {
                var path = Path.Combine(directory, file);
                if (!File.Exists(path))
                {
                    errors.Add(GameTexts.Validation.ConfigFileMissing(file));
                    continue;
                }

                try
                {
                    texts[file] = File.ReadAllText(path, Encoding.UTF8);
                }
                catch (IOException exception)
                {
                    errors.Add(GameTexts.Validation.ConfigFileUnreadable(file, exception.Message));
                }
            }

            if (errors.Count > 0)
            {
                return new ConfigLoadResult(null, errors);
            }

            return LoadFromTexts(texts);
        }

        /// <summary>
        /// Builds a config from file contents keyed by file name. The Dev Panel uses this to validate
        /// an edit in memory before writing anything to disk.
        /// </summary>
        public static ConfigLoadResult LoadFromTexts(IReadOnlyDictionary<string, string> texts)
        {
            var errors = new List<string>();

            var fishCatalog = Parse<FishCatalogConfig>(texts, FishCatalogFile, errors);
            var maps = Parse<MapsConfig>(texts, MapsFile, errors);
            var progression = Parse<ProgressionConfig>(texts, ProgressionFile, errors);
            var rods = Parse<RodsConfig>(texts, RodsFile, errors);
            var economy = Parse<EconomyConfig>(texts, EconomyFile, errors);
            var arena = Parse<ArenaConfig>(texts, ArenaFile, errors);

            if (errors.Count > 0)
            {
                return new ConfigLoadResult(null, errors);
            }

            errors.AddRange(GameConfigValidator.Validate(fishCatalog, maps, progression, rods, economy, arena));
            if (errors.Count > 0)
            {
                return new ConfigLoadResult(null, errors);
            }

            var config = new GameConfig(fishCatalog, maps, progression, rods, economy, arena, Fingerprint(texts));
            return new ConfigLoadResult(config, errors);
        }

        private static T Parse<T>(IReadOnlyDictionary<string, string> texts, string file, List<string> errors)
            where T : class
        {
            if (!texts.TryGetValue(file, out var json) || string.IsNullOrWhiteSpace(json))
            {
                errors.Add(GameTexts.Validation.ConfigFileMissing(file));
                return null;
            }

            try
            {
                var value = JsonConvert.DeserializeObject<T>(json, JsonSettings.Default);
                if (value == null)
                {
                    errors.Add(GameTexts.Validation.ConfigFileEmpty(file));
                }

                return value;
            }
            catch (JsonException exception)
            {
                errors.Add(GameTexts.Validation.ConfigFileInvalidJson(file, exception.Message));
                return null;
            }
        }

        private static string Fingerprint(IReadOnlyDictionary<string, string> texts)
        {
            var builder = new StringBuilder();
            foreach (var file in RequiredFiles)
            {
                builder.Append(file).Append('\n');
                // Line endings differ between checkouts on Windows and Linux; they are not balance.
                builder.Append(texts[file].Replace("\r\n", "\n")).Append('\n');
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                return string.Concat(hash.Take(5).Select(b => b.ToString("x2")));
            }
        }
    }
}
