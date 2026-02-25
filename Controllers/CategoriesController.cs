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
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new CategoryItemDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        CreatedAt = c.CreatedAt
                    })
                    .ToListAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب أنواع الأدوية" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequest(new { message = "اسم النوع مطلوب" });

                var exists = await _context.Categories
                    .AnyAsync(c => c.IsActive && c.Name.Trim().ToLower() == dto.Name.Trim().ToLower());
                if (exists)
                    return BadRequest(new { message = "نوع الدواء موجود مسبقاً" });

                var category = new Category
                {
                    Name = dto.Name.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم إضافة نوع الدواء بنجاح", id = category.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, new { message = "حدث خطأ أثناء إضافة نوع الدواء" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null || !category.IsActive)
                    return NotFound(new { message = "نوع الدواء غير موجود" });

                category.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم حذف نوع الدواء بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return StatusCode(500, new { message = "حدث خطأ أثناء حذف نوع الدواء" });
            }
        }
    }
}
