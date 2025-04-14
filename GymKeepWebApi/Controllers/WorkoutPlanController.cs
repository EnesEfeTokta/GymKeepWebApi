using GymKeepWebApi.Models;
using GymKeepWebApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("api/workoutplans")]
[Authorize] // Bu controller'daki tüm işlemler için giriş yapmış olmak gerekir
public class WorkoutPlanController : ControllerBase
{
    private readonly MyDbContext _context;

    public WorkoutPlanController(MyDbContext context)
    {
        _context = context;
    }

    // Helper metot: Giriş yapmış kullanıcının ID'sini alır
    private int GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out int userId))
        {
            return userId;
        }
        // Bu durum normalde [Authorize] nedeniyle olmamalı
        throw new UnauthorizedAccessException("Kullanıcı kimliği alınamadı.");
    }

    // GET: api/workoutplans
    // Giriş yapmış kullanıcının planlarını listeler
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkoutPlanSummaryDto>>> GetMyWorkoutPlans()
    {
        var userId = GetCurrentUserId();
        var plans = await _context.WorkoutPlans
            .Where(wp => wp.UserId == userId)
            .Select(wp => new WorkoutPlanSummaryDto(
                wp.Id,
                wp.Name,
                wp.Description,
                wp.CreatedAt,
                wp.PlanExercises.Count() // Plandaki egzersiz sayısı
            ))
            .ToListAsync();

        return Ok(plans);
    }

    // GET: api/workoutplans/{id}
    // Belirli bir planın detaylarını getirir (kullanıcıya aitse)
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkoutPlanDetailDto>> GetWorkoutPlanById(int id)
    {
        var userId = GetCurrentUserId();
        var plan = await _context.WorkoutPlans
            .Include(wp => wp.PlanExercises) // İlişkili egzersizleri çek
                .ThenInclude(pe => pe.Exercise) // Egzersiz isimlerini almak için
            .Where(wp => wp.Id == id && wp.UserId == userId)
            .Select(wp => new WorkoutPlanDetailDto(
                wp.Id,
                wp.Name,
                wp.Description,
                wp.CreatedAt,
                wp.PlanExercises.Select(pe => new PlanExerciseDetailDto(
                    pe.Id,
                    pe.ExerciseId,
                    pe.Exercise.Name, // Egzersiz adı
                    pe.Sets,
                    pe.Reps,
                    pe.RestDurationSeconds,
                    pe.OrderInPlan
                )).OrderBy(ped => ped.OrderInPlan).ToList() // Sıraya göre listele
            ))
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { Message = "Antrenman planı bulunamadı veya size ait değil." });
        }

        return Ok(plan);
    }

    // POST: api/workoutplans
    // Yeni bir antrenman planı oluşturur
    [HttpPost]
    public async Task<ActionResult<WorkoutPlanSummaryDto>> CreateWorkoutPlan([FromBody] CreateWorkoutPlanDto createDto)
    {
        var userId = GetCurrentUserId();
        var plan = new WorkoutPlan
        {
            UserId = userId,
            Name = createDto.Name,
            Description = createDto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.WorkoutPlans.Add(plan);
        await _context.SaveChangesAsync();

        var summaryDto = new WorkoutPlanSummaryDto(plan.Id, plan.Name, plan.Description, plan.CreatedAt, 0);
        // Oluşturulan planı döndür (201 Created)
        return CreatedAtAction(nameof(GetWorkoutPlanById), new { id = plan.Id }, summaryDto);
    }

    // PUT: api/workoutplans/{id}
    // Mevcut bir planı günceller (kullanıcıya aitse)
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkoutPlan(int id, [FromBody] UpdateWorkoutPlanDto updateDto)
    {
        var userId = GetCurrentUserId();
        var planToUpdate = await _context.WorkoutPlans
            .FirstOrDefaultAsync(wp => wp.Id == id && wp.UserId == userId);

        if (planToUpdate == null)
        {
            return NotFound(new { Message = "Güncellenecek plan bulunamadı veya size ait değil." });
        }

        planToUpdate.Name = updateDto.Name;
        planToUpdate.Description = updateDto.Description;
        // planToUpdate.UpdatedAt = DateTime.UtcNow; // Eğer UpdatedAt alanı varsa

        await _context.SaveChangesAsync();

        return NoContent(); // Başarılı güncelleme
    }

    // DELETE: api/workoutplans/{id}
    // Bir planı siler (kullanıcıya aitse)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkoutPlan(int id)
    {
        var userId = GetCurrentUserId();
        var planToDelete = await _context.WorkoutPlans
            .Include(wp => wp.PlanExercises) // İlişkili egzersizleri de silmek için (Cascade ayarına bağlı)
            .FirstOrDefaultAsync(wp => wp.Id == id && wp.UserId == userId);

        if (planToDelete == null)
        {
            return NotFound(new { Message = "Silinecek plan bulunamadı veya size ait değil." });
        }

        _context.WorkoutPlans.Remove(planToDelete); // Cascade ayarı varsa PlanExercises de silinir
        await _context.SaveChangesAsync();

        return NoContent(); // Başarılı silme
    }

    // --- Plana Egzersiz Ekleme/Güncelleme/Silme ---

    // POST: api/workoutplans/{planId}/exercises
    // Bir plana yeni egzersiz ekler
    [HttpPost("{planId}/exercises")]
    public async Task<ActionResult<PlanExerciseDetailDto>> AddExerciseToPlan(int planId, [FromBody] AddOrUpdatePlanExerciseDto exerciseDto)
    {
        var userId = GetCurrentUserId();
        var plan = await _context.WorkoutPlans
                         .Include(p => p.PlanExercises) // Mevcut egzersizleri kontrol etmek için
                         .FirstOrDefaultAsync(wp => wp.Id == planId && wp.UserId == userId);

        if (plan == null)
        {
            return NotFound(new { Message = "Antrenman planı bulunamadı veya size ait değil." });
        }

        // Egzersiz veritabanında var mı?
        var exerciseExists = await _context.Exercises.AnyAsync(e => e.Id == exerciseDto.ExerciseId);
        if (!exerciseExists)
        {
            return BadRequest(new { Message = "Geçersiz Egzersiz ID'si." });
        }

        // Aynı egzersiz planda zaten var mı? (İsteğe bağlı kontrol)
        if (plan.PlanExercises.Any(pe => pe.ExerciseId == exerciseDto.ExerciseId))
        {
            return BadRequest(new { Message = "Bu egzersiz zaten planda mevcut." });
        }

        var planExercise = new PlanExercise
        {
            WorkoutPlanId = planId,
            ExerciseId = exerciseDto.ExerciseId,
            Sets = exerciseDto.Sets,
            Reps = exerciseDto.Reps,
            RestDurationSeconds = exerciseDto.RestDurationSeconds,
            OrderInPlan = exerciseDto.OrderInPlan ?? (plan.PlanExercises.Count > 0 ? plan.PlanExercises.Max(pe => pe.OrderInPlan ?? 0) + 1 : 1) // Otomatik sıra
        };

        _context.PlanExercises.Add(planExercise);
        await _context.SaveChangesAsync();

        // Eklenen egzersizin detayını döndür
        var exercise = await _context.Exercises.FindAsync(planExercise.ExerciseId);
        var detailDto = new PlanExerciseDetailDto(
            planExercise.Id,
            planExercise.ExerciseId,
            exercise?.Name ?? "Bilinmeyen Egzersiz",
            planExercise.Sets,
            planExercise.Reps,
            planExercise.RestDurationSeconds,
            planExercise.OrderInPlan
        );

        // Oluşturulan kaynağın konumunu ve kendisini döndür
        // Tam URI için daha karmaşık bir yapı gerekir, şimdilik sadece DTO dönelim.
        return Ok(detailDto); // Veya CreatedAtAction benzeri
    }

    // PUT: api/workoutplans/{planId}/exercises/{planExerciseId}
    // Plandaki bir egzersizin detaylarını günceller
    [HttpPut("{planId}/exercises/{planExerciseId}")]
    public async Task<IActionResult> UpdateExerciseInPlan(int planId, int planExerciseId, [FromBody] AddOrUpdatePlanExerciseDto updateDto)
    {
        var userId = GetCurrentUserId();
        var planExerciseToUpdate = await _context.PlanExercises
            .Include(pe => pe.WorkoutPlan) // Planın kullanıcıya ait olduğunu doğrulamak için
            .FirstOrDefaultAsync(pe => pe.Id == planExerciseId && pe.WorkoutPlanId == planId && pe.WorkoutPlan.UserId == userId);

        if (planExerciseToUpdate == null)
        {
            return NotFound(new { Message = "Plandaki egzersiz kaydı bulunamadı veya size ait değil." });
        }

        // Egzersiz ID'si değişiyorsa yeni ID'nin geçerli olduğunu kontrol et
        if (planExerciseToUpdate.ExerciseId != updateDto.ExerciseId)
        {
            var exerciseExists = await _context.Exercises.AnyAsync(e => e.Id == updateDto.ExerciseId);
            if (!exerciseExists)
            {
                return BadRequest(new { Message = "Geçersiz Egzersiz ID'si." });
            }
            planExerciseToUpdate.ExerciseId = updateDto.ExerciseId;
        }

        planExerciseToUpdate.Sets = updateDto.Sets;
        planExerciseToUpdate.Reps = updateDto.Reps;
        planExerciseToUpdate.RestDurationSeconds = updateDto.RestDurationSeconds;
        planExerciseToUpdate.OrderInPlan = updateDto.OrderInPlan;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/workoutplans/{planId}/exercises/{planExerciseId}
    // Bir egzersizi plandan kaldırır
    [HttpDelete("{planId}/exercises/{planExerciseId}")]
    public async Task<IActionResult> RemoveExerciseFromPlan(int planId, int planExerciseId)
    {
        var userId = GetCurrentUserId();
        var planExerciseToDelete = await _context.PlanExercises
            .Include(pe => pe.WorkoutPlan)
            .FirstOrDefaultAsync(pe => pe.Id == planExerciseId && pe.WorkoutPlanId == planId && pe.WorkoutPlan.UserId == userId);

        if (planExerciseToDelete == null)
        {
            return NotFound(new { Message = "Plandaki egzersiz kaydı bulunamadı veya size ait değil." });
        }

        _context.PlanExercises.Remove(planExerciseToDelete);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}