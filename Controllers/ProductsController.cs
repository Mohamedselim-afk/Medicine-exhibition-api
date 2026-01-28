using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Medicine_exhibition_api.Data;
using Medicine_exhibition_api.DTOs;
using Medicine_exhibition_api.Models;

namespace Medicine_exhibition_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProducts([FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Products.Where(p => p.IsActive).AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Name.Contains(search));
                }

                var products = await query
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var result = products.Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Dose = p.Dose,
                    Notes = p.Notes,
                    LocationInStore = p.LocationInStore,
                    ImageBase64 = p.ImageData != null ? Convert.ToBase64String(p.ImageData) : null,
                    StockQuantity = p.StockQuantity,
                    CreatedAt = p.CreatedAt
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب المنتجات" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null || !product.IsActive)
                {
                    return NotFound(new { message = "المنتج غير موجود" });
                }

                var result = new ProductResponseDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Dose = product.Dose,
                    Notes = product.Notes,
                    LocationInStore = product.LocationInStore,
                    ImageBase64 = product.ImageData != null ? Convert.ToBase64String(product.ImageData) : null,
                    StockQuantity = product.StockQuantity,
                    CreatedAt = product.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب المنتج" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
        {
            try
            {
                byte[]? imageData = null;
                string? imageContentType = null;

                if (dto.Image != null && dto.Image.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await dto.Image.CopyToAsync(memoryStream);
                        imageData = memoryStream.ToArray();
                        imageContentType = dto.Image.ContentType;
                    }
                }

                var product = new Product
                {
                    Name = dto.Name,
                    Price = dto.Price,
                    Dose = dto.Dose,
                    Notes = dto.Notes,
                    LocationInStore = dto.LocationInStore,
                    ImageData = imageData,
                    ImageContentType = imageContentType,
                    StockQuantity = dto.StockQuantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم إنشاء المنتج بنجاح", productId = product.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء المنتج" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpdateProductDto dto)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null || !product.IsActive)
                {
                    return NotFound(new { message = "المنتج غير موجود" });
                }

                if (!string.IsNullOrWhiteSpace(dto.Name))
                    product.Name = dto.Name;

                if (dto.Price.HasValue)
                    product.Price = dto.Price.Value;

                if (dto.Dose != null)
                    product.Dose = dto.Dose;

                if (dto.Notes != null)
                    product.Notes = dto.Notes;

                if (dto.LocationInStore != null)
                    product.LocationInStore = dto.LocationInStore;

                if (dto.StockQuantity.HasValue)
                    product.StockQuantity = dto.StockQuantity.Value;

                if (dto.Image != null && dto.Image.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await dto.Image.CopyToAsync(memoryStream);
                        product.ImageData = memoryStream.ToArray();
                        product.ImageContentType = dto.Image.ContentType;
                    }
                }

                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم تحديث المنتج بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                return StatusCode(500, new { message = "حدث خطأ أثناء تحديث المنتج" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound(new { message = "المنتج غير موجود" });
                }

                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم حذف المنتج بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return StatusCode(500, new { message = "حدث خطأ أثناء حذف المنتج" });
            }
        }
    }
}

