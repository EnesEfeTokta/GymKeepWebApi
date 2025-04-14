using GymKeepWebApi.Models;
using GymKeepWebApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/exerciseregions")] // Çoğul isim
public class ExerciseRegionController : ControllerBase
{
    private readonly MyDbContext _context;

    public ExerciseRegionController(MyDbContext context)
    {
        _context = context;
    }

    // GET: api/exerciseregions
    [HttpGet]
    [AllowAnonymous] // Herkes bölgeleri görebilir
    public async Task<ActionResult<IEnumerable<ExerciseRegionDto>>> GetExerciseRegions()
    {
        var regions = await _context.ExerciseRegions
            .Select(er => new ExerciseRegionDto(er.Id, er.Name))
            .ToListAsync();
        return Ok(regions);
    }
}