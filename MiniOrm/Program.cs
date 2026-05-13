using MiniOrm;
using MiniOrm.Data;
using MiniOrm.Models;
using static MiniOrm.Data.DbContext;

class Program
{
    static void Main(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("MINIORM_CONN");
        if (string.IsNullOrEmpty(connStr))
        {
            Console.WriteLine("MINIORM_CONN environment variable is not set!");
            return;
        }

        using var db = new AppDbContext(connStr);


        // Insert
        var product = new Product
        {
            Name = "Gaming Mouse",
            Price = 49.99m,
            Discount = null,
            InStock = true
        };
        int id = db.Products.Insert(product);
        Console.WriteLine($"Inserted Product - ID: {id}, Discount: {product.Discount?.ToString() ?? "NULL"}");


        //// Find
        //var found = db.Products.FindById(1);
        //if(found != null)
        //{
        //    Console.WriteLine($"Found Product: {found?.Name}, Price: {found?.Price}, Discount: {found?.Discount?.ToString() ?? "NULL"}");
        //}
        //else
        //{
        //    Console.WriteLine("No Product Found");
        //}


        //// Update
        //if (found != null)
        //{
        //    found.Price = 44.99m;
        //    found.Discount = 2.2m;
        //    db.Products.Update(found);
        //    Console.WriteLine($"Product Updated : Price: {found?.Price}, Discount: {found?.Discount?.ToString() ?? "NULL"}");
        //}


        // Get All
        var all = db.Products.GetAll();
        Console.WriteLine($"Total Products: {all.Count}");
        foreach (var p in all)
        {
            Console.WriteLine(
                $"ID: {p.Id}, Name: {p.Name}, Price: {p.Price}, Discount: {p.Discount?.ToString() ?? "NULL"}, InStock: {p.InStock}"
            );
        }


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
      
    }
}