using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Medicine_exhibition_api.Data;
using Medicine_exhibition_api.DTOs;
using Medicine_exhibition_api.Models;
using Medicine_exhibition_api.Services;

namespace Medicine_exhibition_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PushNotificationService _pushNotificationService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            ApplicationDbContext context,
            PushNotificationService pushNotificationService,
            ILogger<InvoicesController> logger)
        {
            _context = context;
            _pushNotificationService = pushNotificationService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

                if (dto.Items == null || !dto.Items.Any())
                {
                    return BadRequest(new { message = "يجب إضافة منتج واحد على الأقل" });
                }

                var invoice = new Invoice
                {
                    CustomerName = dto.CustomerName,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsConfirmed = false,
                    IsViewedByOwner = false
                };

                decimal totalAmount = 0;

                foreach (var itemDto in dto.Items)
                {
                    var product = await _context.Products.FindAsync(itemDto.ProductId);

                    if (product == null || !product.IsActive)
                    {
                        return BadRequest(new { message = $"المنتج برقم {itemDto.ProductId} غير موجود" });
                    }

                    if (product.StockQuantity < itemDto.Quantity)
                    {
                        return BadRequest(new { message = $"الكمية المتاحة من {product.Name} غير كافية" });
                    }

                    var unitPrice = product.Price;
                    var itemTotal = unitPrice * itemDto.Quantity;
                    totalAmount += itemTotal;

                    var invoiceItem = new InvoiceItem
                    {
                        Invoice = invoice,
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = itemTotal
                    };

                    invoice.InvoiceItems.Add(invoiceItem);

                    // Update stock
                    product.StockQuantity -= itemDto.Quantity;
                }

                invoice.TotalAmount = totalAmount;
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم إنشاء الفاتورة بنجاح", invoiceId = invoice.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء الفاتورة" });
            }
        }

        [HttpPost("{id}/confirm")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> ConfirmInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices
                    .Include(i => i.CreatedByUser)
                    .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Product)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null)
                {
                    return NotFound(new { message = "الفاتورة غير موجودة" });
                }

                if (invoice.IsConfirmed)
                {
                    return BadRequest(new { message = "الفاتورة مؤكدة بالفعل" });
                }

                invoice.IsConfirmed = true;
                await _context.SaveChangesAsync();

                // Send notification to owner
                await _pushNotificationService.SendInvoiceNotificationToOwner(
                    invoice.Id,
                    invoice.CustomerName,
                    invoice.TotalAmount,
                    invoice.CreatedByUser.Username
                );

                return Ok(new { message = "تم تأكيد الفاتورة بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming invoice");
                return StatusCode(500, new { message = "حدث خطأ أثناء تأكيد الفاتورة" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInvoices([FromQuery] bool? isConfirmed = null, [FromQuery] int? createdByUserId = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;

                var query = _context.Invoices
                    .Include(i => i.CreatedByUser)
                    .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Product)
                    .AsQueryable();

                // Owner: sees all invoices unless deleted by owner
                // Employee: sees only own invoices for today (auto-hidden next day)
                if (role == "Owner")
                {
                    query = query.Where(i => !i.IsDeletedByOwner);

                    if (createdByUserId.HasValue)
                    {
                        query = query.Where(i => i.CreatedByUserId == createdByUserId.Value);
                    }
                }
                else
                {
                    var today = DateTime.UtcNow.Date;
                    var tomorrow = today.AddDays(1);

                    query = query.Where(i =>
                        i.CreatedByUserId == userId &&
                        i.CreatedAt >= today &&
                        i.CreatedAt < tomorrow);
                }

                if (isConfirmed.HasValue)
                {
                    query = query.Where(i => i.IsConfirmed == isConfirmed.Value);
                }

                var invoices = await query
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                var result = invoices.Select(i => new InvoiceResponseDto
                {
                    Id = i.Id,
                    CustomerName = i.CustomerName,
                    TotalAmount = i.TotalAmount,
                    CreatedAt = i.CreatedAt,
                    CreatedByUserName = i.CreatedByUser.Username,
                    IsConfirmed = i.IsConfirmed,
                    IsViewedByOwner = i.IsViewedByOwner,
                    Items = i.InvoiceItems.Select(ii => new InvoiceItemResponseDto
                    {
                        Id = ii.Id,
                        ProductId = ii.ProductId,
                        ProductName = ii.Product.Name,
                        Quantity = ii.Quantity,
                        UnitPrice = ii.UnitPrice,
                        TotalPrice = ii.TotalPrice
                    }).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoices");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب الفواتير" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;

                var invoice = await _context.Invoices
                    .Include(i => i.CreatedByUser)
                    .Include(i => i.InvoiceItems)
                    .ThenInclude(ii => ii.Product)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null || (role == "Owner" && invoice.IsDeletedByOwner))
                {
                    return NotFound(new { message = "الفاتورة غير موجودة" });
                }

                if (role != "Owner" && invoice.CreatedByUserId != userId)
                {
                    return Forbid();
                }

                var result = new InvoiceResponseDto
                {
                    Id = invoice.Id,
                    CustomerName = invoice.CustomerName,
                    TotalAmount = invoice.TotalAmount,
                    CreatedAt = invoice.CreatedAt,
                    CreatedByUserName = invoice.CreatedByUser.Username,
                    IsConfirmed = invoice.IsConfirmed,
                    IsViewedByOwner = invoice.IsViewedByOwner,
                    Items = invoice.InvoiceItems.Select(ii => new InvoiceItemResponseDto
                    {
                        Id = ii.Id,
                        ProductId = ii.ProductId,
                        ProductName = ii.Product.Name,
                        Quantity = ii.Quantity,
                        UnitPrice = ii.UnitPrice,
                        TotalPrice = ii.TotalPrice
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice");
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب الفاتورة" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null)
                {
                    return NotFound(new { message = "الفاتورة غير موجودة" });
                }

                if (invoice.IsDeletedByOwner)
                {
                    return Ok(new { message = "تم حذف الفاتورة بالفعل" });
                }

                invoice.IsDeletedByOwner = true;
                invoice.DeletedByOwnerAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم حذف الفاتورة بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice");
                return StatusCode(500, new { message = "حدث خطأ أثناء حذف الفاتورة" });
            }
        }

        [HttpPost("{id}/mark-as-viewed")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> MarkInvoiceAsViewed(int id)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);

                if (invoice == null)
                {
                    return NotFound(new { message = "الفاتورة غير موجودة" });
                }

                invoice.IsViewedByOwner = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم تحديد الفاتورة كمقروءة" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice as viewed");
                return StatusCode(500, new { message = "حدث خطأ أثناء تحديث حالة الفاتورة" });
            }
        }
    }
}

