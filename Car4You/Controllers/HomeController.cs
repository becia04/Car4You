using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using Car4You.DAL;
using Car4You.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

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

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Index()
        {
            var cars = _context.Cars
            .Include(c => c.Version)
            .Include(c=>c.Photos)
            .Include(b=>b.BodyTypes)
            .Include(m=>m.CarModel)
                .ThenInclude(b=>b.Brand)
            .OrderByDescending(c=>c.PublishDate)
    .ToList();
            ViewData["AnimateMode"] = "scroll-animations"; // dla innych widoków

            return View(cars);
        }

        public IActionResult CarList(string sortOrder)
        {
            var cars = _context.Cars
         .Include(c => c.CarModel).ThenInclude(cm => cm.Brand)
         .Include(c => c.BodyTypes)
         .Include(c => c.Photos)
         .Include(c => c.Version)
         .AsQueryable();

            switch (sortOrder)
            {
                case "brand_model":
                    cars = cars.OrderBy(c => c.CarModel.Brand.Name).ThenBy(c => c.CarModel.Name);
                    break;
                case "year":
                    cars = cars.OrderByDescending(c => c.Year);
                    break;
                case "price":
                    cars = cars.OrderBy(c => c.NewPrice);
                    break;
                default:
                    cars = cars.OrderByDescending(c => c.PublishDate); // Załóżmy, że masz takie pole
                    break;
            }

            return View(cars.ToList());
        }

        public IActionResult SortedCars(string sortOrder, string sortDir)
        {
            var cars = _context.Cars
                .Include(c => c.CarModel).ThenInclude(cm => cm.Brand)
                .Include(c => c.BodyTypes)
                .Include(c => c.Photos)
                .Include(c => c.Version)
                .AsQueryable();

            bool ascending = sortDir == "asc";

            switch (sortOrder)
            {
                case "brand_model":
                    cars = ascending
                        ? cars.OrderBy(c => c.CarModel.Brand.Name).ThenBy(c => c.CarModel.Name)
                        : cars.OrderByDescending(c => c.CarModel.Brand.Name).ThenByDescending(c => c.CarModel.Name);
                    break;
                case "year":
                    cars = ascending ? cars.OrderBy(c => c.Year) : cars.OrderByDescending(c => c.Year);
                    break;
                case "price":
                    cars = ascending ? cars.OrderBy(c => c.NewPrice) : cars.OrderByDescending(c => c.NewPrice);
                    break;
                default:
                    cars = ascending ? cars.OrderBy(c => c.PublishDate) : cars.OrderByDescending(c => c.PublishDate);
                    break;
            }

            return PartialView("_CarListPartial", cars.ToList());
        }


        public async Task<IActionResult> Details(int id)
        {
            var car = await _context.Cars
                .Include(c => c.BodyTypes)
                .Include(c => c.CarModel).ThenInclude(m => m.Brand)
                .Include(c => c.Version)
                .Include(c => c.CarEquipments).ThenInclude(c=>c.Equipment).ThenInclude(c=>c.EquipmentType)
                .Include(c => c.Photos)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsHidden);

            if (car == null)
            {
                return NotFound();
            }

            double enginePowerKW = car.EnginePower.HasValue ? Math.Round(car.EnginePower.Value * 0.74) : 0;
            ViewBag.EnginePowerKW = enginePowerKW;

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
