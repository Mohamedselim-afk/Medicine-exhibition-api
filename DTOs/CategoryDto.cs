using System.ComponentModel.DataAnnotations;

namespace Medicine_exhibition_api.DTOs
{
    public class CategoryItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
