using System;
using System.IO;
using System.Linq;
using FishingIdle.Game.Bootstrap;
using FishingIdle.GameService.Config;
using FishingIdle.GameService.Persistence;
using FishingIdle.Texts;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using T = FishingIdle.Texts.GameTexts.DevPanel;

namespace FishingIdle.Editor
{
    /// <summary>
    /// The local Development Panel: project status, roadmap, balance editing and save tools,
    /// inside the Unity Editor (menu Fishing Idle → Painel de Desenvolvimento).
    /// </summary>
    /// <remarks>
    /// It reflects repository files (docs/roadmap.json, version.json, /config) and the local save.
    /// It never shows "background activity": what it shows is what is committed and on disk.
    /// </remarks>
    public sealed class DevPanelWindow : EditorWindow
    {
        private static readonly string[] StatusFilterKeys = { null, "TODO", "IN_PROGRESS", "DONE", "BLOCKED", "NEEDS_OWNER_DECISION" };

        private int _tab;
        private Vector2 _scroll;
        private RoadmapDocument _roadmap;
        private RoadmapSummary _summary;
        private VersionDocument _version;
        private string _roadmapError;
        private string _configVersion;
        private string[] _configErrors = Array.Empty<string>();
        private int _statusFilter;
        private string _expandedTask;
        private readonly System.Collections.Generic.HashSet<string> _openMilestones = new System.Collections.Generic.HashSet<string>();
        private readonly BalanceEditor _balance = new BalanceEditor();
        private string _saveMessage;

        [MenuItem(T.MenuPath, priority = 0)]
        public static void Open()
        {
            var window = GetWindow<DevPanelWindow>();
            window.titleContent = new GUIContent(T.WindowTitle);
            window.minSize = new Vector2(620, 420);
            window.Show();
        }

        private void OnEnable()
        {
            Reload();
        }

        private void OnFocus()
        {
            // Pick up commits pulled or files edited while the window was in the background.
            if (!_balance.HasUnsavedChanges)
            {
                Reload();
            }
        }

        private void Reload()
        {
            _roadmap = RepositoryFiles.Read<RoadmapDocument>(RepositoryFiles.RoadmapPath, out _roadmapError);
            _summary = _roadmap != null ? RoadmapSummary.Build(_roadmap) : null;
            _version = RepositoryFiles.Read<VersionDocument>(RepositoryFiles.VersionPath, out _);

            var config = GameConfigLoader.LoadFromDirectory(GamePaths.RepositoryConfigDirectory);
            _configVersion = config.Succeeded ? config.Config.Version : null;
            _configErrors = config.Errors.ToArray();

            if (_roadmap != null && _openMilestones.Count == 0 && _roadmap.CurrentMilestone != null)
            {
                _openMilestones.Add(_roadmap.CurrentMilestone);
            }

            _balance.Load();
            Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _tab = GUILayout.Toolbar(_tab, new[] { T.TabOverview, T.TabRoadmap, T.TabBalance, T.TabSave }, EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(T.Reload, EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    Reload();
                }
            }

            EditorGUILayout.Space(6);

            if (_tab == 2)
            {
                // The balance tab manages its own scrolling so its action buttons stay visible.
                _balance.Draw();
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case 0: DrawOverview(); break;
                case 1: DrawRoadmap(); break;
                default: DrawSave(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ Visão geral

        private void DrawOverview()
        {
            if (_roadmap == null)
            {
                EditorGUILayout.HelpBox(T.RoadmapUnreadable + ": " + _roadmapError, MessageType.Error);
                return;
            }

            var s = _summary;
            EditorGUILayout.LabelField(_roadmap.Project + " — " + _roadmap.TargetVersion, EditorStyles.largeLabel);
            EditorGUILayout.Space(4);

            if (s.Current != null)
            {
                EditorGUILayout.LabelField(T.CurrentMilestone, s.Current.Id + " — " + s.Current.Title);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 18), s.Current.Tasks.Count == 0 ? 0 : s.Current.DoneCount / (float)s.Current.Tasks.Count,
                    T.MilestoneProgress(s.Current.DoneCount, s.Current.Tasks.Count));
            }

            EditorGUILayout.LabelField(T.Completion, T.CompletionValue(s.Done, s.Total, Format.Percent(s.Ratio, 1)));
            EditorGUILayout.LabelField(T.Pending, (s.Total - s.Done).ToString());
            EditorGUILayout.LabelField(T.GameVersion, _version?.Components != null && _version.Components.TryGetValue("client", out var client) ? client : "—");
            EditorGUILayout.LabelField(T.ConfigVersion, _configVersion ?? T.ConfigInvalid);
            EditorGUILayout.LabelField(T.RoadmapUpdatedAt, RepositoryFiles.FormatTimestamp(_roadmap.UpdatedAt));

            if (_configErrors.Length > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", _configErrors.Select(e => "• " + e)), MessageType.Error);
            }

            Section(T.NextStep);
            if (s.Next != null)
            {
                TaskLine(s.Next);
            }
            else
            {
                EditorGUILayout.LabelField("—");
            }

            Section(T.InProgress);
            if (s.InProgress.Count == 0)
            {
                EditorGUILayout.LabelField(T.NothingInProgress);
            }

            foreach (var task in s.InProgress)
            {
                TaskLine(task);
            }

            Section(T.Blockers);
            if (s.Blocked.Count == 0 && s.OpenDecisions.Count == 0)
            {
                EditorGUILayout.LabelField(T.NoBlockers);
            }

            foreach (var decision in s.OpenDecisions)
            {
                EditorGUILayout.HelpBox(decision.Id + " — " + decision.Title + "\n\n" + decision.Detail, MessageType.Warning);
            }

            foreach (var task in s.Blocked.Where(t => s.OpenDecisions.All(d => d.TaskId != t.Id)))
            {
                TaskLine(task);
            }

            Section(T.RecentlyDone);
            foreach (var task in s.RecentlyDone)
            {
                TaskLine(task);
            }
        }

        // ------------------------------------------------------------------ Roadmap

        private void DrawRoadmap()
        {
            if (_roadmap == null)
            {
                EditorGUILayout.HelpBox(T.RoadmapUnreadable + ": " + _roadmapError, MessageType.Error);
                return;
            }

            var labels = StatusFilterKeys.Select(k => k == null ? T.FilterAll : GameTexts.RoadmapStatus(k)).ToArray();
            _statusFilter = EditorGUILayout.Popup(_statusFilter, labels);
            var filter = StatusFilterKeys[_statusFilter];
            EditorGUILayout.Space(4);

            foreach (var milestone in _roadmap.Milestones)
            {
                var tasks = milestone.Tasks.Where(t => filter == null || t.Status == filter).ToList();
                if (filter != null && tasks.Count == 0)
                {
                    continue;
                }

                var open = _openMilestones.Contains(milestone.Id) || filter != null;
                var header = milestone.Id + " — " + milestone.Title + "   (" + T.MilestoneProgress(milestone.DoneCount, milestone.Tasks.Count) + ")";
                var nowOpen = EditorGUILayout.Foldout(open, header, true, EditorStyles.foldoutHeader);
                if (filter == null && nowOpen != open)
                {
                    if (nowOpen) _openMilestones.Add(milestone.Id);
                    else _openMilestones.Remove(milestone.Id);
                }

                if (!nowOpen)
                {
                    continue;
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(milestone.Goal, EditorStyles.wordWrappedMiniLabel);
                if (filter == null && milestone.CompletionCriteria.Count > 0)
                {
                    EditorGUILayout.LabelField(T.CompletionCriteria, EditorStyles.miniBoldLabel);
                    foreach (var criterion in milestone.CompletionCriteria)
                    {
                        EditorGUILayout.LabelField("• " + criterion, EditorStyles.wordWrappedMiniLabel);
                    }
                }

                foreach (var task in tasks)
                {
                    TaskLine(task, true);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(6);
            }
        }

        private void TaskLine(RoadmapTask task, bool expandable = false)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var previous = GUI.color;
                GUI.color = StatusColor(task.Status);
                GUILayout.Label(GameTexts.RoadmapStatus(task.Status), EditorStyles.miniButton, GUILayout.Width(150));
                GUI.color = previous;

                var text = task.Id + "  " + task.Title + "  ·  " + GameTexts.RoadmapSubsystem(task.Subsystem);
                if (expandable)
                {
                    if (GUILayout.Button(text, EditorStyles.label))
                    {
                        _expandedTask = _expandedTask == task.Id ? null : task.Id;
                    }
                }
                else
                {
                    GUILayout.Label(text);
                }
            }

            if (!expandable || _expandedTask != task.Id)
            {
                return;
            }

            EditorGUI.indentLevel += 2;
            EditorGUILayout.LabelField(task.Description, EditorStyles.wordWrappedLabel);
            if (task.Dependencies.Count > 0)
            {
                EditorGUILayout.LabelField(T.Dependencies + ": " + string.Join(", ", task.Dependencies), EditorStyles.wordWrappedMiniLabel);
            }

            if (!string.IsNullOrEmpty(task.CompletionNotes))
            {
                EditorGUILayout.LabelField(T.Notes + ": " + task.CompletionNotes, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.LabelField(RepositoryFiles.FormatTimestamp(task.UpdatedAt), EditorStyles.miniLabel);
            EditorGUI.indentLevel -= 2;
        }

        private static Color StatusColor(string status)
        {
            switch (status)
            {
                case "DONE": return new Color(0.55f, 0.9f, 0.6f);
                case "IN_PROGRESS": return new Color(0.55f, 0.8f, 1f);
                case "BLOCKED": return new Color(1f, 0.55f, 0.5f);
                case "NEEDS_OWNER_DECISION": return new Color(1f, 0.82f, 0.4f);
                default: return new Color(0.85f, 0.85f, 0.85f);
            }
        }

        // ------------------------------------------------------------------ Save

        private void DrawSave()
        {
            var directory = GamePaths.SaveDirectory;
            var path = Path.Combine(directory, JsonFilePlayerRepository.SaveFileName);

            EditorGUILayout.LabelField(T.SaveLocation, EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(directory, GUILayout.Height(18));
                if (GUILayout.Button(T.OpenFolder, GUILayout.Width(110)))
                {
                    Directory.CreateDirectory(directory);
                    EditorUtility.RevealInFinder(directory);
                }
            }

            Section(T.SaveSummary);
            if (!File.Exists(path))
            {
                EditorGUILayout.LabelField(T.SaveMissing, EditorStyles.wordWrappedLabel);
            }
            else
            {
                // Read-only peek: never goes through the repository, which would repair or move files.
                PlayerSave save = null;
                try
                {
                    save = JsonConvert.DeserializeObject<PlayerSave>(File.ReadAllText(path), JsonSettings.Default);
                }
                catch (Exception)
                {
                    EditorGUILayout.HelpBox(T.SaveUnreadable, MessageType.Warning);
                }

                if (save != null)
                {
                    EditorGUILayout.LabelField(T.SaveVersionLabel, save.SaveVersion.ToString());
                    EditorGUILayout.LabelField(GameTexts.Player.Level, save.FisherLevel.ToString());
                    EditorGUILayout.LabelField(GameTexts.Player.Coins, Format.Number(save.Coins));
                    EditorGUILayout.LabelField(GameTexts.Player.Shells, Format.Number(save.Shells));
                    EditorGUILayout.LabelField(GameTexts.Box.Title, GameTexts.Box.Count(save.FishingBox?.Count ?? 0));
                    EditorGUILayout.LabelField(GameTexts.Player.TotalCatches, Format.Number(save.Stats?.TotalCatches ?? 0));
                    EditorGUILayout.LabelField(T.SaveUpdatedAt, Format.DateTimeFromUnixMs(save.UpdatedAtMs));
                }
            }

            EditorGUILayout.Space(12);
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(T.PlayingNote, MessageType.Info);
            }

            var previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.55f);
            if (GUILayout.Button(T.ResetSave, GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(GameTexts.Dialogs.ResetTitle, GameTexts.Dialogs.ResetBody, GameTexts.Dialogs.ResetConfirm, GameTexts.Dialogs.Cancel))
                {
                    ResetSave(directory);
                }
            }

            GUI.backgroundColor = previous;

            if (!string.IsNullOrEmpty(_saveMessage))
            {
                EditorGUILayout.HelpBox(_saveMessage, MessageType.Info);
            }
        }

        private void ResetSave(string directory)
        {
            string kept;
            if (EditorApplication.isPlaying && GameRoot.Instance != null && GameRoot.Instance.IsRunning)
            {
                kept = GameRoot.Instance.ResetSave();
            }
            else
            {
                kept = new JsonFilePlayerRepository(directory).Reset();
            }

            _saveMessage = kept != null ? T.ResetDone + "\n" + kept : T.ResetNothing;
        }

        private static void Section(string title)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }
}
