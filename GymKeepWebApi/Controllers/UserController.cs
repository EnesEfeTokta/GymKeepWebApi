using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymKeepWebApi.Models; // Model namespace'iniz
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly MyDbContext _context; // DbContext'inizin adı

    public UserController(MyDbContext context)
    {
        _context = context;
    }

    // --- Mevcut CRUD İşlemleri (Önceki cevaptan) ---

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
    {
        return await _context.Users
            .Select(u => new UserResponseDto // DTO'ya dönüştür
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        // DTO'ya dönüştürerek parolayı gönderme
        var userDto = new UserResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
        return userDto;
    }

    // PUT: api/users/{id} (Güncelleme)
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User user) // Güncelleme için de DTO kullanmak daha iyi
    {
        if (id != user.UserId)
        {
            return BadRequest("ID mismatch.");
        }

        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null)
        {
            return NotFound();
        }

        // Kullanıcı adı/e-posta değiştiriliyorsa unique kontrolü yapılmalı
        if (existingUser.Username != user.Username && await _context.Users.AnyAsync(u => u.Username == user.Username && u.UserId != id))
        {
            return BadRequest("Username already exists.");
        }
        if (existingUser.Email != user.Email && await _context.Users.AnyAsync(u => u.Email == user.Email && u.UserId != id))
        {
            return BadRequest("Email already exists.");
        }


        existingUser.Username = user.Username;
        existingUser.Email = user.Email;
        existingUser.UpdatedAt = DateTime.UtcNow;

        // Parolayı burada güncelleME! Ayrı ve güvenli bir endpoint kullanın.
        _context.Entry(existingUser).Property(x => x.Password).IsModified = false;
        _context.Entry(existingUser).Property(x => x.CreatedAt).IsModified = false; // Oluşturma tarihi değişmez

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await UserExists(id)) return NotFound(); else throw;
        }
        catch (DbUpdateException ex)
        {
            return BadRequest($"Failed to update user. Potential duplicate username or email. Error: {ex.InnerException?.Message ?? ex.Message}");
        }

        return NoContent();
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(); // İlişkili verilerin silinmesi (Cascade) DbContext'te ayarlandıysa gerçekleşir.
        }
        catch (DbUpdateException ex) // Cascade delete yoksa ve FK varsa hata verebilir
        {
            return BadRequest($"Cannot delete user. They might have associated data (plans, sessions, etc.). Error: {ex.InnerException?.Message ?? ex.Message}");
        }

        return NoContent();
    }


    // --- YENİ ENDPOINT'LER ---

    // POST: api/users/register
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponseDto>> RegisterUser(RegisterRequestDto registerDto)
    {
        // Model validasyonu (Required, EmailAddress, StringLength vb.) otomatik yapılır.

        // Kullanıcı adı veya e-posta zaten var mı?
        if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
        {
            // ModelState.AddModelError("Username", "Username already exists."); // Daha detaylı hata için
            return BadRequest("Username already exists.");
        }
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
        {
            // ModelState.AddModelError("Email", "Email already exists.");
            return BadRequest("Email already exists.");
        }

        // Yeni kullanıcı oluştur
        var user = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            Password = registerDto.Password, // << GÜVENSİZ!!
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Başarılı yanıtı oluştur (parola olmadan DTO ile)
        var userDto = new UserResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        // Yeni oluşturulan kaynağın URL'si ile 201 Created döndür
        return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDto);
    }

    // POST: api/users/login
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponseDto>> LoginUser(LoginRequestDto loginDto)
    {
        // Kullanıcıyı username ile bul
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);

        if (user == null)
        {
            // Kullanıcı bulunamadıysa veya parola yanlışsa aynı hatayı dönmek daha güvenlidir
            // (kullanıcı adı var mı yok mu bilgisini sızdırmamak için)
            return Unauthorized("Invalid username or password.");
        }

        // !!! GÜVENLİK RİSKİ: PAROLA KONTROLÜ HASH İLE YAPILMALI !!!
        // Gerçek uygulamada: if (!VerifyPasswordHash(loginDto.Password, user.Password))
        if (user.Password != loginDto.Password) // << GÜVENSİZ!!
        {
            return Unauthorized("Invalid username or password.");
        }

        // Başarılı giriş - Kullanıcı bilgilerini DTO ile döndür
        var userDto = new UserResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Ok(userDto); // Başarılı girişte 200 OK
    }


    // --- Helper Metotlar ---
    private async Task<bool> UserExists(int id)
    {
        return await _context.Users.AnyAsync(e => e.UserId == id);
    }
}

public class RegisterRequestDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 6)] // Örnek uzunluk kısıtlaması
    public string Password { get; set; } = null!;
}

// Giriş isteği için
public class LoginRequestDto
{
    [Required]
    public string Username { get; set; } = null!; // Veya Email ile giriş yapılacaksa Email

    [Required]
    public string Password { get; set; } = null!;
}

// Kullanıcı bilgisi döndürmek için (parola olmadan)
public class UserResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // İhtiyaç varsa diğer güvenli alanlar eklenebilir
}