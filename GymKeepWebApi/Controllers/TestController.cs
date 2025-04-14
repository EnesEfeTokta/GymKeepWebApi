using Microsoft.AspNetCore.Mvc;

namespace GymKeepWebApi.Controllers
{
    public class TestController : Controller
    {
        private readonly MyDbContext _context;

        public TestController(MyDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var data = _context.Users.ToList();
            return View(data);
        }
    }
}
