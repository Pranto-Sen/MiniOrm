using MiniOrm.Attributes;

namespace MiniOrm.Models
{
    [Table("orders")]
    public class Order
    {
        [PrimaryKey]
        [Column("id")]
        public int Id { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; }

        [Column("customer_name")]
        public string? CustomerName { get; set; }  
    }
}