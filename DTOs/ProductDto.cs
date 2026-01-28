using System.ComponentModel.DataAnnotations;

namespace Medicine_exhibition_api.DTOs
{
    public class CreateProductDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [StringLength(100)]
        public string? Dose { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(200)]
        public string? LocationInStore { get; set; }

        public IFormFile? Image { get; set; }

        public int StockQuantity { get; set; } = 0;
    }

    public class UpdateProductDto
    {
        [StringLength(200)]
        public string? Name { get; set; }

        public decimal? Price { get; set; }

        [StringLength(100)]
        public string? Dose { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(200)]
        public string? LocationInStore { get; set; }

        public IFormFile? Image { get; set; }

        public int? StockQuantity { get; set; }
    }

    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Dose { get; set; }
        public string? Notes { get; set; }
        public string? LocationInStore { get; set; }
        public string? ImageBase64 { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

