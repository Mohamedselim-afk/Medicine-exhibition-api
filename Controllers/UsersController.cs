using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Medicine_exhibition_api.Data;

namespace Medicine_exhibition_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Owner")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var employees = await _context.Users
                    .Where(u => u.IsActive && u.Role == "Employee")
                    .OrderBy(u => u.Username)
                    .Select(u => new
                    {
                        id = u.Id,
                        username = u.Username,
                        email = u.Email,
                        fullName = u.FullName,
                        phoneNumber = u.PhoneNumber,
                        createdAt = u.CreatedAt
                    })
                    .ToListAsync();

                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب الموظفين" });
            }
        }

        [HttpGet("employees/{id:int}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            try
            {
                var employee = await _context.Users
                    .Where(u => u.Id == id && u.IsActive && u.Role == "Employee")
                    .Select(u => new
                    {
                        id = u.Id,
                        username = u.Username,
                        email = u.Email,
                        fullName = u.FullName,
                        phoneNumber = u.PhoneNumber,
                        createdAt = u.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    return NotFound(new { message = "الموظف غير موجود" });
                }

                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب بيانات الموظف" });
            }
        }

        [HttpGet("employees/{id:int}/invoices")]
        public async Task<IActionResult> GetEmployeeInvoices(int id, [FromQuery] bool? isConfirmed = null)
        {
            try
            {
                var exists = await _context.Users.AnyAsync(u => u.Id == id && u.IsActive && u.Role == "Employee");
                if (!exists)
                {
                    return NotFound(new { message = "الموظف غير موجود" });
                }

                var query = _context.Invoices
                    .Include(i => i.CreatedByUser)
                    .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Product)
                    .Where(i => !i.IsDeletedByOwner && i.CreatedByUserId == id)
                    .AsQueryable();

                if (isConfirmed.HasValue)
                {
                    query = query.Where(i => i.IsConfirmed == isConfirmed.Value);
                }

                var invoices = await query
                    .OrderByDescending(i => i.CreatedAt)
                    .Select(i => new
                    {
                        id = i.Id,
                        customerName = i.CustomerName,
                        totalAmount = i.TotalAmount,
                        createdAt = i.CreatedAt,
                        createdByUserName = i.CreatedByUser.Username,
                        isConfirmed = i.IsConfirmed,
                        isViewedByOwner = i.IsViewedByOwner,
                        items = i.InvoiceItems.Select(ii => new
                        {
                            id = ii.Id,
                            productId = ii.ProductId,
                            productName = ii.Product.Name,
                            quantity = ii.Quantity,
                            unitPrice = ii.UnitPrice,
                            totalPrice = ii.TotalPrice
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee invoices");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب فواتير الموظف" });
            }
        }
    }
}


