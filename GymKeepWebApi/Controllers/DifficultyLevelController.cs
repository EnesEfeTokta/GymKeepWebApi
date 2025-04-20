using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymKeepWebApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class DifficultyLevelController : ControllerBase
{
    private readonly MyDbContext _context;

    public DifficultyLevelController(MyDbContext context)
    {
        _context = context;
    }

    // GET: api/difficultylevels
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DifficultyLevel>>> GetDifficultyLevels()
    {
        return await _context.DifficultyLevels.ToListAsync();
    }

    // GET: api/difficultylevels/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<DifficultyLevel>> GetDifficultyLevel(int id)
    {
        var difficultyLevel = await _context.DifficultyLevels.FindAsync(id);
        if (difficultyLevel == null) return NotFound();
        return difficultyLevel;
    }
    // Genellikle lookup tabloları için POST/PUT/DELETE adminlere özel olur.
}