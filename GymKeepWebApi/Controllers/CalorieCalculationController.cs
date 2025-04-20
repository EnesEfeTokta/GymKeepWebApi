using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymKeepWebApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/users/{userId}/[controller]")] // api/users/{userId}/caloriecalculations
public class CalorieCalculationController : ControllerBase
{
    private readonly MyDbContext _context;

    public CalorieCalculationController(MyDbContext context)
    {
        _context = context;
    }

    // GET: api/users/{userId}/caloriecalculations
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalorieCalculation>>> GetCalorieCalculations(int userId)
    {
        if (!await _context.Users.AnyAsync(u => u.UserId == userId)) return NotFound($"User {userId} not found.");

        return await _context.CalorieCalculations
                             .Where(cc => cc.UserId == userId)
                             .OrderByDescending(cc => cc.CalculationDate)
                             .ToListAsync();
    }

    // GET: api/users/{userId}/caloriecalculations/{calcId}
    [HttpGet("{calcId}")]
    public async Task<ActionResult<CalorieCalculation>> GetCalorieCalculation(int userId, int calcId)
    {
        var calculation = await _context.CalorieCalculations
                                        .FirstOrDefaultAsync(cc => cc.CalorieCalculationId == calcId && cc.UserId == userId);

        if (calculation == null) return NotFound($"Calculation {calcId} not found for user {userId}.");
        return calculation;
    }

    // POST: api/users/{userId}/caloriecalculations
    [HttpPost]
    public async Task<ActionResult<CalorieCalculation>> CreateCalorieCalculation(int userId, CalorieCalculation calculation)
    {
        if (!await _context.Users.AnyAsync(u => u.UserId == userId)) return NotFound($"User {userId} not found.");

        calculation.UserId = userId;
        calculation.CalculationDate = DateTime.UtcNow; // Tarihi sunucuda set et

        _context.CalorieCalculations.Add(calculation);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCalorieCalculation), new { userId = userId, calcId = calculation.CalorieCalculationId }, calculation);
    }

    // DELETE: api/users/{userId}/caloriecalculations/{calcId}
    [HttpDelete("{calcId}")]
    public async Task<IActionResult> DeleteCalorieCalculation(int userId, int calcId)
    {
        var calculation = await _context.CalorieCalculations
                                       .FirstOrDefaultAsync(cc => cc.CalorieCalculationId == calcId && cc.UserId == userId);

        if (calculation == null) return NotFound($"Calculation {calcId} not found for user {userId}.");

        _context.CalorieCalculations.Remove(calculation);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    // Güncelleme (PUT) genellikle bu tür kayıtlar için mantıklı olmayabilir, yenisi oluşturulur.
}