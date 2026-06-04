using System.ComponentModel.DataAnnotations.Schema;

namespace FnD.Cloud.API.Models;

public class Order
{
    public int Id { get; set; } // Cloud Primary Key
    public int LocalOrderId { get; set; } // POS Client Key
    public string TenantId { get; set; } = string.Empty; // ◄ THE NEW SHIELD
    public DateTime OrderDate { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    public List<CloudOrderItem> Items { get; set; } = new();
}