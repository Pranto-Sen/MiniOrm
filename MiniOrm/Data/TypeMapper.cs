using System.Reflection;
using MiniOrm.Attributes;

namespace MiniOrm.Data
{
    public static class TypeMapper
    {
        public static EntityMetadata GetMetadata<T>()
        {
            var type = typeof(T);
            var metadata = new EntityMetadata();

            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            metadata.TableName = tableAttr?.Name ?? type.Name.ToLower() + "s";

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                var pkAttr = prop.GetCustomAttribute<PrimaryKeyAttribute>();

                if (columnAttr == null && pkAttr == null)
                    continue;

                var colInfo = new ColumnInfo
                {
                    Property = prop,
                    ColumnName = columnAttr?.Name ?? prop.Name.ToLower(),
                    IsPrimaryKey = pkAttr != null,
                    IsNullable = IsNullableType(prop.PropertyType),
                    PostgresType = GetPostgresType(prop.PropertyType)
                };

                if (colInfo.IsPrimaryKey)
                    metadata.PrimaryKey = prop;

                metadata.Columns.Add(colInfo);
            }

            return metadata;
        }

        private static bool IsNullableType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return true;
            return !type.IsValueType;
        }

        public static string GetPostgresType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(int)) return "INTEGER";
            if (type == typeof(long)) return "BIGINT";
            if (type == typeof(float)) return "REAL";
            if (type == typeof(double)) return "DOUBLE PRECISION";
            if (type == typeof(decimal)) return "NUMERIC";
            if (type == typeof(bool)) return "BOOLEAN";
            if (type == typeof(DateTime)) return "TIMESTAMP";
            if (type == typeof(Guid)) return "UUID";
            if (type == typeof(string)) return "TEXT";

            return "TEXT";
        }

        public static bool IsNullableType(Type type, out bool isNullable)
        {
            isNullable = IsNullableType(type);
            return isNullable;
        }
    }
}