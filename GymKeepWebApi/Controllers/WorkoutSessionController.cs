using GymKeepWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using GymKeepWebApi.Dtos;

[ApiController]
[Route("api/workoutsessions")]
[Authorize]
public class WorkoutSessionController : ControllerBase
{
    private readonly MyDbContext _context;

    public WorkoutSessionController(MyDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out int userId)) { return userId; }
        throw new UnauthorizedAccessException("Kullanıcı kimliği alınamadı.");
    }

    // POST: api/workoutsessions/start
    // Yeni bir antrenman seansı başlatır
    [HttpPost("start")]
    public async Task<ActionResult<WorkoutSessionSummaryDto>> StartSession([FromBody] StartWorkoutSessionDto startDto)
    {
        var userId = GetCurrentUserId();
        string? planName = null;

        // Eğer plan ID'si verildiyse, planın kullanıcıya ait olup olmadığını kontrol et
        if (startDto.WorkoutPlanId.HasValue)
        {
            var plan = await _context.WorkoutPlans
                .Where(wp => wp.Id == startDto.WorkoutPlanId.Value && wp.UserId == userId)
                .Select(wp => new { wp.Name }) // Sadece ismi alalım
                .FirstOrDefaultAsync();
            if (plan == null)
            {
                return BadRequest(new { Message = "Belirtilen antrenman planı bulunamadı veya size ait değil." });
            }
            planName = plan.Name;
        }

        var newSession = new WorkoutSession
        {
            UserId = userId,
            SessionDate = DateTime.UtcNow,
            WorkoutPlanId = startDto.WorkoutPlanId,
            Notes = startDto.Notes
            // DurationMinutes başlangıçta null
        };

        _context.WorkoutSessions.Add(newSession);
        await _context.SaveChangesAsync();

        // Eğer bir plandan başlatıldıysa, plandaki egzersizleri seansa otomatik ekle (opsiyonel)
        if (startDto.WorkoutPlanId.HasValue)
        {
            var planExercises = await _context.PlanExercises
                .Where(pe => pe.WorkoutPlanId == startDto.WorkoutPlanId.Value)
                .OrderBy(pe => pe.OrderInPlan)
                .ToListAsync();

            foreach (var planEx in planExercises)
            {
                var sessionExercise = new SessionExercise
                {
                    WorkoutSessionId = newSession.Id,
                    ExerciseId = planEx.ExerciseId,
                    PlanExerciseId = planEx.Id, // Planla bağlantı
                    OrderInSession = planEx.OrderInPlan
                };
                _context.SessionExercises.Add(sessionExercise);

                // Plandaki set sayısı kadar boş SetLog oluştur (opsiyonel)
                for (int i = 1; i <= planEx.Sets; i++)
                {
                    _context.SetLogs.Add(new SetLog
                    {
                        SessionExerciseId = sessionExercise.Id, // ID atanmasını beklemeden eklemek sorun olabilir, SaveChanges sonrası yapılmalı
                        SetNumber = i,
                        IsCompleted = false
                    });
                }
            }
            await _context.SaveChangesAsync(); // Egzersizleri ve boş setleri kaydet
        }


        var summaryDto = new WorkoutSessionSummaryDto(
            newSession.Id,
            newSession.SessionDate,
            newSession.DurationMinutes,
            newSession.Notes,
            newSession.WorkoutPlanId,
            planName,
            await _context.SessionExercises.CountAsync(se => se.WorkoutSessionId == newSession.Id), // Egzersiz sayısı
            0 // Başlangıçta tamamlanan set 0
        );

        // Oluşturulan seansın özetini döndür
        return Ok(summaryDto); // Veya CreatedAtAction
    }

    // GET: api/workoutsessions
    // Kullanıcının geçmiş antrenman seanslarını listeler
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkoutSessionSummaryDto>>> GetMySessions()
    {
        var userId = GetCurrentUserId();
        var sessions = await _context.WorkoutSessions
            .Where(ws => ws.UserId == userId)
            .Include(ws => ws.WorkoutPlan) // Plan adını almak için
            .Include(ws => ws.SessionExercises) // Egzersiz sayısını almak için
                .ThenInclude(se => se.SetLogs) // Tamamlanan set sayısını almak için
            .OrderByDescending(ws => ws.SessionDate) // En yeniden eskiye sırala
            .Select(ws => new WorkoutSessionSummaryDto(
                ws.Id,
                ws.SessionDate,
                ws.DurationMinutes,
                ws.Notes,
                ws.WorkoutPlanId,
                ws.WorkoutPlan != null ? ws.WorkoutPlan.Name : null,
                ws.SessionExercises.Count,
                ws.SessionExercises.SelectMany(se => se.SetLogs).Count(sl => sl.IsCompleted) // Tamamlanan set sayısı
            ))
            .ToListAsync();

        return Ok(sessions);
    }

    // GET: api/workoutsessions/{sessionId}
    // Belirli bir seansın tüm detaylarını getirir
    [HttpGet("{sessionId}")]
    public async Task<ActionResult<WorkoutSessionDetailDto>> GetSessionDetails(int sessionId)
    {
        var userId = GetCurrentUserId();
        var sessionDetail = await _context.WorkoutSessions
            .Where(ws => ws.Id == sessionId && ws.UserId == userId)
            .Include(ws => ws.WorkoutPlan)
            .Include(ws => ws.SessionExercises.OrderBy(se => se.OrderInSession)) // Sıralı egzersizler
                .ThenInclude(se => se.Exercise) // Egzersiz adı için
            .Include(ws => ws.SessionExercises.OrderBy(se => se.OrderInSession))
                .ThenInclude(se => se.SetLogs.OrderBy(sl => sl.SetNumber)) // Sıralı setler
            .Select(ws => new WorkoutSessionDetailDto(
                ws.Id,
                ws.SessionDate,
                ws.DurationMinutes,
                ws.Notes,
                ws.WorkoutPlanId,
                ws.WorkoutPlan != null ? ws.WorkoutPlan.Name : null,
                ws.SessionExercises.Select(se => new SessionExerciseDetailDto(
                    se.Id,
                    se.ExerciseId,
                    se.Exercise.Name,
                    se.OrderInSession,
                    se.SetLogs.Select(sl => new SetLogDto(
                        sl.Id,
                        sl.SetNumber,
                        sl.Weight,
                        sl.RepsCompleted,
                        sl.IsCompleted,
                        sl.CompletedAt
                    )).ToList()
                )).ToList()
            ))
            .FirstOrDefaultAsync();

        if (sessionDetail == null)
        {
            return NotFound(new { Message = "Antrenman seansı bulunamadı veya size ait değil." });
        }

        return Ok(sessionDetail);
    }

    // POST: api/workoutsessions/{sessionId}/exercises
    // Devam eden bir seansa egzersiz ekler
    [HttpPost("{sessionId}/exercises")]
    public async Task<ActionResult<SessionExerciseDetailDto>> AddExerciseToSession(int sessionId, [FromBody] AddSessionExerciseDto addDto)
    {
        var userId = GetCurrentUserId();
        // Seans kullanıcıya ait mi kontrol et
        var sessionExists = await _context.WorkoutSessions.AnyAsync(ws => ws.Id == sessionId && ws.UserId == userId);
        if (!sessionExists)
        {
            return NotFound(new { Message = "Antrenman seansı bulunamadı veya size ait değil." });
        }
        // Egzersiz var mı kontrol et
        var exercise = await _context.Exercises.FindAsync(addDto.ExerciseId);
        if (exercise == null)
        {
            return BadRequest(new { Message = "Geçersiz Egzersiz ID'si." });
        }

        var maxOrder = await _context.SessionExercises
                                   .Where(se => se.WorkoutSessionId == sessionId)
                                   .MaxAsync(se => (int?)se.OrderInSession) ?? 0;

        var sessionExercise = new SessionExercise
        {
            WorkoutSessionId = sessionId,
            ExerciseId = addDto.ExerciseId,
            OrderInSession = addDto.OrderInSession ?? (maxOrder + 1)
            // PlanExerciseId burada null olacak
        };

        _context.SessionExercises.Add(sessionExercise);
        await _context.SaveChangesAsync();

        var dto = new SessionExerciseDetailDto(
            sessionExercise.Id,
            sessionExercise.ExerciseId,
            exercise.Name,
            sessionExercise.OrderInSession,
            new List<SetLogDto>() // Başlangıçta boş set listesi
        );

        return Ok(dto); // Veya CreatedAtAction
    }


    // POST: api/workoutsessions/{sessionId}/exercises/{sessionExerciseId}/sets
    // Bir seans egzersizi için set loglar (tamamlandı olarak işaretler/günceller)
    [HttpPost("{sessionId}/exercises/{sessionExerciseId}/sets")]
    public async Task<ActionResult<SetLogDto>> LogSet(int sessionId, int sessionExerciseId, [FromBody] LogSetDto logDto)
    {
        var userId = GetCurrentUserId();
        // SessionExercise'in kullanıcıya ait olduğunu doğrula (Session üzerinden)
        var sessionExercise = await _context.SessionExercises
            .Include(se => se.WorkoutSession)
            .FirstOrDefaultAsync(se => se.Id == sessionExerciseId && se.WorkoutSessionId == sessionId && se.WorkoutSession.UserId == userId);

        if (sessionExercise == null)
        {
            return NotFound(new { Message = "Seans egzersiz kaydı bulunamadı veya size ait değil." });
        }

        // Mevcut set logunu bul veya yenisini oluştur
        var setLog = await _context.SetLogs
            .FirstOrDefaultAsync(sl => sl.SessionExerciseId == sessionExerciseId && sl.SetNumber == logDto.SetNumber);

        if (setLog == null) // Eğer set önceden oluşturulmadıysa (plandan gelmediyse)
        {
            setLog = new SetLog
            {
                SessionExerciseId = sessionExerciseId,
                SetNumber = logDto.SetNumber
            };
            _context.SetLogs.Add(setLog);
        }

        // Bilgileri güncelle
        setLog.Weight = logDto.Weight;
        setLog.RepsCompleted = logDto.RepsCompleted;
        setLog.IsCompleted = logDto.IsCompleted;
        setLog.CompletedAt = logDto.IsCompleted ? DateTime.UtcNow : (DateTime?)null; // Tamamlandıysa zamanı kaydet

        await _context.SaveChangesAsync();

        var dto = new SetLogDto(
            setLog.Id,
            setLog.SetNumber,
            setLog.Weight,
            setLog.RepsCompleted,
            setLog.IsCompleted,
            setLog.CompletedAt
        );
        return Ok(dto);
    }


    // PUT: api/workoutsessions/{sessionId}/end
    // Bir seansı bitirir (süreyi, notları günceller)
    [HttpPut("{sessionId}/end")]
    public async Task<IActionResult> EndSession(int sessionId, [FromBody] EndWorkoutSessionDto endDto)
    {
        var userId = GetCurrentUserId();
        var sessionToEnd = await _context.WorkoutSessions
                                   .FirstOrDefaultAsync(ws => ws.Id == sessionId && ws.UserId == userId);

        if (sessionToEnd == null)
        {
            return NotFound(new { Message = "Antrenman seansı bulunamadı veya size ait değil." });
        }

        sessionToEnd.DurationMinutes = endDto.DurationMinutes ?? (int)(DateTime.UtcNow - sessionToEnd.SessionDate).TotalMinutes; // Otomatik süre veya DTO'dan gelen
        sessionToEnd.Notes = endDto.Notes ?? sessionToEnd.Notes; // Yeni not veya mevcutu koru

        await _context.SaveChangesAsync();
        return NoContent();
    }


    // DELETE: api/workoutsessions/{sessionId}
    // Bir antrenman seansını tamamen siler
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteSession(int sessionId)
    {
        var userId = GetCurrentUserId();
        var sessionToDelete = await _context.WorkoutSessions
            .Include(ws => ws.SessionExercises) // İlişkili egzersizleri yükle
                .ThenInclude(se => se.SetLogs) // İlişkili set loglarını yükle
            .FirstOrDefaultAsync(ws => ws.Id == sessionId && ws.UserId == userId);

        if (sessionToDelete == null)
        {
            return NotFound(new { Message = "Antrenman seansı bulunamadı veya size ait değil." });
        }

        // Önce bağlı SetLog'ları silmek gerekebilir (Cascade yoksa)
        // _context.SetLogs.RemoveRange(sessionToDelete.SessionExercises.SelectMany(se => se.SetLogs));
        // Sonra SessionExercise'leri silmek gerekebilir (Cascade yoksa)
        // _context.SessionExercises.RemoveRange(sessionToDelete.SessionExercises);
        // En son Session'ı sil
        _context.WorkoutSessions.Remove(sessionToDelete); // Cascade varsa hepsi gider

        await _context.SaveChangesAsync();
        return NoContent();
    }
}