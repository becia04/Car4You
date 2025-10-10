using Car4You.DAL;
using Car4You.Data;
using Car4You.Helper;
using Car4You.Models;
using DinkToPdf;
using DinkToPdf.Contracts;
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

            // Generowanie kodu QR
            string carUrl = Url.Action("Details", "Home", new { id = car.Id }, Request.Scheme);

            string qrFileName = $"qr_{car.Id}.png";
            string qrFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp", qrFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(qrFilePath));

            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            {
                var qrData = qrGenerator.CreateQrCode(carUrl, QRCoder.QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCoder.PngByteQRCode(qrData);
                var qrBytes = qrCode.GetGraphic(20);
                System.IO.File.WriteAllBytes(qrFilePath, qrBytes);
            }

            // Render widoku Details do HTML
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var html = await _viewRenderService.RenderToStringAsync(ControllerContext,"DetailsPdf", car);

            // Ustawienia PDF
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
        ColorMode = ColorMode.Color,
        Orientation = Orientation.Portrait,
        PaperSize = PaperKind.A4
    },
                Objects = {
        new ObjectSettings() {
            PagesCount = true,
            HtmlContent = html,
            WebSettings = {
                DefaultEncoding = "utf-8",
                UserStyleSheet = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "css", "pdf_export.css"),
                LoadImages = true,
                EnableJavascript = true
            }
        }
    }
            };

            var file = _converter.Convert(doc);
            return File(file, "application/pdf", $"{car.CarModel.Brand.Name}_{car.CarModel.Name}_{car.Version?.Name}_{car.Year}.pdf");
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
