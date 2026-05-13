using System.Reflection;

namespace MiniOrm.Data
{
    public class EntityMetadata
    {
        public string TableName { get; set; } = string.Empty;
        public PropertyInfo? PrimaryKey { get; set; }
        public List<ColumnInfo> Columns { get; set; } = new();
    }

    public class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public PropertyInfo Property { get; set; } = null!;
        public string PostgresType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
}