using System.ComponentModel.DataAnnotations.Schema;

namespace FnD.Cloud.API.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    public string? TenantId { get; internal set; }
}