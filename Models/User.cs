using System.ComponentModel.DataAnnotations;

namespace Medicine_exhibition_api.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "Employee"; // Owner or Employee

        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(30)]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? FcmToken { get; set; } // For push notifications

        [StringLength(500)]
        public string? ResetToken { get; set; } // For password reset

        public DateTime? ResetTokenExpiry { get; set; } // Reset token expiration

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}

