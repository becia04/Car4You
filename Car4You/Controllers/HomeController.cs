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
        private readonly IWebHostEnvironment _environment;

        public HomeController(ILogger<HomeController> logger, CarDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        public IActionResult CreateEquipment()
        {
            ViewBag.EquipmentTypes = _context.EquipmentTypes.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult CreateEquipment(Equipment equipment, IFormFile imageFile)
        {
            bool state = true;
            if (equipment.Name == null)
            {
                ModelState.AddModelError("Name", "Nazwa jest wymagana");
                state = false;
            }
            else
            {
                if (_context.Equipments.Any(e => e.Name == equipment.Name))
                {
                    ModelState.AddModelError("Name", "Nazwa już istnieje!");
                }
            }
            if (imageFile != null && imageFile.Length > 0)
            {
                string normalizedFileName = NormalizeFileName(equipment.Name);
                string fileExtension = Path.GetExtension(imageFile.FileName);
                string finalFileName = normalizedFileName + fileExtension;
                string uploadDir = Path.Combine(_environment.WebRootPath, "equipment");

                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                string filePath = Path.Combine(uploadDir, finalFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }

                // **Przypisujemy Icon przed walidacją**
                equipment.Icon = "/equipment/" + finalFileName;
            }
            else
            {
                state = false;
                ModelState.AddModelError("Icon", "Ikona jest wymagana");
            }
            if(equipment.EquipmentTypeId==null)
            {
                ModelState.AddModelError("EquipmentTypeId", "Musisz wybrać typ");
                state=false;
            }
            if (state==false)
            {
                ViewBag.EquipmentTypes = _context.EquipmentTypes.ToList();
                return View(equipment);
                
            }

            _context.Equipments.Add(equipment);
            _context.SaveChanges();
            return RedirectToAction("CreateEquipment");

        }

        private string NormalizeFileName(string name)
        {
            string normalized = name.ToLower();
            normalized = Regex.Replace(normalized.Normalize(NormalizationForm.FormD), @"[\u0300-\u036f]", ""); // Usunięcie akcentów
            normalized = normalized.Replace(" ", "_"); // Zamiana spacji na _
            normalized = Regex.Replace(normalized, @"[^a-z0-9_]", ""); // Usunięcie niedozwolonych znaków
            return normalized;
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
            var equipments = _context.Equipments
                .Include(a=>a.EquipmentType)
                .ToList();
            return View(equipments);
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
