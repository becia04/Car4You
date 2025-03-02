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

        // GET: Cars/Create
        public IActionResult Create()
        {
            ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name");
            ViewData["BodyTypes"] = new SelectList(_context.BodyTypes, "Id", "Name");
            ViewData["FuelTypes"] = new SelectList(_context.FuelTypes, "Id", "Name");
            ViewData["Gearboxes"] = new SelectList(_context.Gearboxes, "Id", "Name");

            return View();
        }

        // POST: Cars/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car)
        {
            if (ModelState.IsValid)
            {
                _context.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Przekierowanie do listy aut
            }

            // Jeśli są błędy, ponownie wypełniamy listy dla dropdownów
            ViewData["Brands"] = new SelectList(_context.Brands, "Id", "Name");
            ViewData["BodyTypes"] = new SelectList(_context.BodyTypes, "Id", "Name");
            ViewData["FuelTypes"] = new SelectList(_context.FuelTypes, "Id", "Name");
            ViewData["Gearboxes"] = new SelectList(_context.Gearboxes, "Id", "Name");

            return View(car);
        }

        public IActionResult Index()
        {
            return View();
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
