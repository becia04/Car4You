using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using Car4You.DAL;
using Car4You.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using Microsoft.EntityFrameworkCore;

namespace Car4You.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CarDbContext _context;

        public HomeController(ILogger<HomeController> logger, CarDbContext context)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var cars = _context.Cars
    .Include(c => c.Version)
            .Include(c=>c.Photos)
            .Include(b=>b.BodyTypes)
            .Include(m=>m.CarModel)
            .ThenInclude(b=>b.Brand)
    .ToList();
           
            return View(cars);
        }

        public async Task<IActionResult> Details(int id)
        {
            var car = await _context.Cars
                .Include(c => c.BodyTypes)
                .Include(c => c.CarModel).ThenInclude(m => m.Brand)
                .Include(c => c.Version)
                .Include(c => c.CarEquipments)
                .Include(c => c.Photos)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsHidden);

            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
