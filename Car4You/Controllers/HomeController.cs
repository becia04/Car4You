using Car4You.DAL;
using Car4You.Data;
using Car4You.Helper;
using Car4You.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using QRCoder;
using SelectPdf;
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
        private readonly IWebHostEnvironment _environment;

        public HomeController(ILogger<HomeController> logger, CarDbContext context, AppDbContext appcontext, ViewRenderService viewRenderService, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _appcontext = appcontext;
            _viewRenderService = viewRenderService;
            _environment = environment;
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
                .Include(c => c.CarEquipments)
                    .ThenInclude(ce => ce.Equipment)
                    .ThenInclude(e => e.EquipmentType)
                .Include(c => c.Photos)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null)
                return NotFound();

            // 🔹 Dane pomocnicze
            ViewBag.EnginePowerKW = car.EnginePower.HasValue
                ? Math.Round(car.EnginePower.Value * 0.74)
                : 0;

            // 🔹 Generowanie QR
            string carUrl = Url.Action(
                "Details",
                "Home",
                new { id = car.Id },
                Request.Scheme
            )!;

            string tempDir = Path.Combine(_environment.WebRootPath, "temp");
            Directory.CreateDirectory(tempDir);

            string qrFilePath = Path.Combine(tempDir, $"qr_{car.Id}.png");

            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrData = qrGenerator.CreateQrCode(carUrl, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrData);
                System.IO.File.WriteAllBytes(qrFilePath, qrCode.GetGraphic(20));
            }

            // 🔹 Render Razor → HTML
            var html = await _viewRenderService.RenderToStringAsync(
                ControllerContext,
                "DetailsPdf",
                car
            );

            // 🔹 Konwerter PDF (SelectPdf)
            var converter = new HtmlToPdf();

            converter.Options.PdfPageSize = PdfPageSize.A4;
            converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
            converter.Options.MarginTop = 10;
            converter.Options.MarginBottom = 10;
            converter.Options.MarginLeft = 10;
            converter.Options.MarginRight = 10;

            // 🔑 KLUCZOWE – base URL dla /css, /icons, /temp
            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var doc = converter.ConvertHtmlString(html, baseUrl);

            byte[] pdf = doc.Save();
            doc.Close();

            // 🔹 Sprzątanie QR po zakończeniu odpowiedzi
            Response.OnCompleted(() =>
            {
                try
                {
                    if (System.IO.File.Exists(qrFilePath))
                        System.IO.File.Delete(qrFilePath);
                }
                catch { }

                return Task.CompletedTask;
            });

            string fileName =
                $"{car.CarModel.Brand.Name}_{car.CarModel.Name}_{car.Version?.Name}_{car.Year}.pdf";

            return File(pdf, "application/pdf", fileName);
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
