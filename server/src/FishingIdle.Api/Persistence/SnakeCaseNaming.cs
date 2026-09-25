using System.Text;
using Microsoft.EntityFrameworkCore;

namespace FishingIdle.Api.Persistence;

/// <summary>
/// Applies PostgreSQL-idiomatic <c>snake_case</c> names to every column, key and index
/// so hand-written SQL and psql output stay readable.
/// </summary>
internal static class SnakeCaseNaming
{
    internal static void ApplySnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                // An explicit HasColumnName wins; only convention-derived names are rewritten.
                if (property.GetColumnName() == property.Name)
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()!));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
            }
        }
    }

    internal static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (char.IsUpper(current))
            {
                var previous = i > 0 ? name[i - 1] : '\0';
                var needsSeparator = i > 0
                    && previous != '_'
                    && (!char.IsUpper(previous) || (i + 1 < name.Length && char.IsLower(name[i + 1])));

                if (needsSeparator)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}
