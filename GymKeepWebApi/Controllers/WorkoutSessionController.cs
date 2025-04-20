using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymKeepWebApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/users/{userId}/[controller]")] // /api/users/{userId}/workoutsessions
public class WorkoutSessionController : ControllerBase
{
    private readonly MyDbContext _context;

    public WorkoutSessionController(MyDbContext context)
    {
        _context = context;
    }

    // GET: api/users/{userId}/workoutsessions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkoutSession>>> GetWorkoutSessions(int userId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        if (!await UserExists(userId)) return NotFound($"User {userId} not found.");

        var query = _context.WorkoutSessions
                            .Where(ws => ws.UserId == userId)
                            .Include(ws => ws.WorkoutPlan) // Takip edilen planı yükle (opsiyonel)
                            .OrderByDescending(ws => ws.SessionDate) // En yeniden eskiye sırala
                            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(ws => ws.SessionDate >= startDate.Value.Date); // Tarihin başlangıcı
        }
        if (endDate.HasValue)
        {
            // Bitiş tarihinin sonunu dahil etmek için +1 gün ekleyip küçük kontrolü yapın
            query = query.Where(ws => ws.SessionDate < endDate.Value.Date.AddDays(1));
        }


        return await query.ToListAsync();
    }

    // GET: api/users/{userId}/workoutsessions/{sessionId}
    [HttpGet("{sessionId}")]
    public async Task<ActionResult<WorkoutSession>> GetWorkoutSession(int userId, int sessionId)
    {
        var workoutSession = await _context.WorkoutSessions
                                           .Include(ws => ws.WorkoutPlan)
                                           .Include(ws => ws.User) // Kullanıcıyı yükle (opsiyonel)
                                                                   // SessionExercises'leri de burada yüklemek isteyebilirsiniz
                                                                   // .Include(ws => ws.SessionExercises)
                                                                   //    .ThenInclude(se => se.Exercise)
                                           .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == sessionId && ws.UserId == userId);

        if (workoutSession == null)
        {
            return NotFound($"Session {sessionId} not found for user {userId}.");
        }

        return workoutSession;
    }

    // POST: api/users/{userId}/workoutsessions
    [HttpPost]
    public async Task<ActionResult<WorkoutSession>> CreateWorkoutSession(int userId, WorkoutSession workoutSession)
    {
        if (!await UserExists(userId)) return NotFound($"User {userId} not found.");

        // Opsiyonel: Eğer bir WorkoutPlanId verildiyse, o planın kullanıcıya ait olup olmadığını kontrol et
        if (workoutSession.WorkoutPlanId.HasValue &&
           !await _context.WorkoutPlans.AnyAsync(wp => wp.WorkoutPlanId == workoutSession.WorkoutPlanId.Value && wp.UserId == userId))
        {
            return BadRequest($"Workout plan {workoutSession.WorkoutPlanId} not found or does not belong to user {userId}.");
        }

        workoutSession.UserId = userId;
        // workoutSession.SessionDate zaten default olarak DateTime.UtcNow alıyor (modelde)

        _context.WorkoutSessions.Add(workoutSession);
        await _context.SaveChangesAsync();

        // İlişkili verileri yükle ve döndür
        await _context.Entry(workoutSession).Reference(ws => ws.User).LoadAsync();
        if (workoutSession.WorkoutPlanId.HasValue) await _context.Entry(workoutSession).Reference(ws => ws.WorkoutPlan).LoadAsync();


        return CreatedAtAction(nameof(GetWorkoutSession), new { userId = userId, sessionId = workoutSession.WorkoutSessionId }, workoutSession);
    }

    // PUT: api/users/{userId}/workoutsessions/{sessionId}
    [HttpPut("{sessionId}")]
    public async Task<IActionResult> UpdateWorkoutSession(int userId, int sessionId, WorkoutSession workoutSession)
    {
        if (sessionId != workoutSession.WorkoutSessionId) return BadRequest("Session ID mismatch.");

        if (!await _context.WorkoutSessions.AsNoTracking().AnyAsync(ws => ws.WorkoutSessionId == sessionId && ws.UserId == userId))
        {
            return NotFound($"Session {sessionId} not found for user {userId}.");
        }

        // Opsiyonel: Eğer WorkoutPlanId değiştirildiyse, yeni planın kullanıcıya ait olup olmadığını kontrol et
        var originalSession = await _context.WorkoutSessions.AsNoTracking().FirstOrDefaultAsync(ws => ws.WorkoutSessionId == sessionId);
        if (workoutSession.WorkoutPlanId != originalSession?.WorkoutPlanId && workoutSession.WorkoutPlanId.HasValue &&
           !await _context.WorkoutPlans.AnyAsync(wp => wp.WorkoutPlanId == workoutSession.WorkoutPlanId.Value && wp.UserId == userId))
        {
            return BadRequest($"Workout plan {workoutSession.WorkoutPlanId} not found or does not belong to user {userId}.");
        }


        workoutSession.UserId = userId; // Tekrar set et
        _context.Entry(workoutSession).State = EntityState.Modified;
        // UserId değiştirilmemeli
        _context.Entry(workoutSession).Property(x => x.UserId).IsModified = false;


        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await WorkoutSessionExists(sessionId, userId)) return NotFound();
            else throw;
        }

        return NoContent();
    }

    // DELETE: api/users/{userId}/workoutsessions/{sessionId}
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteWorkoutSession(int userId, int sessionId)
    {
        var workoutSession = await _context.WorkoutSessions.FirstOrDefaultAsync(ws => ws.WorkoutSessionId == sessionId && ws.UserId == userId);
        if (workoutSession == null)
        {
            return NotFound($"Session {sessionId} not found for user {userId}.");
        }

        _context.WorkoutSessions.Remove(workoutSession); // Cascade delete SessionExercises ve SetLogs'u silmeli
        await _context.SaveChangesAsync();

        return NoContent();
    }


    // --- SessionExercise Yönetimi ---

    // GET: api/users/{userId}/workoutsessions/{sessionId}/exercises
    [HttpGet("{sessionId}/exercises")]
    public async Task<ActionResult<IEnumerable<SessionExercise>>> GetSessionExercises(int userId, int sessionId)
    {
        if (!await WorkoutSessionExists(sessionId, userId))
        {
            return NotFound($"Session {sessionId} not found for user {userId}.");
        }

        return await _context.SessionExercises
                             .Where(se => se.WorkoutSessionId == sessionId)
                             .Include(se => se.Exercise) // Egzersiz detaylarını yükle
                                .ThenInclude(ex => ex.ExerciseRegion)
                             .Include(se => se.Exercise)
                                .ThenInclude(ex => ex.DifficultyLevel)
                             .Include(se => se.PlanExercise) // Hangi plan egzersizi (opsiyonel)
                             .OrderBy(se => se.OrderInSession)
                             .ToListAsync();
    }

    // GET: api/users/{userId}/workoutsessions/{sessionId}/exercises/{sessionExerciseId}
    [HttpGet("{sessionId}/exercises/{sessionExerciseId}")]
    public async Task<ActionResult<SessionExercise>> GetSessionExercise(int userId, int sessionId, int sessionExerciseId)
    {
        if (!await WorkoutSessionExists(sessionId, userId))
        {
            return NotFound($"Session {sessionId} not found for user {userId}.");
        }

        var sessionExercise = await _context.SessionExercises
                                            .Include(se => se.Exercise)
                                            .Include(se => se.PlanExercise)
                                            .FirstOrDefaultAsync(se => se.SessionExerciseId == sessionExerciseId && se.WorkoutSessionId == sessionId);

        if (sessionExercise == null)
        {
            return NotFound($"Session exercise {sessionExerciseId} not found in session {sessionId}.");
        }
        return sessionExercise;
    }


    // POST: api/users/{userId}/workoutsessions/{sessionId}/exercises
    [HttpPost("{sessionId}/exercises")]
    public async Task<ActionResult<SessionExercise>> AddExerciseToSession(int userId, int sessionId, SessionExercise sessionExercise)
    {
        if (!await WorkoutSessionExists(sessionId, userId))
        {
            return NotFound($"Session {sessionId} not found for user {userId}.");
        }

        // ExerciseId geçerli mi?
        if (!await _context.Exercises.AnyAsync(ex => ex.ExerciseId == sessionExercise.ExerciseId))
        {
            return BadRequest($"Exercise with ID {sessionExercise.ExerciseId} not found.");
        }

        // PlanExerciseId verildiyse geçerli mi ve o seansın planına mı ait? (İleri seviye kontrol)
        if (sessionExercise.PlanExerciseId.HasValue)
        {
            var session = await _context.WorkoutSessions.FindAsync(sessionId);
            if (session?.WorkoutPlanId == null || !await _context.PlanExercises.AnyAsync(pe => pe.PlanExerciseId == sessionExercise.PlanExerciseId.Value && pe.WorkoutPlanId == session.WorkoutPlanId))
            {
                return BadRequest($"PlanExercise {sessionExercise.PlanExerciseId} is not valid for the plan associated with session {sessionId}.");
            }
        }


        sessionExercise.WorkoutSessionId = sessionId;

        _context.SessionExercises.Add(sessionExercise);
        await _context.SaveChangesAsync();

        await _context.Entry(sessionExercise).Reference(se => se.Exercise).LoadAsync(); // İlişkiyi yükle

        return CreatedAtAction(nameof(GetSessionExercise), new { userId, sessionId, sessionExerciseId = sessionExercise.SessionExerciseId }, sessionExercise);
    }

    // PUT: api/users/{userId}/workoutsessions/{sessionId}/exercises/{sessionExerciseId}
    [HttpPut("{sessionId}/exercises/{sessionExerciseId}")]
    public async Task<IActionResult> UpdateSessionExercise(int userId, int sessionId, int sessionExerciseId, SessionExercise sessionExercise)
    {
        if (sessionExerciseId != sessionExercise.SessionExerciseId || sessionId != sessionExercise.WorkoutSessionId) return BadRequest("ID mismatch.");

        if (!await WorkoutSessionExists(sessionId, userId))
        {
            return NotFound($"Session {sessionId} not found for user {userId}.");
        }

        // Gerekirse ExerciseId, PlanExerciseId geçerlilik kontrolleri eklenebilir

        _context.Entry(sessionExercise).State = EntityState.Modified;
        _context.Entry(sessionExercise).Property(x => x.WorkoutSessionId).IsModified = false; // Ana ID değiştirilemez

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.SessionExercises.AnyAsync(se => se.SessionExerciseId == sessionExerciseId && se.WorkoutSessionId == sessionId)) return NotFound();
            else throw;
        }

        return NoContent();
    }

    // DELETE: api/users/{userId}/workoutsessions/{sessionId}/exercises/{sessionExerciseId}
    [HttpDelete("{sessionId}/exercises/{sessionExerciseId}")]
    public async Task<IActionResult> RemoveExerciseFromSession(int userId, int sessionId, int sessionExerciseId)
    {
        if (!await WorkoutSessionExists(sessionId, userId))
        {
            return NotFound($"Session {sessionId} not found for user {userId}.");
        }

        var sessionExercise = await _context.SessionExercises.FirstOrDefaultAsync(se => se.SessionExerciseId == sessionExerciseId && se.WorkoutSessionId == sessionId);
        if (sessionExercise == null)
        {
            return NotFound($"Session exercise {sessionExerciseId} not found in session {sessionId}.");
        }

        _context.SessionExercises.Remove(sessionExercise); // Cascade delete SetLogs'u silmeli
        await _context.SaveChangesAsync();

        return NoContent();
    }


    // --- SetLog Yönetimi ---

    // GET: api/users/{userId}/workoutsessions/{sessionId}/exercises/{sessionExerciseId}/setlogs
    [HttpGet("{sessionId}/exercises/{sessionExerciseId}/setlogs")]
    public async Task<ActionResult<IEnumerable<SetLog>>> GetSetLogs(int userId, int sessionId, int sessionExerciseId)
    {
        // Zincirleme kontrol: User -> Session -> SessionExercise
        if (!await WorkoutSessionExists(sessionId, userId)) return NotFound($"Session {sessionId} not found for user {userId}.");
        if (!await _context.SessionExercises.AnyAsync(se => se.SessionExerciseId == sessionExerciseId && se.WorkoutSessionId == sessionId)) return NotFound($"Session exercise {sessionExerciseId} not found in session {sessionId}.");


        return await _context.SetLogs
                             .Where(sl => sl.SessionExerciseId == sessionExerciseId)
                             .OrderBy(sl => sl.SetNumber)
                             .ToListAsync();
    }

    // GET: api/users/{userId}/workoutsessions/{sessionId}/exercises/{sessionExerciseId}/setlogs/{setLogId}
    [HttpGet("{sessionId}/exercises/{sessionExerciseId}/setlogs/{setLogId}")]
    public async Task<ActionResult<SetLog>> GetSetLog(int userId, int sessionId, int sessionExerciseId, int setLogId)
    {
        if (!await WorkoutSessionExists(sessionId, userId)) return NotFound($"Session {sessionId} not found for user {userId}.");

        var setLog = await _context.SetLogs
                                   .FirstOrDefaultAsync(sl => sl.SetLogId == setLogId && sl.SessionExerciseId == sessionExerciseId);

        // SessionExercise'in doğru seansa ait olduğunu da kontrol etmek daha güvenli olur
        if (setLog == null || !await _context.SessionExercises.AnyAsync(se => se.SessionExerciseId == sessionExerciseId && se.WorkoutSessionId == sessionId))
        {
            return NotFound($"Set log {setLogId} not found for session exercise {sessionExerciseId}.");
        }

        return setLog;
    }


    // POST: api/users/{userId}/workoutsessions/{sessionId}/exercises/{sessionExerciseId}/setlogs
    [HttpPost("{sessionId}/exercises/{sessionExerciseId}/setlogs")]
    public async Task<ActionResult<SetLog>> AddSetLog(int userId, int sessionId, int sessionExerciseId, SetLog setLog)
    {
        if (!await WorkoutSessionExists(sessionId, userId)) return NotFound($"Session {sessionId} not found for user {userId}.");
        if (!await _context.SessionExercises.AnyAsync(se => se.SessionExerciseId == sessionExerciseId && se.WorkoutSessionId == sessionId)) return NotFound($"Session exercise {sessionExerciseId} not found in session {sessionId}.");

        setLog.SessionExerciseId = sessionExerciseId;
        // CreatedAt null olabilir veya otomatik atanabilir

        _context.SetLogs.Add(setLog);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSetLog), new { userId, sessionId, sessionExerciseId, setLogId = setLog.SetLogId }, setLog);
    }

    // PUT: api/users/{userId}/workoutsessions/{sessionId}/exercises/{sessionExerciseId}/setlogs/{setLogId}
    [HttpPut("{sessionId}/exercises/{sessionExerciseId}/setlogs/{setLogId}")]
    public async Task<IActionResult> UpdateSetLog(int userId, int sessionId, int sessionExerciseId, int setLogId, SetLog setLog)
    {
        if (setLogId != setLog.SetLogId || sessionExerciseId != setLog.SessionExerciseId) return BadRequest("ID mismatch.");

        // Kontrol zinciri
        if (!await WorkoutSessionExists(sessionId, userId)) return NotFound($"Session {sessionId} not found for user {userId}.");
        if (!await _context.SessionExercises.AnyAsync(se => se.SessionExerciseId == sessionExerciseId && se.WorkoutSessionId == sessionId)) return NotFound($"Session exercise {sessionExerciseId} not found in session {sessionId}.");

        _context.Entry(setLog).State = EntityState.Modified;
        _context.Entry(setLog).Property(x => x.SessionExerciseId).IsModified = false;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.SetLogs.AnyAsync(sl => sl.SetLogId == setLogId && sl.SessionExerciseId == sessionExerciseId)) return NotFound();
            else throw;
        }

        return NoContent();
    }


    // DELETE: api/users/{userId}/workoutsessions/{sessionId}/exercises/{sessionExerciseId}/setlogs/{setLogId}
    [HttpDelete("{sessionId}/exercises/{sessionExerciseId}/setlogs/{setLogId}")]
    public async Task<IActionResult> DeleteSetLog(int userId, int sessionId, int sessionExerciseId, int setLogId)
    {
        // Kontrol zinciri
        if (!await WorkoutSessionExists(sessionId, userId)) return NotFound($"Session {sessionId} not found for user {userId}.");
        if (!await _context.SessionExercises.AnyAsync(se => se.SessionExerciseId == sessionExerciseId && se.WorkoutSessionId == sessionId)) return NotFound($"Session exercise {sessionExerciseId} not found in session {sessionId}.");

        var setLog = await _context.SetLogs.FirstOrDefaultAsync(sl => sl.SetLogId == setLogId && sl.SessionExerciseId == sessionExerciseId);
        if (setLog == null) return NotFound($"Set log {setLogId} not found.");

        _context.SetLogs.Remove(setLog);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    private async Task<bool> UserExists(int userId)
    {
        return await _context.Users.AnyAsync(e => e.UserId == userId);
    }
    private async Task<bool> WorkoutSessionExists(int sessionId, int userId)
    {
        return await _context.WorkoutSessions.AnyAsync(e => e.WorkoutSessionId == sessionId && e.UserId == userId);
    }
}