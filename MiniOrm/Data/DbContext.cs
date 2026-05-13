using MiniOrm.Models;
using Npgsql;

namespace MiniOrm.Data
{
    public abstract class DbContext : IDisposable
    {
        private readonly string _connectionString;
        private NpgsqlConnection? _connection;

        protected DbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public NpgsqlConnection GetConnection()
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _connection = new NpgsqlConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        public class AppDbContext : DbContext
        {
            public DbSet<Product> Products { get; set; }
            public DbSet<Order> Orders { get; set; }

            public AppDbContext(string connectionString) : base(connectionString)
            {
                Products = new DbSet<Product>(this);
                Orders = new DbSet<Order>(this);
            }
        }
    }
}