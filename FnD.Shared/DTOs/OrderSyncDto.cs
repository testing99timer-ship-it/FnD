using System;
using System.Collections.Generic;
using System.Text;

namespace FnD.Shared.DTOs
{
    public class OrderSyncDto
    {
        public int LocalOrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }

        // A list of the items bought in this order
        public List<OrderItemSyncDto> Items { get; set; } = new();
    }

    public class OrderItemSyncDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
