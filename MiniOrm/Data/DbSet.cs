using System.Data;
using Npgsql;

namespace MiniOrm.Data
{
    public class DbSet<T> where T : class, new()
    {
        private readonly DbContext _context;
        private readonly EntityMetadata _metadata;

        public DbSet(DbContext context)
        {
            _context = context;
            _metadata = TypeMapper.GetMetadata<T>();
        }

        public int Insert(T entity)
        {
            var columns = _metadata.Columns.Where(c => !c.IsPrimaryKey).ToList();

            var columnNames = string.Join(", ", columns.Select(c => c.ColumnName));
            var paramNames = string.Join(", ", columns.Select(c => $"@{c.ColumnName}"));

            var sql = $"INSERT INTO {_metadata.TableName} ({columnNames}) VALUES ({paramNames})";

            if (_metadata.PrimaryKey != null)
                sql += $" RETURNING {_metadata.Columns.First(c => c.IsPrimaryKey).ColumnName};";

            using var cmd = new NpgsqlCommand(sql, _context.GetConnection());

            foreach (var col in columns)
            {
                var value = col.Property.GetValue(entity);
                cmd.Parameters.AddWithValue($"@{col.ColumnName}", value ?? DBNull.Value);
            }

            var result = cmd.ExecuteScalar();
            if (result != null && _metadata.PrimaryKey != null)
                _metadata.PrimaryKey.SetValue(entity, Convert.ToInt32(result));

            return Convert.ToInt32(result);
        }

        public T? FindById(int id)
        {
            var pk = _metadata.Columns.First(c => c.IsPrimaryKey);
            var sql = $"SELECT * FROM {_metadata.TableName} WHERE {pk.ColumnName} = @id";

            using var cmd = new NpgsqlCommand(sql, _context.GetConnection());
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapToEntity(reader) : null;
        }

        public List<T> GetAll()
        {
            var sql = $"SELECT * FROM {_metadata.TableName}";
            using var cmd = new NpgsqlCommand(sql, _context.GetConnection());
            using var reader = cmd.ExecuteReader();

            var list = new List<T>();
            while (reader.Read())
                list.Add(MapToEntity(reader));

            return list;
        }

        public void Update(T entity)
        {
            var pk = _metadata.Columns.First(c => c.IsPrimaryKey);
            var pkValue = pk.Property.GetValue(entity);

            var setParts = _metadata.Columns
                .Where(c => !c.IsPrimaryKey)
                .Select(c => $"{c.ColumnName} = @{c.ColumnName}");

            var sql = $"UPDATE {_metadata.TableName} SET {string.Join(", ", setParts)} WHERE {pk.ColumnName} = @pk";

            using var cmd = new NpgsqlCommand(sql, _context.GetConnection());
            cmd.Parameters.AddWithValue("@pk", pkValue!);

            foreach (var col in _metadata.Columns.Where(c => !c.IsPrimaryKey))
            {
                var value = col.Property.GetValue(entity);
                cmd.Parameters.AddWithValue($"@{col.ColumnName}", value ?? DBNull.Value);
            }

            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            var pk = _metadata.Columns.First(c => c.IsPrimaryKey);
            var sql = $"DELETE FROM {_metadata.TableName} WHERE {pk.ColumnName} = @id";

            using var cmd = new NpgsqlCommand(sql, _context.GetConnection());
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private T MapToEntity(NpgsqlDataReader reader)
        {
            var entity = new T();
            foreach (var col in _metadata.Columns)
            {
                int ordinal = reader.GetOrdinal(col.ColumnName);
                if (reader.IsDBNull(ordinal))
                {
                    if (col.IsNullable)
                        col.Property.SetValue(entity, null);
                }
                else
                {
                    var value = reader.GetValue(ordinal);
                    var targetType = Nullable.GetUnderlyingType(col.Property.PropertyType) ?? col.Property.PropertyType;
                    col.Property.SetValue(entity, Convert.ChangeType(value, targetType));
                }
            }
            return entity;
        }
    }
}