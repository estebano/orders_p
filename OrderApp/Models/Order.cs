using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderApp.Models;

[Table("Orders")]
public class Order
{
    [Key]
    public System.Guid Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Column("CreatedAt")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Shipping status for processing lifecycle
    public ShippingStatus ShippingStatus { get; set; } = ShippingStatus.Pending;
    
    // Timestamp of the last update to the order (status changes etc.)
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}

public enum ShippingStatus
{
    Pending,
    Processing,
    Shipped,
    Failed
}
