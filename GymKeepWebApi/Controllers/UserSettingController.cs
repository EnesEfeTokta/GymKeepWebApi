using GymKeepWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GymKeepWebApi.Dtos;

[ApiController]
[Route("api/usersettings")]
[Authorize]
public class UserSettingController : ControllerBase
{
    private readonly MyDbContext _context;

    public UserSettingController(MyDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out int userId)) { return userId; }
        throw new UnauthorizedAccessException("Kullanıcı kimliği alınamadı.");
    }

    // GET: api/usersettings/me
    // Giriş yapmış kullanıcının ayarlarını getirir
    [HttpGet("me")]
    public async Task<ActionResult<UserSettingDto>> GetMySettings()
    {
        var userId = GetCurrentUserId();
        var settings = await _context.UserSettings
            .Where(us => us.UserId == userId)
            .Select(us => new UserSettingDto(
                us.DailyGoal,
                us.IsDarkMode,
                us.NotificationsEnabled,
                us.NotificationTime,
                us.UpdatedAt
            ))
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            // Ayar yoksa varsayılan döndür veya 404
            return Ok(new UserSettingDto(null, false, true, null, DateTime.UtcNow)); // Varsayılan döndürme örneği
            // return NotFound(new { Message = "Kullanıcı ayarları bulunamadı." });
        }

        return Ok(settings);
    }

    // PUT: api/usersettings/me
    // Giriş yapmış kullanıcının ayarlarını günceller (veya oluşturur)
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMySettings([FromBody] UpdateUserSettingDto updateDto)
    {
        var userId = GetCurrentUserId();
        var settings = await _context.UserSettings.FirstOrDefaultAsync(us => us.UserId == userId);

        if (settings == null)
        {
            // Ayar yoksa yenisini oluştur
            settings = new UserSetting
            {
                UserId = userId,
                // Değerleri DTO'dan ata
                DailyGoal = updateDto.DailyGoal,
                IsDarkMode = updateDto.IsDarkMode,
                NotificationsEnabled = updateDto.NotificationsEnabled,
                NotificationTime = updateDto.NotificationTime,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserSettings.Add(settings);
        }
        else
        {
            // Mevcut ayarları güncelle
            settings.DailyGoal = updateDto.DailyGoal;
            settings.IsDarkMode = updateDto.IsDarkMode;
            settings.NotificationsEnabled = updateDto.NotificationsEnabled;
            settings.NotificationTime = updateDto.NotificationTime;
            settings.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return NoContent(); // Başarılı
    }
}