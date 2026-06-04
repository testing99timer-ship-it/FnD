namespace FnD.LocalPOS.Client.Models;

// Represents the localized item stock configuration inside the register
public class LocalCatalogItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Category { get; set; } = string.Empty;
}

// Manages item modifications inside the active checkout cart session
public class ActiveCartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal => Quantity * UnitPrice;
}

// Mirrored serialization wrapper to transmit payload safely to CloudOrder endpoints
public class OutboundSyncOrder
{
    public int LocalOrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OutboundSyncOrderItem> Items { get; set; } = new();
}

public class OutboundSyncOrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}