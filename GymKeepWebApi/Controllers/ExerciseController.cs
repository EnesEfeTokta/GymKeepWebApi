using GymKeepWebApi.Models;
using GymKeepWebApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // Yetkilendirme için

[ApiController]
[Route("api/[controller]")] // Route: /api/exercise
public class ExerciseController : ControllerBase
{
    private readonly MyDbContext _context;

    public ExerciseController(MyDbContext context)
    {
        _context = context;
    }

    // GET: api/exercise
    // Tüm egzersizleri listeler, filtreleme eklenebilir.
    [HttpGet]
    [AllowAnonymous] // Herkes egzersizleri listeleyebilir (isterseniz [Authorize] yapabilirsiniz)
    public async Task<ActionResult<IEnumerable<ExerciseDto>>> GetAllExercises(
        [FromQuery] int? difficultyId = null,
        [FromQuery] int? regionId = null)
    {
        // Temel sorgu, ilişkili verileri (seviye, bölge) dahil et
        var query = _context.Exercises
            .Include(e => e.DifficultyLevel) // Seviye adını almak için
            .Include(e => e.ExerciseRegion) // Bölge adını almak için
            .AsQueryable(); // Filtreleme uygulamak için

        // Filtreleme
        if (difficultyId.HasValue)
        {
            query = query.Where(e => e.DifficultyLevelId == difficultyId.Value);
        }
        if (regionId.HasValue)
        {
            query = query.Where(e => e.RegionId == regionId.Value);
        }

        // Sonuçları DTO'ya dönüştür
        var exercises = await query
            .Select(e => new ExerciseDto(
                e.Id,
                e.Name,
                e.Description,
                e.VideoUrl,
                e.ImageUrl,
                e.DifficultyLevelId,
                e.DifficultyLevel != null ? e.DifficultyLevel.Name : "Bilinmiyor", // Null kontrolü
                e.RegionId,
                e.ExerciseRegion != null ? e.ExerciseRegion.Name : "Bilinmiyor" // Null kontrolü
            ))
            .ToListAsync();

        return Ok(exercises);
    }

    // GET: api/exercise/{id}
    // Belirli bir egzersizin detayını getirir.
    [HttpGet("{id}")]
    [AllowAnonymous] // Herkes detay görebilir
    public async Task<ActionResult<ExerciseDto>> GetExerciseById(int id)
    {
        var exercise = await _context.Exercises
            .Include(e => e.DifficultyLevel)
            .Include(e => e.ExerciseRegion)
            .Where(e => e.Id == id)
            .Select(e => new ExerciseDto(
                e.Id,
                e.Name,
                e.Description,
                e.VideoUrl,
                e.ImageUrl,
                e.DifficultyLevelId,
                e.DifficultyLevel != null ? e.DifficultyLevel.Name : "Bilinmiyor",
                e.RegionId,
                e.ExerciseRegion != null ? e.ExerciseRegion.Name : "Bilinmiyor"
            ))
            .FirstOrDefaultAsync();

        if (exercise == null)
        {
            return NotFound(new { Message = "Egzersiz bulunamadı." });
        }

        return Ok(exercise);
    }

    // POST: api/exercise
    // Yeni bir egzersiz oluşturur.
    [HttpPost]
    [Authorize] // Sadece giriş yapmış kullanıcılar (veya belirli roller) ekleyebilir
    // [Authorize(Roles = "Admin")] // Örnek: Sadece Admin ekleyebilir
    public async Task<ActionResult<ExerciseDto>> CreateExercise([FromBody] CreateExerciseDto createDto)
    {
        // Gönderilen DifficultyLevelId ve RegionId veritabanında var mı kontrol et
        var difficultyExists = await _context.DifficultyLevels.AnyAsync(dl => dl.Id == createDto.DifficultyLevelId);
        var regionExists = await _context.ExerciseRegions.AnyAsync(er => er.Id == createDto.RegionId);

        if (!difficultyExists || !regionExists)
        {
            return BadRequest(new { Message = "Geçersiz Zorluk Seviyesi veya Bölge ID'si." });
        }

        // Yeni egzersiz entity'sini oluştur
        var exercise = new Exercise
        {
            Name = createDto.Name,
            Description = createDto.Description,
            VideoUrl = createDto.VideoUrl,
            ImageUrl = createDto.ImageUrl,
            DifficultyLevelId = createDto.DifficultyLevelId,
            RegionId = createDto.RegionId
            // CreatedAt vb. alanlar modelde varsayılan değere sahipse otomatik atanır
        };

        _context.Exercises.Add(exercise);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Detaylı loglama yapılmalı
            Console.WriteLine($"Error creating exercise: {ex.InnerException?.Message ?? ex.Message}");
            return StatusCode(500, new { Message = "Egzersiz oluşturulurken bir veritabanı hatası oluştu." });
        }

        // Başarıyla oluşturulan egzersizi DTO olarak döndür.
        // İlişkili verileri tekrar yüklemek gerekebilir veya manuel map yapılabilir.
        // En temizi: Kayıttan sonra ID ile tekrar çekip DTO oluşturmak.
        var createdExerciseDto = await GetExerciseById(exercise.Id); // İçindeki Ok'u değil değeri alalım
        if (createdExerciseDto.Result is OkObjectResult okResult && okResult.Value is ExerciseDto dto)
        {
            // Yeni oluşturulan kaynağın konumunu ve kendisini döndür (201 Created)
            return CreatedAtAction(nameof(GetExerciseById), new { id = exercise.Id }, dto);
        }
        else
        {
            // Beklenmedik durum, loglanmalı. Sadece temel bilgiyi dön.
            return StatusCode(201, new { Message = "Egzersiz başarıyla oluşturuldu ama detay alınamadı.", Id = exercise.Id });
        }

    }

    // PUT: api/exercise/{id}
    // Mevcut bir egzersizi günceller.
    [HttpPut("{id}")]
    [Authorize] // Sadece giriş yapmış kullanıcılar (veya Admin) güncelleyebilir
    public async Task<IActionResult> UpdateExercise(int id, [FromBody] UpdateExerciseDto updateDto)
    {
        var exerciseToUpdate = await _context.Exercises.FindAsync(id);

        if (exerciseToUpdate == null)
        {
            return NotFound(new { Message = "Güncellenecek egzersiz bulunamadı." });
        }

        // Gönderilen DifficultyLevelId ve RegionId veritabanında var mı kontrol et
        var difficultyExists = await _context.DifficultyLevels.AnyAsync(dl => dl.Id == updateDto.DifficultyLevelId);
        var regionExists = await _context.ExerciseRegions.AnyAsync(er => er.Id == updateDto.RegionId);

        if (!difficultyExists || !regionExists)
        {
            return BadRequest(new { Message = "Geçersiz Zorluk Seviyesi veya Bölge ID'si." });
        }

        // Entity'nin alanlarını güncelle
        exerciseToUpdate.Name = updateDto.Name;
        exerciseToUpdate.Description = updateDto.Description;
        exerciseToUpdate.VideoUrl = updateDto.VideoUrl;
        exerciseToUpdate.ImageUrl = updateDto.ImageUrl;
        exerciseToUpdate.DifficultyLevelId = updateDto.DifficultyLevelId;
        exerciseToUpdate.RegionId = updateDto.RegionId;
        // exerciseToUpdate.UpdatedAt = DateTime.UtcNow; // Eğer böyle bir alan varsa

        // Durumu Modified olarak işaretle (FindAsync sonrası değişikliklerde EF Core genelde bunu otomatik anlar)
        // _context.Entry(exerciseToUpdate).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Aynı anda başka birisi bu kaydı değiştirmiş veya silmiş olabilir
            Console.WriteLine($"Concurrency error updating exercise {id}: {ex.Message}");
            return Conflict(new { Message = "Egzersiz güncellenirken bir çakışma yaşandı. Lütfen tekrar deneyin." });
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"Error updating exercise {id}: {ex.InnerException?.Message ?? ex.Message}");
            return StatusCode(500, new { Message = "Egzersiz güncellenirken bir veritabanı hatası oluştu." });
        }

        // Başarılı güncelleme sonrası genellikle 204 No Content döndürülür.
        return NoContent();
    }

    // DELETE: api/exercise/{id}
    // Bir egzersizi siler.
    [HttpDelete("{id}")]
    [Authorize] // Sadece giriş yapmış kullanıcılar (veya Admin) silebilir
    public async Task<IActionResult> DeleteExercise(int id)
    {
        var exerciseToDelete = await _context.Exercises.FindAsync(id);

        if (exerciseToDelete == null)
        {
            return NotFound(new { Message = "Silinecek egzersiz bulunamadı." });
        }

        _context.Exercises.Remove(exerciseToDelete);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // İlişkili veri silinemiyorsa (Restrict kuralı varsa) buraya düşebilir
            Console.WriteLine($"Error deleting exercise {id}: {ex.InnerException?.Message ?? ex.Message}");
            // Kullanıcı dostu bir mesaj vermek önemli
            if (ex.InnerException?.Message.Contains("constraint") ?? false)
            {
                return BadRequest(new { Message = "Bu egzersiz başka kayıtlarda kullanıldığı için silinemiyor (örn: antrenman planları)." });
            }
            return StatusCode(500, new { Message = "Egzersiz silinirken bir veritabanı hatası oluştu." });
        }


        // Başarılı silme sonrası genellikle 204 No Content döndürülür.
        return NoContent();
    }
}