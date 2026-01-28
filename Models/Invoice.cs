using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medicine_exhibition_api.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty; // اسم المشتري

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int CreatedByUserId { get; set; } // الموظف الذي أنشأ الفاتورة

        public bool IsConfirmed { get; set; } = false; // تم التأكيد من الموظف

        public bool IsViewedByOwner { get; set; } = false; // تم الاطلاع من صاحب المحل

        public bool IsDeletedByOwner { get; set; } = false; // حذفها صاحب المحل (Soft Delete)

        public DateTime? DeletedByOwnerAt { get; set; }

        // Navigation properties
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; } = null!;

        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }
}

