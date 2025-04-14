using GymKeepWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // List<Claim> için
using System.Security.Claims; // Claims için eklendi
using Microsoft.AspNetCore.Authentication; // SignInAsync, SignOutAsync için eklendi
using Microsoft.AspNetCore.Authentication.Cookies; // Cookie şeması için eklendi
using Microsoft.AspNetCore.Authorization; // [Authorize], [AllowAnonymous] için eklendi

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly MyDbContext _context;

    // IConfiguration genellikle JWT için gerekir, Cookie Auth için zorunlu değil.
    public UserController(MyDbContext context)
    {
        _context = context;
    }

    public record UserDto(int Id, string Username, string Email, DateTime CreatedAt);

    public record RegisterModel(
        [Required][StringLength(50)] string Username,
        [Required][EmailAddress][StringLength(150)] string Email,
        // Öneri: Şifre için minimum uzunluk ekleyin
        [Required][MinLength(6)] string Password
        );
    public record LoginModel([Required] string Username, [Required] string Password);

    [HttpPost("register")]
    [AllowAnonymous] // Kayıt için kimlik doğrulaması gerekmez
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
        {
            return BadRequest(new { Message = "Bu kullanıcı adı zaten alınmış." });
        }
        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            return BadRequest(new { Message = "Bu e-posta adresi zaten kullanılıyor." });
        }

        // --- GÜVENLİK ÖNERİSİ: Şifreyi Hash'leyin! ---
        // Düz metin saklamak yerine hash kullanın. User modelindeki Password alanını PasswordHash yapın.
        // string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
        // --- GÜVENLİK ÖNERİSİ SONU ---

        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            // PasswordHash = passwordHash, // Hash'lenmiş şifre
            Password = model.Password, // <<< GÜVENLİ DEĞİL! Geçici olarak bırakıldı.
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userDto = new UserDto(user.Id, user.Username, user.Email, user.CreatedAt);

        // Direkt başarılı mesajı ve kullanıcı bilgisini dönelim.
        return Ok(new { Message = "Kayıt başarılı.", User = userDto });
    }

    [HttpPost("login")]
    [AllowAnonymous] // Giriş için kimlik doğrulaması gerekmez
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        // Kullanıcıyı bul
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

        // --- GÜVENLİK ÖNERİSİ: Hash Karşılaştırması ---
        // Düz metin karşılaştırması yerine hash'i doğrulayın
        // if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash)) // User modelinde PasswordHash varsa
        // --- GÜVENLİK ÖNERİSİ SONU ---

        // <<< GÜVENLİ DEĞİL! Geçici olarak düz metin karşılaştırması bırakıldı.>>>
        if (user == null || user.Password != model.Password)
        {
            return Unauthorized(new { Message = "Kullanıcı adı veya şifre yanlış." });
        }

        // --- Cookie Authentication ile Giriş Yapma ---
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Kullanıcı ID'si (ÇOK ÖNEMLİ)
            new Claim(ClaimTypes.Name, user.Username), // Kullanıcı adı
            new Claim(ClaimTypes.Email, user.Email),
            // İleride rolleriniz olursa: new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.Role, "User")
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme); // Şema adını doğru yazın

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // Tarayıcı kapanınca cookie silinmesin (isteğe bağlı)
            AllowRefresh = true, // Sliding expiration için
            // ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60) // İsterseniz özel süre belirleyebilirsiniz (Program.cs'teki ile tutarlı olmalı)
        };

        // Kimlik doğrulama cookie'sini oluştur ve yanıta ekle
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
        // --- Giriş Yapma Bitiş ---

        var userDto = new UserDto(user.Id, user.Username, user.Email, user.CreatedAt);
        return Ok(new { Message = "Giriş başarılı.", User = userDto }); // Token yerine kullanıcı bilgisi dönülebilir
    }

    [HttpPost("logout")]
    [Authorize] // Çıkış yapmak için giriş yapmış olmak gerekir
    public async Task<IActionResult> Logout() // async Task<IActionResult> olarak değiştirildi
    {
        // Kimlik doğrulama cookie'sini sil
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Eğer HttpContext.Session başka veriler için kullanılıyorsa onu da temizleyebilirsiniz.
        // HttpContext.Session.Clear();

        return Ok(new { Message = "Çıkış başarılı." });
    }


    [HttpGet("me")]
    [Authorize] // Bu endpoint artık kimlik doğrulaması gerektirir
    public async Task<IActionResult> GetCurrentUser() // Metot adı eski haline getirildi
    {
        // [Authorize] attribute'u sayesinde HttpContext.User otomatik olarak doldurulur.
        // Session'dan okumaya gerek YOKTUR.
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier); // Cookie'deki claim'den ID'yi al

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            // Bu durum normalde [Authorize] filtresi nedeniyle olmamalı, ama bir sorun varsa buraya düşebilir.
            return Unauthorized(new { Message = "Geçersiz veya eksik kimlik bilgisi." });
        }

        // Veritabanından kullanıcıyı bul
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            // Cookie geçerli ama kullanıcı DB'de yok (silinmiş olabilir)
            // Güvenlik için çıkış yaptırmak iyi bir pratik olabilir: await HttpContext.SignOutAsync(...)
            return NotFound(new { Message = "Kullanıcı bulunamadı." });
        }

        // Hassas bilgi (şifre) göndermemek için DTO kullan
        var userDto = new UserDto(user.Id, user.Username, user.Email, user.CreatedAt);
        return Ok(userDto);
    }

    [HttpGet] // /api/user
    [Authorize] // Artık kimlik doğrulaması gerektirir (veya Rol bazlı)
    // [Authorize(Roles = "Admin")] // Sadece Admin'ler listelesin isterseniz
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        // Session kontrolüne gerek YOKTUR. [Authorize] bunu halleder.
        try
        {
            var users = await _context.Users
                                     .Select(u => new UserDto(u.Id, u.Username, u.Email, u.CreatedAt))
                                     .ToListAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting all users: {ex.Message}");
            return StatusCode(500, new { Message = "Kullanıcılar alınırken bir sunucu hatası oluştu." });
        }
    }

    [HttpGet("test-db")]
    [AllowAnonymous] // Bu endpoint kimlik doğrulaması gerektirmez
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            if (canConnect)
            {
                return Ok(new { Message = "Veritabanı bağlantısı başarılı." });
            }
            else
            {
                return StatusCode(503, new { Message = "Veritabanına bağlanılamadı." });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DB Connection Error: {ex.Message}");
            return StatusCode(500, new { Message = "Veritabanı bağlantısı test edilirken bir sunucu hatası oluştu." });
        }
    }
}