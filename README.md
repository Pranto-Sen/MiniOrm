# MiniOrm — Build a Scratch ORM with ADO.NET

A simplified Entity Framework–style ORM built on raw **ADO.NET + Npgsql** against PostgreSQL.  
Implements reflection-based entity mapping, a generic `DbSet<T>` with full CRUD, and a command-line migration tool.

---

## Project Structure

```
MiniOrm.sln
├── MiniOrm/                        (Console Application — library + demo)
│   ├── Attributes/
│   │   ├── TableAttribute.cs       Maps class → PostgreSQL table name
│   │   ├── ColumnAttribute.cs      Maps property → column name
│   │   └── PrimaryKeyAttribute.cs  Marks the primary key property
│   ├── Models/
│   │   ├── Product.cs              Entity with nullable int? and string? fields
│   │   └── Order.cs                Second entity to prove DbSet<T> is generic
│   ├── Data/
│   │   ├── DbContext.cs            Opens NpgsqlConnection, exposes DbSet<T> properties
│   │   ├── DbSet.cs                Generic CRUD — GetAll, GetById, Insert, Update, Delete
│   │   ├── TypeMapper.cs           CLR → PostgreSQL DDL type mapping (handles Nullable<T>)
│   │   └── EntityMetadata.cs       Reflection cache: table name, columns, PK, nullability
│   └── Program.cs                  7-step demo walkthrough against a live database
│
└── MiniOrm.Migrations/             (Console Application — CLI)
    ├── Commands/
    │   └── MigrationRunner.cs      add / apply / list / rollback logic
    ├── migrations/                 SQL migration files (NNN_description.sql)
    │   ├── 001_create_products.sql
    │   ├── 001_create_products_rollback.sql
    │   ├── 002_create_orders.sql
    │   └── 002_create_orders_rollback.sql
    └── Program.cs                  CLI entry point
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- A running PostgreSQL instance (local or remote)
- Only **Npgsql** is used as a third-party NuGet package (no EF Core, no Dapper)

---

## PostgreSQL Setup

```sql
-- Connect as superuser (e.g. psql -U postgres) and run:
CREATE DATABASE miniorm_db;
-- Or use an existing database — MiniOrm creates its own tables.
```

---

## Setting MINIORM_CONN

Both projects read the connection string from the `MINIORM_CONN` environment variable.

**Linux / macOS**
```bash
export MINIORM_CONN="Host=localhost;Database=miniorm_db;Username=postgres;Password=yourpassword"
```

**Windows (PowerShell)**
```powershell
$env:MINIORM_CONN = "Host=localhost;Database=miniorm_db;Username=postgres;Password=yourpassword"
```

---

## Running the Demo (MiniOrm)

The `Program.cs` demo walks through all 5 tasks end-to-end:

```bash
cd MiniOrm
dotnet run
```

**Expected output (abbreviated):**
```
═══════════════════════════════════════════════════════
  MiniOrm — ADO.NET ORM Demo Walkthrough
═══════════════════════════════════════════════════════

[DbContext] Connected to: localhost/miniorm_db
── Step 1 — Create tables via reflection-generated DDL
── Step 2 — Insert Products (exercises nullable fields)
── Step 3 — GetAll() Products
── Step 4 — GetById()
── Step 5 — Update
── Step 6 — Insert Orders (second entity)
── Step 7 — Delete and verify
── Cleanup — Drop tables so the demo can be re-run
```

---

## Running Migrations (MiniOrm.Migrations)

The migration CLI uses a `__migrations` tracking table in your database.

```bash
cd MiniOrm.Migrations

# Create a new blank migration file
dotnet run -- add create_products_table

# Apply all pending migrations
dotnet run -- apply

# List all migrations and their status
dotnet run -- list

# Roll back the last migration
dotnet run -- rollback

# Roll back the last 2 migrations
dotnet run -- rollback 2
```

### Migration file naming

Files live in `MiniOrm.Migrations/migrations/` and follow the pattern:

```
NNN_description.sql              — forward migration
NNN_description_rollback.sql     — optional rollback script
```

Example:
```
001_create_products.sql
001_create_products_rollback.sql
002_create_orders.sql
002_create_orders_rollback.sql
```

---

## Type Mapping

`TypeMapper.cs` handles the CLR → PostgreSQL type conversion:

| C# Type       | PostgreSQL Type   | Notes                          |
|---------------|-------------------|--------------------------------|
| `int`         | `INTEGER`         |                                |
| `int?`        | `INTEGER`         | Column is nullable             |
| `long`        | `BIGINT`          |                                |
| `decimal`     | `NUMERIC(18,4)`   |                                |
| `bool`        | `BOOLEAN`         |                                |
| `string`      | `TEXT`            | NOT NULL                       |
| `string?`     | `TEXT`            | Nullable via NullabilityInfoContext |
| `DateTime`    | `TIMESTAMP`       | NOT NULL                       |
| `DateTime?`   | `TIMESTAMP`       | Nullable                       |
| `Guid`        | `UUID`            |                                |
| `byte[]`      | `BYTEA`           |                                |

### Nullability detection

`EntityMetadata` uses two mechanisms:

1. **`Nullable.GetUnderlyingType()`** — detects `Nullable<T>` value types (`int?`, `DateTime?`)  
2. **`NullabilityInfoContext`** (.NET 6+) — detects nullable reference types (`string?`) from the NRT annotations in the compiled assembly

Only properties decorated with `[Column]` are mapped. Properties without `[Column]` are silently ignored, which means you can freely add navigation properties, computed properties, etc. without confusing the ORM.

---

## Attribute Filtering

The ORM only processes properties that carry **both** `[Column]` and (for the PK) `[PrimaryKey]` attributes.  
This design matches the assignment requirement: *"attribute-only filtering"* — the mapper never falls back to convention-based discovery.

```csharp
[Table("products")]
public class Product
{
    [PrimaryKey]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("reorder_level")]
    public int? ReorderLevel { get; set; }   // maps to nullable INTEGER

    [Column("description")]
    public string? Description { get; set; } // maps to nullable TEXT

    // No [Column] → completely ignored by the ORM
    public string DisplayLabel => $"{Name} (${Price})";
}
```

---

## Restrictions

- Only **Npgsql** is used as a third-party NuGet package.  
- EF Core, Dapper, or any other ORM = zero marks per assignment rules.

---

## Submission

GitHub repository submitted via the course portal.  
Deadline: **13 May 2026**
