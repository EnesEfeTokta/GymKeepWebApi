using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymKeepWebApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ExerciseRegionController : ControllerBase
{
    private readonly MyDbContext _context;

    public ExerciseRegionController(MyDbContext context)
    {
        _context = context;
    }

    // GET: api/exerciseregions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExerciseRegion>>> GetExerciseRegions()
    {
        return await _context.ExerciseRegions.ToListAsync();
    }

    // GET: api/exerciseregions/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ExerciseRegion>> GetExerciseRegion(int id)
    {
        var region = await _context.ExerciseRegions.FindAsync(id);
        if (region == null) return NotFound();
        return region;
    }
    // Genellikle lookup tabloları için POST/PUT/DELETE adminlere özel olur.
}