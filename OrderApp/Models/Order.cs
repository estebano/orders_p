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
}
