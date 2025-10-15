using Car4You.DAL;
using Car4You.Data;
using Car4You.Helper;
using Car4You.Models;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Car4You.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CarDbContext _context;
        private readonly AppDbContext _appcontext;
        private readonly ViewRenderService _viewRenderService;
        private readonly IConverter _converter;

        public HomeController(ILogger<HomeController> logger, CarDbContext context, AppDbContext appcontext, ViewRenderService viewRenderService, IConverter converter)
        {
            _context = context;
            _logger = logger;
            _appcontext = appcontext;
            _viewRenderService = viewRenderService;
            _converter = converter;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> HideCar(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null)
                return NotFound();

            // Przełączamy wartość IsHidden
            car.IsHidden = !car.IsHidden;
            await _context.SaveChangesAsync();

            // Ustawiamy komunikat w zależności od stanu
            TempData["Message"] = car.IsHidden
                ? "Samochód został ukryty."
                : "Samochód został ponownie widoczny.";

            // Przekierowanie z powrotem do szczegółów auta
            return RedirectToAction("Details", "Home", new { id = car.Id });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportToPdf(int id)
        {
            var car = await _context.Cars
                .Include(c => c.BodyTypes)
                .Include(c => c.CarModel).ThenInclude(m => m.Brand)
                .Include(c => c.Version)
                .Include(c => c.CarEquipments).ThenInclude(c => c.Equipment).ThenInclude(c => c.EquipmentType)
                .Include(c => c.Photos)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null)
                return NotFound();

            // 🔹 Generowanie kodu QR
            string carUrl = Url.Action("Details", "Home", new { id = car.Id }, Request.Scheme);
            string tempDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");
            Directory.CreateDirectory(tempDir);

            string qrFilePath = Path.Combine(tempDir, $"qr_{car.Id}.png");
            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            {
                var qrData = qrGenerator.CreateQrCode(carUrl, QRCoder.QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCoder.PngByteQRCode(qrData);
                var qrBytes = qrCode.GetGraphic(20);
                System.IO.File.WriteAllBytes(qrFilePath, qrBytes);
            }

            // 🔹 Render widoku PDF
            var html = await _viewRenderService.RenderToStringAsync(ControllerContext, "DetailsPdf", car);

            // 🔹 Ustawienia PDF
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4
                },
                Objects =
        {
            new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = html,
                WebSettings = new WebSettings
                {
                    DefaultEncoding = "utf-8",
                    LoadImages = true,
                    EnableJavascript = true,
                    UserStyleSheet = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "pdf_export.css")
                }
            }
        }
            };

            // 🔹 Wygeneruj PDF w pamięci
            var file = _converter.Convert(doc);

            // 🔹 Zarejestruj czyszczenie plików tymczasowych po wysłaniu odpowiedzi
            Response.OnCompleted(() =>
            {
                try
                {
                    if (System.IO.File.Exists(qrFilePath))
                        System.IO.File.Delete(qrFilePath);
                }
                catch { /* Ignorujemy błędy */ }

                return Task.CompletedTask;
            });

            // 🔹 Zwróć gotowy plik
            string fileName = $"{car.CarModel.Brand.Name}_{car.CarModel.Name}_{car.Version?.Name}_{car.Year}.pdf";
            return File(file, "application/pdf", fileName);
        }



        public IActionResult About()
        {
            return View();
        }

        public IActionResult Rental()
        {
            var setting = _appcontext.SiteSettings.FirstOrDefault(s => s.Key == "RentalVisible");

            if (setting == null || setting.Value != "true")
            {
                TempData["RentalMessage"] = "Aktualnie nie wynajmujemy.";
                return RedirectToAction("Index", "Home");
            }

            return View(); // normalny widok
        }

        public IActionResult Contact()
        {
            return View();
        }

        public async Task<IActionResult> Index()
        {
            bool isAdmin = User.IsInRole("Admin"); // sprawdzenie roli

            var carsQuery = _context.Cars
    .Include(c => c.Version)
    .Include(c => c.Photos)
    .Include(c => c.BodyTypes)
    .Include(c => c.CarModel).ThenInclude(b => b.Brand)
    .AsQueryable();

            if (!isAdmin)
            {
                carsQuery = carsQuery.Where(c => !c.IsHidden);
            }

            carsQuery = carsQuery.OrderByDescending(c => c.PublishDate);

            ViewData["AnimateMode"] = "scroll-animations"; // dla innych widoków
            var cars = await carsQuery.ToListAsync();
            return View(cars);
        }

        public async Task<IActionResult> CarList(string sortOrder)
        {
            bool isAdmin = User.IsInRole("Admin");

            var carsQuery = _context.Cars
                .Include(c => c.CarModel).ThenInclude(cm => cm.Brand)
                .Include(c => c.BodyTypes)
                .Include(c => c.Photos)
                .Include(c => c.Version)
                .AsQueryable();

            // najpierw filtrujemy
            if (!isAdmin)
            {
                carsQuery = carsQuery.Where(c => !c.IsHidden);
            }

            // potem sortujemy
            carsQuery = sortOrder switch
            {
                "brand_model" => carsQuery
                    .OrderBy(c => c.CarModel.Brand.Name ?? "")
                    .ThenBy(c => c.CarModel.Name ?? ""),

                "year" => carsQuery
                    .OrderByDescending(c => c.Year),

                "price" => carsQuery
                    .OrderBy(c => c.NewPrice),

                _ => carsQuery
                    .OrderByDescending(c => c.PublishDate)
            };

            var cars = await carsQuery.ToListAsync();
            return View(cars);
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
            bool isAdmin = User.IsInRole("Admin"); // sprawdzenie roli

            var carQuery = _context.Cars
                .Include(c => c.BodyTypes)
                .Include(c => c.CarModel).ThenInclude(m => m.Brand)
                .Include(c => c.Version)
                .Include(c => c.CarEquipments).ThenInclude(c => c.Equipment).ThenInclude(c => c.EquipmentType)
                .Include(c => c.Photos)
                .AsQueryable();

            if (!isAdmin)
            {
                carQuery = carQuery.Where(c => !c.IsHidden);
            }

            var car = await carQuery.FirstOrDefaultAsync(c => c.Id == id);

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
