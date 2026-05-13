using MiniOrm.Migrations.Commands;

class Program
{
    static void Main(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("MINIORM_CONN");
        if (string.IsNullOrEmpty(connStr))
        {
            Console.WriteLine("Please set MINIORM_CONN environment variable.");
            return;
        }

        var remaining = args.ToList();
        if (remaining.Count > 0 && remaining[0].ToLower() == "migrations")
            remaining.RemoveAt(0);

        MigrationRunner.Run(remaining.ToArray(), connStr);
    }
}