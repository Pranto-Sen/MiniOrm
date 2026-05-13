using Npgsql;
using MiniOrm.Data;
using MiniOrm.Models;
using System.Reflection;
using static MiniOrm.Data.DbContext;

namespace MiniOrm.Migrations.Commands
{
    public static class MigrationRunner
    {
        private static readonly string MigrationFolder = "Migrations";

        public static void Run(string[] args, string connStr)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: migrations [add <name> | apply | list | rollback]");
                return;
            }

            switch (args[0].ToLower())
            {
                case "add":
                    if (args.Length < 2) { Console.WriteLine("Usage: migrations add <Name>"); return; }
                    CreateMigrationFile(args[1]);
                    break;
                case "apply":
                    ApplyMigrations(connStr);
                    break;
                case "list":
                    ListMigrations(connStr);
                    break;
                case "rollback":
                    RollbackLastMigration(connStr);
                    break;
                default:
                    Console.WriteLine("Unknown command. Use: add | apply | list | rollback");
                    break;
            }
        }

        private static void CreateMigrationFile(string name)
        {
            Directory.CreateDirectory(MigrationFolder);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var fileName = $"{timestamp}_{name}.sql";
            var filePath = Path.Combine(MigrationFolder, fileName);

            var up = new System.Text.StringBuilder();
            var down = new System.Text.StringBuilder();

            up.AppendLine("-- up");
            down.AppendLine("-- down");

            foreach (var entityType in GetEntityTypes())
            {
                var meta = GetMetadata(entityType);
                if (meta == null) continue;

                up.AppendLine();
                up.AppendLine($"CREATE TABLE IF NOT EXISTS {meta.TableName} (");

                var cols = new List<string>();
                foreach (var col in meta.Columns)
                {
                    if (col.IsPrimaryKey)
                        cols.Add($"    {col.ColumnName} SERIAL PRIMARY KEY");
                    else
                        cols.Add($"    {col.ColumnName} {col.PostgresType} {(col.IsNullable ? "NULL" : "NOT NULL")}");
                }

                up.AppendLine(string.Join(",\n", cols));
                up.AppendLine(");");

                down.AppendLine($"DROP TABLE IF EXISTS {meta.TableName};");
            }

            up.AppendLine();
            up.AppendLine("CREATE TABLE IF NOT EXISTS __migrations (");
            up.AppendLine("    id         SERIAL PRIMARY KEY,");
            up.AppendLine("    filename   TEXT UNIQUE NOT NULL,");
            up.AppendLine("    applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP");
            up.AppendLine(");");
            down.AppendLine("DROP TABLE IF EXISTS __migrations;");

            File.WriteAllText(filePath, up.ToString() + "\n" + down.ToString());

            Console.WriteLine($"Migration created: {fileName}");
            Console.WriteLine();
            Console.WriteLine(up.ToString());
        }

        private static List<Type> GetEntityTypes()
        {
            var dbSetGeneric = typeof(DbSet<>);
            return typeof(AppDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == dbSetGeneric)
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                .ToList();
        }

        private static EntityMetadata? GetMetadata(Type entityType)
        {
            return typeof(TypeMapper)
                .GetMethod("GetMetadata", BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(entityType)
                .Invoke(null, null) as EntityMetadata;
        }


        private static void ApplyMigrations(string connStr)
        {
            if (!Directory.Exists(MigrationFolder))
            {
                Console.WriteLine("No Migrations folder. Run: migrations add <Name>");
                return;
            }

            using var conn = new NpgsqlConnection(connStr);
            conn.Open();
            EnsureMigrationsTable(conn);

            var applied = GetApplied(conn);
            var files = Directory.GetFiles(MigrationFolder, "*.sql").OrderBy(f => f);
            int count = 0;

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (applied.Contains(fileName))
                {
                    Console.WriteLine($"Already applied: {fileName}");
                    continue;
                }

                Console.WriteLine($"Applying: {fileName}");
                var upSql = ExtractSection(File.ReadAllText(file), "up", "down");

                if (!string.IsNullOrWhiteSpace(upSql))
                {
                    using var cmd = new NpgsqlCommand(upSql, conn);
                    cmd.ExecuteNonQuery();
                }

                MarkApplied(conn, fileName);
                Console.WriteLine("Done");
                count++;
            }

            Console.WriteLine(count == 0 ? "\nNothing new to apply." : $"\n{count} migration(s) applied!");
        }


        private static void ListMigrations(string connStr)
        {
            if (!Directory.Exists(MigrationFolder)) { Console.WriteLine("No migrations yet."); return; }

            using var conn = new NpgsqlConnection(connStr);
            conn.Open();
            EnsureMigrationsTable(conn);

            var applied = GetApplied(conn);
            var files = Directory.GetFiles(MigrationFolder, "*.sql").OrderBy(f => f).ToList();

            Console.WriteLine($"\nMigrations ({files.Count} total):");
            foreach (var file in files)
            {
                var fn = Path.GetFileName(file);
                Console.WriteLine($"  {(applied.Contains(fn) ? "[APPLIED]" : "[PENDING]")}  {fn}");
            }
        }


        private static void RollbackLastMigration(string connStr)
        {
            using var conn = new NpgsqlConnection(connStr);
            conn.Open();
            EnsureMigrationsTable(conn);

            var last = GetLastApplied(conn);
            if (string.IsNullOrEmpty(last)) { Console.WriteLine("Nothing to rollback."); return; }

            var filePath = Directory.GetFiles(MigrationFolder, "*.sql")
                .FirstOrDefault(f => Path.GetFileName(f) == last);

            if (filePath == null) { Console.WriteLine($"File not found: {last}"); return; }

            Console.WriteLine($"Rolling back: {last}");
            var downSql = ExtractSection(File.ReadAllText(filePath), "down", null);

            if (!string.IsNullOrWhiteSpace(downSql))
            {
                using var cmd = new NpgsqlCommand(downSql, conn);
                cmd.ExecuteNonQuery();
            }

            RemoveApplied(conn, last);
            Console.WriteLine("Rollback successful.");
        }


        private static void EnsureMigrationsTable(NpgsqlConnection conn)
        {
            using var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS __migrations (
                    id SERIAL PRIMARY KEY,
                    filename TEXT UNIQUE NOT NULL,
                    applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );", conn);
            cmd.ExecuteNonQuery();
        }

        private static HashSet<string> GetApplied(NpgsqlConnection conn)
        {
            var set = new HashSet<string>();
            using var cmd = new NpgsqlCommand("SELECT filename FROM __migrations", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) set.Add(reader.GetString(0));
            return set;
        }

        private static string GetLastApplied(NpgsqlConnection conn)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT filename FROM __migrations ORDER BY id DESC LIMIT 1", conn);
            return cmd.ExecuteScalar()?.ToString() ?? "";
        }

        private static void MarkApplied(NpgsqlConnection conn, string filename)
        {
            using var cmd = new NpgsqlCommand("INSERT INTO __migrations (filename) VALUES (@f)", conn);
            cmd.Parameters.AddWithValue("@f", filename);
            cmd.ExecuteNonQuery();
        }

        private static void RemoveApplied(NpgsqlConnection conn, string filename)
        {
            using var cmd = new NpgsqlCommand("DELETE FROM __migrations WHERE filename = @f", conn);
            cmd.Parameters.AddWithValue("@f", filename);
            cmd.ExecuteNonQuery();
        }

        private static string ExtractSection(string sql, string start, string? end)
        {
            var result = new System.Text.StringBuilder();
            bool inside = false;

            foreach (var line in sql.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Equals($"-- {start}", StringComparison.OrdinalIgnoreCase)) { inside = true; continue; }
                if (inside && end != null && trimmed.Equals($"-- {end}", StringComparison.OrdinalIgnoreCase)) break;
                if (inside) result.AppendLine(line);
            }

            return result.ToString().Trim();
        }
    }
}