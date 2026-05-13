# MiniOrm — Build a Lightweight ORM with ADO.NET

A lightweight **Entity Framework–style ORM** built from scratch using **ADO.NET + Npgsql** with **PostgreSQL**.

The project demonstrates:

- Raw ADO.NET database interaction
- Reflection-based entity mapping
- Attribute-driven table/column configuration
- Migration system
- CRUD operations
- CLR → PostgreSQL type mapping

---

# Features

- Lightweight ORM architecture
- PostgreSQL support via `Npgsql`
- Attribute-only entity mapping
- Automatic SQL generation
- Migration system with:
  - Add migration
  - Apply migration
  - Rollback migration
  - List migrations
- Nullable type support
- Repository-style CRUD operations

---

# Project Structure

```text
MiniOrm/
│
├── MiniOrm/                # Main ORM project
├── MiniOrm.Migrations/     # Migration runner project
└── README.md
```

---

# PostgreSQL Setup

Both projects read the database connection string from the `MINIORM_CONN` environment variable.

## Bash

```bash
export MINIORM_CONN="Host=localhost;Database=db_name;Username=user_name;Password=yourpassword"
```

## PowerShell

```powershell
$env:MINIORM_CONN = "Host=localhost;Database=db_name;Username=user_name;Password=yourpassword"
```

---

# Run Migrations

Navigate to the migration project:

```bash
cd MiniOrm.Migrations
```

## Create Initial Migration

```bash
dotnet run -- migrations add InitialCreate
```

## Apply Migrations

```bash
dotnet run -- migrations apply
```

---

# Migration Commands

Inside `MiniOrm.Migrations`:

## List All Migrations

```bash
dotnet run -- list
```

## Roll Back Last Migration

```bash
dotnet run -- rollback
```

---

# Run the Main Project

```bash
cd ../MiniOrm
dotnet run
```

---

# Expected Output

After running the application:

- Sample data will be inserted into the database
- All inserted data will be displayed in the console

Example test sections are already included in `Program.cs` for:

- Find by ID
- Update
- Delete

These sections are intentionally commented out.

Uncomment them one at a time and provide a valid ID to test specific operations.

Example:

```csharp
// var found = db.Products.FindById(1);

// if (found != null)
// {
//     Console.WriteLine(
//         $"Found Product: {found.Name}, " +
//         $"Price: {found.Price}, " +
//         $"Discount: {found.Discount?.ToString() ?? "NULL"}"
//     );
// }
// else
// {
//     Console.WriteLine("No Product Found");
// }
```

The same approach applies to:

- Update operations
- Delete operations

```csharp
 //// Update
 //if (found != null)
 //{
 //    found.Price = 44.99m;
 //    found.Discount = 2.2m;
 //    db.Products.Update(found);
 //    Console.WriteLine($"Product Updated : Price: {found?.Price}, Discount: {found?.Discount?.ToString() ?? "NULL"}");
 //}

 //// Delete
 //int id = 1;
 //var exist = db.Products.FindById(id);
 //if (exist != null)
 //{
 //    db.Products.Delete(id);
 //    var remaining = db.Products.GetAll().Count;
 //    Console.WriteLine($"Deleted Id = {id}, {remaining} products remaining");
 //}
 //else
 //{
 //    Console.WriteLine("No Product Found");
 //}
```

---

# Type Mapping

`TypeMapper.cs` handles CLR → PostgreSQL type conversion.

| C# Type | PostgreSQL Type | Notes |
|---|---|---|
| `int` | `INTEGER` | Non-nullable |
| `int?` | `INTEGER` | Nullable |
| `long` | `BIGINT` | |
| `decimal` | `NUMERIC(18,4)` | |
| `bool` | `BOOLEAN` | |
| `string` | `TEXT` | NOT NULL |
| `string?` | `TEXT` | Nullable |
| `DateTime` | `TIMESTAMP` | NOT NULL |
| `DateTime?` | `TIMESTAMP` | Nullable |
| `Guid` | `UUID` | |
| `byte[]` | `BYTEA` | |

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
