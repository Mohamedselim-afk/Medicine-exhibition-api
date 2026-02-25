using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medicine_exhibition_api.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(100)]
        public string? Dose { get; set; } // الجرعة

        [StringLength(1000)]
        public string? Notes { get; set; } // ملاحظات عند البيع

        [StringLength(200)]
        public string? LocationInStore { get; set; } // مكان المنتج في المحل

        [Column(TypeName = "varbinary(max)")]
        public byte[]? ImageData { get; set; } // صورة المنتج

        [StringLength(50)]
        public string? ImageContentType { get; set; }

        public int StockQuantity { get; set; } = 0;

        public DateTime? ExpiryDate { get; set; }

        public int? CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }
}

