using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymKeepWebApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ExerciseController : ControllerBase
{
    private readonly MyDbContext _context;

    public ExerciseController(MyDbContext context)
    {
        _context = context;
    }

    // GET: api/exercises
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Exercise>>> GetExercises(
        [FromQuery] int? difficultyLevelId = null,
        [FromQuery] int? regionId = null)
    {
        var query = _context.Exercises
                            .Include(e => e.DifficultyLevel) // İlişkili veriyi yükle
                            .Include(e => e.ExerciseRegion)  // İlişkili veriyi yükle
                            .AsQueryable();

        if (difficultyLevelId.HasValue)
        {
            query = query.Where(e => e.DifficultyLevelId == difficultyLevelId.Value);
        }

        if (regionId.HasValue)
        {
            query = query.Where(e => e.RegionId == regionId.Value);
        }

        return await query.ToListAsync();
    }

    // GET: api/exercises/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Exercise>> GetExercise(int id)
    {
        var exercise = await _context.Exercises
                                     .Include(e => e.DifficultyLevel)
                                     .Include(e => e.ExerciseRegion)
                                     .FirstOrDefaultAsync(e => e.ExerciseId == id);

        if (exercise == null)
        {
            return NotFound();
        }

        return exercise;
    }

    // POST: api/exercises
    [HttpPost]
    public async Task<ActionResult<Exercise>> CreateExercise(Exercise exercise)
    {
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetExercise), new { id = exercise.ExerciseId }, exercise);
    }

    // PUT: api/exercises/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExercise(int id, Exercise exercise)
    {
        if (id != exercise.ExerciseId)
        {
            return BadRequest();
        }

        _context.Entry(exercise).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ExerciseExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/exercises/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        var exercise = await _context.Exercises.FindAsync(id);
        if (exercise == null)
        {
            return NotFound();
        }

        // İlişkili olduğu PlanExercise veya SessionExercise varsa silme işlemi
        // OnDelete Behavior'a göre hata verebilir (Restrict ise).
        try
        {
            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) // FK kısıtlaması hatası
        {
            return BadRequest($"Cannot delete exercise. It might be used in workout plans or sessions. Error: {ex.InnerException?.Message ?? ex.Message}");
        }


        return NoContent();
    }

    private bool ExerciseExists(int id)
    {
        return _context.Exercises.Any(e => e.ExerciseId == id);
    }
}