using Microsoft.EntityFrameworkCore;
using Medicine_exhibition_api.Data;
using Medicine_exhibition_api.Models;

namespace Medicine_exhibition_api.Services
{
    public class PushNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(ApplicationDbContext context, ILogger<PushNotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendInvoiceNotificationToOwner(int invoiceId, string customerName, decimal totalAmount, string createdByUserName)
        {
            try
            {
                // Get all owner users
                var owners = _context.Users
                    .Where(u => u.Role == "Owner" && u.IsActive)
                    .ToList();

                foreach (var owner in owners)
                {
                    // Create notification in database
                    var notification = new Notification
                    {
                        UserId = owner.Id,
                        Title = "فاتورة جديدة",
                        Message = $"تم إنشاء فاتورة جديدة من {createdByUserName} للمشتري {customerName} بقيمة {totalAmount:C}",
                        InvoiceId = invoiceId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);

                    // TODO: Send actual push notification using FCM
                    // You can integrate Firebase Cloud Messaging here
                    // For now, we'll just save the notification in the database
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification");
            }
        }

        public async Task MarkNotificationAsRead(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}

