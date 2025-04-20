using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymKeepWebApi.Models; // Modellerinizin namespace'i
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations; // DTO için Range gibi Attribute'lar

[ApiController]
// Rota kullanıcı bazlı: /api/users/{userId}/WorkoutPlan (Controller adı 'WorkoutPlanController' olduğu için)
[Route("api/users/{userId}/[controller]")]
public class WorkoutPlanController : ControllerBase
{
    private readonly MyDbContext _context;

    public WorkoutPlanController(MyDbContext context)
    {
        _context = context;
    }

    // GET: api/users/{userId}/WorkoutPlan
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkoutPlan>>> GetWorkoutPlans(int userId)
    {
        if (!await UserExists(userId)) // Kullanıcı var mı kontrolü
        {
            return NotFound($"User with ID {userId} not found.");
        }

        // WorkoutPlan yerine WorkoutPlanDto döndürmek daha iyi olabilir ama şimdilik modeli döndürelim
        return await _context.WorkoutPlans
                             .Where(wp => wp.UserId == userId)
                             .OrderBy(wp => wp.Name)
                             .ToListAsync();
    }

    // GET: api/users/{userId}/WorkoutPlan/{planId}
    [HttpGet("{planId}")]
    public async Task<ActionResult<WorkoutPlan>> GetWorkoutPlan(int userId, int planId)
    {
        var workoutPlan = await _context.WorkoutPlans
                                        .FirstOrDefaultAsync(wp => wp.WorkoutPlanId == planId && wp.UserId == userId);

        if (workoutPlan == null)
        {
            return NotFound($"Workout plan with ID {planId} not found for user {userId}.");
        }

        // WorkoutPlanDto döndürmek daha iyi olabilir
        return workoutPlan;
    }

    // POST: api/users/{userId}/WorkoutPlan
    // --- DTO KULLANILACAK ŞEKİLDE GÜNCELLENDİ ---
    [HttpPost]
    public async Task<ActionResult<WorkoutPlan>> CreateWorkoutPlan(int userId, [FromBody] WorkoutPlanCreateDto createDto) // Artık Create DTO alıyor
    {
        if (!await UserExists(userId))
        {
            return NotFound($"User with ID {userId} not found.");
        }

        // Yeni WorkoutPlan nesnesi DTO'dan oluşturuluyor
        var newWorkoutPlan = new WorkoutPlan
        {
            Name = createDto.Name, // DTO'dan Name
            Description = createDto.Description, // DTO'dan Description
            UserId = userId, // Rotadan gelen userId
            CreatedAt = DateTime.UtcNow, // Sunucu zamanı
        };

        _context.WorkoutPlans.Add(newWorkoutPlan);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) // Veritabanı hatasını yakala
        {
            // Hata detayını loglamak önemli
            Console.WriteLine($"ERROR saving new WorkoutPlan: {ex.InnerException?.Message ?? ex.Message}");
            // İstemciye daha genel bir hata döndür veya spesifik duruma göre farklı kodlar
            return StatusCode(500, $"Plan kaydedilirken bir veritabanı hatası oluştu. Detay: {ex.InnerException?.Message ?? ex.Message}");
        }

        // Oluşturulan tam WorkoutPlan nesnesini döndür (ID atanmış haliyle)
        return CreatedAtAction(nameof(GetWorkoutPlan), new { userId = userId, planId = newWorkoutPlan.WorkoutPlanId }, newWorkoutPlan);
    }
    // ------------------------------------------

    // PUT: api/users/{userId}/WorkoutPlan/{planId}
    // --- DTO KULLANILACAK ŞEKİLDE GÜNCELLENDİ ---
    [HttpPut("{planId}")]
    public async Task<IActionResult> UpdateWorkoutPlan(int userId, int planId, [FromBody] WorkoutPlanUpdateDto updateDto) // Artık Update DTO alıyor
    {
        // Mevcut planı bul ve kullanıcıya ait mi kontrol et
        var existingPlan = await _context.WorkoutPlans
                                        .FirstOrDefaultAsync(wp => wp.WorkoutPlanId == planId && wp.UserId == userId);

        if (existingPlan == null)
        {
            return NotFound($"Workout plan with ID {planId} not found for user {userId}.");
        }

        // DTO'dan gelen değerlerle güncelle
        existingPlan.Name = updateDto.Name;
        existingPlan.Description = updateDto.Description;
        // existingPlan.UpdatedAt = DateTime.UtcNow; // UpdatedAt alanı varsa güncellenir

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await WorkoutPlanExists(planId, userId)) return NotFound(); else throw;
        }
        catch (DbUpdateException ex) { return BadRequest($"Database update error: {ex.InnerException?.Message ?? ex.Message}"); }

        return NoContent(); // Başarılı güncelleme
    }
    // ------------------------------------------

    // DELETE: api/users/{userId}/WorkoutPlan/{planId}
    [HttpDelete("{planId}")]
    public async Task<IActionResult> DeleteWorkoutPlan(int userId, int planId)
    {
        var workoutPlan = await _context.WorkoutPlans
                                        // Silmeden önce ilişkili egzersizleri yüklemeye gerek yok
                                        .FirstOrDefaultAsync(wp => wp.WorkoutPlanId == planId && wp.UserId == userId);

        if (workoutPlan == null)
        {
            return NotFound($"Workout plan with ID {planId} not found for user {userId}.");
        }

        _context.WorkoutPlans.Remove(workoutPlan);

        try
        {
            // Cascade delete ayarlıysa ilişkili PlanExercises'leri de siler
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"ERROR deleting WorkoutPlan {planId}: {ex.InnerException?.Message ?? ex.Message}");
            return BadRequest($"Plan silinirken bir veritabanı hatası oluştu. Detay: {ex.InnerException?.Message ?? ex.Message}");
        }

        return NoContent();
    }

    // --- PlanExercises Yönetimi ---

    // GET: api/users/{userId}/WorkoutPlan/{planId}/Exercises
    [HttpGet("{planId}/Exercises")]
    public async Task<ActionResult<IEnumerable<PlanExercise>>> GetPlanExercises(int userId, int planId)
    {
        if (!await WorkoutPlanExists(planId, userId))
        {
            return NotFound($"Workout plan with ID {planId} not found for user {userId}.");
        }

        // PlanExerciseDto döndürmek daha iyi olabilir
        return await _context.PlanExercises
                             .Where(pe => pe.WorkoutPlanId == planId)
                             .Include(pe => pe.Exercise)
                                .ThenInclude(ex => ex.ExerciseRegion)
                             .Include(pe => pe.Exercise)
                                .ThenInclude(ex => ex.DifficultyLevel)
                             .OrderBy(pe => pe.OrderInPlan ?? int.MaxValue)
                             .ToListAsync();
    }

    // GET: api/users/{userId}/WorkoutPlan/{planId}/Exercises/{planExerciseId}
    [HttpGet("{planId}/Exercises/{planExerciseId}")]
    public async Task<ActionResult<PlanExercise>> GetPlanExercise(int userId, int planId, int planExerciseId)
    {
        if (!await WorkoutPlanExists(planId, userId))
        {
            return NotFound($"Workout plan with ID {planId} not found for user {userId}.");
        }

        var planExercise = await _context.PlanExercises
                                     .Include(pe => pe.Exercise)
                                     .FirstOrDefaultAsync(pe => pe.PlanExerciseId == planExerciseId && pe.WorkoutPlanId == planId);

        if (planExercise == null)
        {
            return NotFound($"Exercise with ID {planExerciseId} not found in plan {planId}.");
        }
        // PlanExerciseDto döndürmek daha iyi olabilir
        return planExercise;
    }


    // POST: api/users/{userId}/WorkoutPlan/{planId}/Exercises
    // --- DTO KULLANILACAK ŞEKİLDE GÜNCELLENDİ ---
    [HttpPost("{planId}/Exercises")]
    public async Task<ActionResult<PlanExercise>> AddExerciseToPlan(int userId, int planId, [FromBody] PlanExerciseCreateDto createDto) // Artık Create DTO alıyor
    {
        if (!await WorkoutPlanExists(planId, userId))
        {
            return NotFound($"Workout plan with ID {planId} not found for user {userId}.");
        }
        if (!await _context.Exercises.AnyAsync(ex => ex.ExerciseId == createDto.ExerciseId))
        {
            return BadRequest($"Exercise with ID {createDto.ExerciseId} not found.");
        }

        // DTO'dan yeni PlanExercise nesnesi oluştur
        var newPlanExercise = new PlanExercise
        {
            WorkoutPlanId = planId,
            ExerciseId = createDto.ExerciseId,
            Sets = createDto.Sets,
            Reps = createDto.Reps,
            RestDurationSeconds = createDto.RestDurationSeconds,
            OrderInPlan = createDto.OrderInPlan
        };

        _context.PlanExercises.Add(newPlanExercise);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"ERROR saving new PlanExercise: {ex.InnerException?.Message ?? ex.Message}");
            return StatusCode(500, $"Egzersiz plana kaydedilirken bir veritabanı hatası oluştu: {ex.InnerException?.Message ?? ex.Message}");
        }


        // Oluşturulan kaynağı döndür (ilişkili Exercise ile)
        await _context.Entry(newPlanExercise).Reference(pe => pe.Exercise).LoadAsync();
        return CreatedAtAction(nameof(GetPlanExercise), new { userId, planId, planExerciseId = newPlanExercise.PlanExerciseId }, newPlanExercise);
    }
    // ------------------------------------------

    // PUT: api/users/{userId}/WorkoutPlan/{planId}/Exercises/{planExerciseId}
    // --- DTO KULLANILARAK GÜNCELLENDİ (Önceki hali doğruydu) ---
    [HttpPut("{planId}/Exercises/{planExerciseId}")]
    public async Task<IActionResult> UpdatePlanExercise(int userId, int planId, int planExerciseId, [FromBody] PlanExerciseUpdateDto updateDto) // DTO alıyor
    {
        if (!await WorkoutPlanExists(planId, userId))
        {
            return NotFound($"Workout plan with ID {planId} not found for user {userId}.");
        }

        var existingPlanExercise = await _context.PlanExercises
                                                .FirstOrDefaultAsync(pe => pe.PlanExerciseId == planExerciseId && pe.WorkoutPlanId == planId);

        if (existingPlanExercise == null)
        {
            return NotFound($"Exercise with ID {planExerciseId} not found in plan {planId}.");
        }

        // DTO'dan gelen değerlerle güncelle
        existingPlanExercise.Sets = updateDto.Sets;
        existingPlanExercise.Reps = updateDto.Reps;
        existingPlanExercise.RestDurationSeconds = updateDto.RestDurationSeconds;
        existingPlanExercise.OrderInPlan = updateDto.OrderInPlan;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.PlanExercises.AnyAsync(pe => pe.PlanExerciseId == planExerciseId && pe.WorkoutPlanId == planId)) return NotFound(); else throw;
        }
        catch (DbUpdateException ex) { return BadRequest($"Database update error: {ex.InnerException?.Message ?? ex.Message}"); }

        return NoContent();
    }
    // ---------------------------------

    // DELETE: api/users/{userId}/WorkoutPlan/{planId}/Exercises/{planExerciseId}
    [HttpDelete("{planId}/Exercises/{planExerciseId}")]
    public async Task<IActionResult> RemoveExerciseFromPlan(int userId, int planId, int planExerciseId)
    {
        if (!await WorkoutPlanExists(planId, userId))
        {
            return NotFound($"Workout plan with ID {planId} not found for user {userId}.");
        }

        var planExercise = await _context.PlanExercises
                                        .FirstOrDefaultAsync(pe => pe.PlanExerciseId == planExerciseId && pe.WorkoutPlanId == planId);

        if (planExercise == null)
        {
            return NotFound($"Exercise with ID {planExerciseId} not found in plan {planId}.");
        }

        _context.PlanExercises.Remove(planExercise);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) { return BadRequest($"Database error: {ex.InnerException?.Message ?? ex.Message}"); }

        return NoContent();
    }

    // --- Helper Metotlar ---
    private async Task<bool> UserExists(int userId)
    {
        return await _context.Users.AnyAsync(e => e.UserId == userId);
    }

    private async Task<bool> WorkoutPlanExists(int planId, int userId)
    {
        return await _context.WorkoutPlans.AnyAsync(e => e.WorkoutPlanId == planId && e.UserId == userId);
    }
}

// ========== DTO Sınıfları ==========
// (Bu kısım sınıfın dışında, namespace içinde veya ayrı bir Dtos klasöründe olmalı)

/// <summary>
/// Antrenman planı oluştururken kullanılan veri transfer nesnesi.
/// </summary>
public class WorkoutPlanCreateDto
{
    [Required(ErrorMessage = "Plan adı boş bırakılamaz.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Plan adı 3 ile 100 karakter arasında olmalıdır.")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }
}

/// <summary>
/// Antrenman planı güncellerken kullanılan veri transfer nesnesi.
/// </summary>
public class WorkoutPlanUpdateDto
{
    [Required(ErrorMessage = "Plan adı boş bırakılamaz.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Plan adı 3 ile 100 karakter arasında olmalıdır.")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }
}

/// <summary>
/// Bir plana egzersiz eklerken kullanılan veri transfer nesnesi.
/// </summary>
public class PlanExerciseCreateDto
{
    [Required(ErrorMessage = "Eklenecek egzersizin ID'si gereklidir.")]
    public int ExerciseId { get; set; }

    [Required(ErrorMessage = "Set sayısı gereklidir.")]
    [Range(1, 100, ErrorMessage = "Set sayısı 1 ile 100 arasında olmalıdır.")] // Üst limiti ayarlayın
    public int Sets { get; set; }

    [Required(ErrorMessage = "Tekrar sayısı gereklidir.")]
    [Range(1, 100, ErrorMessage = "Tekrar sayısı 1 ile 100 arasında olmalıdır.")] // Üst limiti ayarlayın
    public int Reps { get; set; }

    [Range(0, 3600, ErrorMessage = "Dinlenme süresi 0 ile 3600 saniye arasında olmalıdır.")] // 1 saate kadar
    public int? RestDurationSeconds { get; set; }

    [Range(1, 1000, ErrorMessage = "Sıra numarası 1 ile 1000 arasında olmalıdır.")] // 0 yerine 1'den başlasın?
    public int? OrderInPlan { get; set; }
}

/// <summary>
/// Bir plandaki egzersizi güncellerken kullanılan veri transfer nesnesi.
/// </summary>
public class PlanExerciseUpdateDto
{
    [Required(ErrorMessage = "Set sayısı gereklidir.")]
    [Range(1, 100, ErrorMessage = "Set sayısı 1 ile 100 arasında olmalıdır.")]
    public int Sets { get; set; }

    [Required(ErrorMessage = "Tekrar sayısı gereklidir.")]
    [Range(1, 100, ErrorMessage = "Tekrar sayısı 1 ile 100 arasında olmalıdır.")]
    public int Reps { get; set; }

    [Range(0, 3600, ErrorMessage = "Dinlenme süresi 0 ile 3600 saniye arasında olmalıdır.")]
    public int? RestDurationSeconds { get; set; }

    [Range(1, 1000, ErrorMessage = "Sıra numarası 1 ile 1000 arasında olmalıdır.")]
    public int? OrderInPlan { get; set; }

    // workoutPlanId, exerciseId, planExerciseId buraya eklenmez, URL'den veya veritabanından alınır.
}

// ----------------------------------