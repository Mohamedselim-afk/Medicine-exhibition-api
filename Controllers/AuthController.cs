using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Medicine_exhibition_api.Data;
using Medicine_exhibition_api.DTOs;
using Medicine_exhibition_api.Helpers;
using Medicine_exhibition_api.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;

namespace Medicine_exhibition_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            JwtHelper jwtHelper,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtHelper = jwtHelper;
            _logger = logger;
        }

        [HttpPost("create-owner")]
        public async Task<IActionResult> CreateOwner([FromBody] CreateOwnerDto dto)
        {
            try
            {
                // Check if there are any users already
                var hasAnyUsers = await _context.Users.AnyAsync();
                if (hasAnyUsers)
                {
                    return BadRequest(new { message = "يوجد بالفعل حساب صاحب عمل في النظام" });
                }

                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                {
                    return BadRequest(new { message = "اسم المستخدم موجود بالفعل" });
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                {
                    return BadRequest(new { message = "البريد الإلكتروني موجود بالفعل" });
                }

                var owner = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Role = "Owner",
                    FullName = dto.FullName ?? "صاحب المحل",
                    PhoneNumber = dto.PhoneNumber,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(owner);
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم إنشاء حساب صاحب العمل بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating owner");
                return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء حساب صاحب العمل" });
            }
        }

        [HttpPost("register")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                // Only owner can create employees
                if (dto.Role != "Employee")
                {
                    return BadRequest(new { message = "يمكن فقط إنشاء حسابات للموظفين" });
                }

                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                {
                    return BadRequest(new { message = "اسم المستخدم موجود بالفعل" });
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                {
                    return BadRequest(new { message = "البريد الإلكتروني موجود بالفعل" });
                }

                var user = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Role = "Employee",
                    FullName = dto.FullName,
                    PhoneNumber = dto.PhoneNumber,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم إنشاء حساب الموظف بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء الحساب" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsActive);

                if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "اسم المستخدم أو كلمة المرور غير صحيحة" });
                }

                var token = _jwtHelper.GenerateToken(user.Id, user.Username, user.Role);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Username = user.Username,
                    Role = user.Role,
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber
                });
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Database connection error during login");
                return StatusCode(500, new { 
                    message = "لا يمكن الاتصال بقاعدة البيانات. يرجى التحقق من الاتصال بالإنترنت وإعدادات قاعدة البيانات",
                    error = "Database connection failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "حدث خطأ أثناء تسجيل الدخول" });
            }
        }

        [HttpGet("generate-hash")]
        public IActionResult GenerateHash([FromQuery] string password = "Admin123!")
        {
            try
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(password, 11);
                return Ok(new { 
                    password = password,
                    hash = hash,
                    message = "استخدم هذا الـ hash في ملفات SQL"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating hash");
                return StatusCode(500, new { message = "حدث خطأ أثناء توليد الـ hash" });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);

                if (user == null)
                {
                    // Don't reveal if email exists for security
                    return Ok(new { message = "إذا كان البريد الإلكتروني موجوداً، سيتم إرسال رابط إعادة تعيين كلمة المرور" });
                }

                // Generate reset token (simple random string)
                var resetToken = Guid.NewGuid().ToString("N");
                user.ResetToken = resetToken;
                user.ResetTokenExpiry = DateTime.UtcNow.AddHours(24); // Token valid for 24 hours
                await _context.SaveChangesAsync();

                // TODO: Send email with reset token
                // For now, return token in response (in production, send via email)
                _logger.LogInformation($"Password reset token for {user.Email}: {resetToken}");

                return Ok(new { 
                    message = "تم إرسال رابط إعادة تعيين كلمة المرور",
                    resetToken = resetToken, // Remove this in production, send via email instead
                    expiresAt = user.ResetTokenExpiry
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password");
                return StatusCode(500, new { message = "حدث خطأ أثناء معالجة الطلب" });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ResetToken == dto.ResetToken && 
                                             u.ResetTokenExpiry > DateTime.UtcNow &&
                                             u.IsActive);

                if (user == null)
                {
                    return BadRequest(new { message = "رمز إعادة التعيين غير صحيح أو منتهي الصلاحية" });
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                user.ResetToken = null;
                user.ResetTokenExpiry = null;
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new { message = "حدث خطأ أثناء تغيير كلمة المرور" });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "المستخدم غير موجود" });
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { message = "حدث خطأ أثناء تغيير كلمة المرور" });
            }
        }

        [HttpPost("update-fcm-token")]
        [Authorize]
        public async Task<IActionResult> UpdateFcmToken([FromBody] string fcmToken)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "المستخدم غير موجود" });
                }

                user.FcmToken = fcmToken;
                await _context.SaveChangesAsync();

                return Ok(new { message = "تم تحديث رمز الإشعارات بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating FCM token");
                return StatusCode(500, new { message = "حدث خطأ أثناء تحديث رمز الإشعارات" });
            }
        }
    }
}

