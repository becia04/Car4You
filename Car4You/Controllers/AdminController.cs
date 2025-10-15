using Car4You.DAL;
using Car4You.Data;
using Car4You.Helper;
using Car4You.Helpers;
using Car4You.Models;
using Car4You.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;
using static Car4You.ViewModels.CarViewModel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Car4You.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly CarDbContext _context;
        private readonly AppDbContext _appcontext;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;
        private readonly PhotoUploadHelper _photoHelper;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(CarDbContext context, AppDbContext appcontext, IWebHostEnvironment environment, ILogger<AdminController> logger, PhotoUploadHelper photoHelper, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _appcontext = appcontext;
            _environment = environment;
            _logger = logger;
            _photoHelper = photoHelper;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Wyświetla wszystkich użytkowników
        public IActionResult Users()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // Usuwanie użytkownika
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            await _userManager.DeleteAsync(user);
            TempData["Message"] = "Użytkownik został usunięty.";
            return RedirectToAction(nameof(Users));
        }

        // Przełączanie roli admina
        [HttpPost]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            else
                await _userManager.AddToRoleAsync(user, "Admin");

            TempData["Message"] = "Rola admina została zaktualizowana.";
            return RedirectToAction(nameof(Users));
        }

        public IActionResult Index()
        {
            var setting = _appcontext.SiteSettings.FirstOrDefault(s => s.Key == "RentalVisible");
            bool isVisible = setting != null && setting.Value == "true";
            ViewBag.RentalVisible = isVisible;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleRental()
        {
            var setting = _appcontext.SiteSettings.FirstOrDefault(s => s.Key == "RentalVisible");

            if (setting == null)
            {
                setting = new SiteSetting { Key = "RentalVisible", Value = "true" };
                _appcontext.SiteSettings.Add(setting);
            }
            else
            {
                setting.Value = (setting.Value == "true") ? "false" : "true";
            }

            _appcontext.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> AddCar()
        {
            var equipmentTypes = _context.EquipmentTypes.OrderBy(e => e.Name).ToList();  // Pobranie typów ekwipunku
            var equipmentList = _context.Equipments.Include(e => e.EquipmentType).OrderBy(e => e.Name).ToList(); // Pobranie ekwipunków z ich typami

            // Grupowanie ekwipunków według typu
            var groupedEquipment = equipmentTypes.Select(equipmentType => new EquipmentGroup
            {
                EquipmentType = equipmentType,
                Equipments = equipmentList.Where(e => e.EquipmentTypeId == equipmentType.Id).ToList()
            }).ToList();

            var viewModel = new CarViewModel
            {
                Car = new Car(), // Nowy obiekt auta
                Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync(),
                CarModels = new List<Models.CarModel>(),
                BodyTypes = await _context.BodyTypes.OrderBy(b => b.Name).ToListAsync(),
                Versions = new List<Models.Version>(),
                EquipmentTypes = equipmentTypes,
                GroupedEquipment = groupedEquipment,
            };


            return View("AddCar", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCar(CarViewModel model, List<IFormFile> carPhotos)
        {
            if (model.Car == null)
                model.Car = new Car();

            try
            {
                model.Car.PublishDate = DateTime.Now;
                model.Car.CarModel = null;
                model.Car.NewPrice = model.Car.OldPrice;

                // Zapisz auto do bazy
                _context.Cars.Add(model.Car);
                await _context.SaveChangesAsync();

                // --- Wyposażenie ---
                if (model.SelectedEquipmentIds != null && model.SelectedEquipmentIds.Any())
                {
                    var carEquipments = model.SelectedEquipmentIds
                        .Select(eid => new CarEquipment { CarId = model.Car.Id, EquipmentId = eid })
                        .ToList();

                    _context.CarEquipments.AddRange(carEquipments);
                }

                // --- Zdjęcia ---
                var files = Request.Form.Files; // ASP.NET zawsze wypełnia to pole, nawet jeśli carPhotos jest puste

                int mainPhotoIndex = 0;
                int.TryParse(Request.Form["MainPhotoIndex"], out mainPhotoIndex);

                if (files != null && files.Count > 0)
                {
                    var photos = new List<Photo>();

                    var car = await _context.Cars
                        .Include(c => c.CarModel)
                        .ThenInclude(m => m.Brand)
                        .FirstOrDefaultAsync(c => c.Id == model.Car.Id);

                    if (car == null)
                    {
                        _logger.LogError($"Nie znaleziono auta o ID {model.Car.Id}");
                        return StatusCode(500, "Błąd zapisu zdjęć");
                    }

                    string brand = car.CarModel?.Brand?.Name ?? "BrakMarki";
                    string modelName = car.CarModel?.Name ?? "BrakModelu";
                    string generation = car.CarModel?.Versions?.FirstOrDefault()?.Name ?? "";
                    string year = car.Year.ToString();

                    string carsFolder = Path.Combine(_environment.WebRootPath, "cars");
                    if (!Directory.Exists(carsFolder))
                        Directory.CreateDirectory(carsFolder);

                    var logoPath = Path.Combine(_environment.WebRootPath, "logo.png");

                    for (int i = 0; i < files.Count; i++)
                    {
                        var file = files[i];
                        string originalFileName =
                            $"{car.Id}_{brand}_{modelName}{(string.IsNullOrEmpty(generation) ? "" : $"_{generation}")}_{year}_{i + 1}";
                        var fileNameWithoutExtension = FileHelper.NormalizeFileName(originalFileName);
                        var fileName = $"{fileNameWithoutExtension}.jpg";
                        var targetPath = Path.Combine(carsFolder, fileName);

                        try
                        {
                            // Wczytaj oryginalny plik do pamięci
                            using (var ms = new MemoryStream())
                            {
                                await file.CopyToAsync(ms);
                                ms.Position = 0;

                                // Nakładanie logo bez zapisu tymczasowego pliku
                                using (var processedStream = await PhotoUploadHelper.OverlayLogoStreamAsync(ms, logoPath))
                                {
                                    // Kompresja i zapis gotowego pliku do docelowej ścieżki
                                    await _photoHelper.CompressToMaxSizeAsync(processedStream, targetPath, 140 * 1024);
                                }
                            }

                            photos.Add(new Photo
                            {
                                CarId = car.Id,
                                Title = fileName,
                                PhotoPath = $"/cars/{fileName}",
                                IsMain = (i == mainPhotoIndex)
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Błąd podczas przetwarzania zdjęcia {file.FileName}");
                        }
                    }

                    _context.Photos.AddRange(photos);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Auto zostało dodane.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd w AddCar");
                return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> EditCar(int id)
        {
            var car = await _context.Cars
                .Include(c => c.BodyTypes)
                .Include(c => c.Photos)
                .Include(c => c.Version)
                .Include(c => c.CarEquipments)
                .Include(c => c.CarModel)
                    .ThenInclude(cm => cm.Versions)
                .Include(c => c.CarModel)
                    .ThenInclude(cm => cm.Brand)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null) return NotFound();

            var equipmentTypes = await _context.EquipmentTypes.OrderBy(e => e.Name).ToListAsync();
            var equipmentList = await _context.Equipments
                .Include(e => e.EquipmentType)
                .OrderBy(e => e.Name)
                .ToListAsync();

            var groupedEquipment = equipmentTypes.Select(equipmentType => new EquipmentGroup
            {
                EquipmentType = equipmentType,
                Equipments = equipmentList.Where(e => e.EquipmentTypeId == equipmentType.Id).ToList()
            }).ToList();

            // Przygotowanie podglądu zdjęć kompatybilnego z JS (SavedPhotoPaths)
            var savedPhotoPaths = car.Photos.Select(p => new TempPhoto
            {
                Src = p.PhotoPath,  // pełna ścieżka lub względna URL
                IsMain = p.IsMain
            }).ToList();

            var viewModel = new CarViewModel
            {
                Car = car,
                Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync(),
                CarModels = await _context.CarModels
                    .Where(m => m.BrandId == car.CarModel.BrandId)
                    .OrderBy(m => m.Name)
                    .ToListAsync(),
                BodyTypes = await _context.BodyTypes.OrderBy(b => b.Name).ToListAsync(),
                Versions = await _context.Versions
                    .Where(v => v.CarModelId == car.CarModelId)
                    .OrderBy(v => v.Name)
                    .ToListAsync(),
                EquipmentTypes = equipmentTypes,
                GroupedEquipment = groupedEquipment,
                SelectedEquipmentIds = car.CarEquipments.Select(ce => ce.EquipmentId).ToList(),
                SavedPhotoPaths = savedPhotoPaths  // ← tutaj podgląd do JS
            };

            return View("EditCar", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCar(CarViewModel model)
        {
            if (model.Car == null)
                return BadRequest("Brak danych auta");

            try
            {
                // --- Pobierz istniejące auto z bazy ---
                var car = await _context.Cars
                    .Include(c => c.CarEquipments)
                    .Include(c => c.Photos)
                    .Include(c => c.CarModel)
                        .ThenInclude(m => m.Brand)
                    .FirstOrDefaultAsync(c => c.Id == model.Car.Id);

                if (car == null)
                    return NotFound();

                // --- Nowa data ogłoszenia ---
                if (model.NewPublishDate) // ✅ checkbox zaznaczony
                {
                    car.PublishDate = DateTime.Now;
                }

                // --- Aktualizacja pól auta ---
                car.Title = model.Car.Title;
                car.Year = model.Car.Year;
                car.EnginePower = model.Car.EnginePower;
                car.Mileage = model.Car.Mileage;
                car.CubicCapacity = model.Car.CubicCapacity;
                car.OldPrice = model.Car.OldPrice;
                if(model.Car.NewPrice!=null && model.Car.NewPrice!=model.Car.OldPrice)
                {
                    car.NewPrice = model.Car.NewPrice;
                }
                else
                {
                    car.NewPrice = model.Car.OldPrice;
                }
                car.FirstOwner = model.Car.FirstOwner;
                car.PolishPlate = model.Car.PolishPlate;
                car.AccidentFree = model.Car.AccidentFree;
                car.CarModelId = model.Car.CarModelId;
                car.VersionId = model.Car.VersionId;
                car.BodyId = model.Car.BodyId;
                car.Color = model.Car.Color;
                car.FuelType = model.Car.FuelType;
                car.ColorType = model.Car.ColorType;
                car.NextTechnicalBad = model.Car.NextTechnicalBad;
                car.Description = model.Car.Description;
                car.NextOc=model.Car.NextOc;
                car.VIN = model.Car.VIN;
                car.Door = model.Car.Door;
                car.Seat = model.Car.Seat;
                car.Drive = model.Car.Drive;
                car.Gearbox = model.Car.Gearbox;
                car.Origin = model.Car.Origin;
                car.PriceType = model.Car.PriceType;

                _context.Cars.Update(car);
                await _context.SaveChangesAsync();


                // --- Wyposażenie ---
                // Usuń stare powiązania
                _context.CarEquipments.RemoveRange(car.CarEquipments);

                if (model.SelectedEquipmentIds != null && model.SelectedEquipmentIds.Any())
                {
                    var newEquipments = model.SelectedEquipmentIds
                        .Select(eid => new CarEquipment { CarId = car.Id, EquipmentId = eid })
                        .ToList();
                    _context.CarEquipments.AddRange(newEquipments);
                }

                // --- Zdjęcia (bezpieczne przeniesienie zachowanych -> temp, usunięcie reszty, potem przeniesienie z temp -> cars) ---

                // --- 1) Parsowanie listy zachowanych zdjęć (relatywne ścieżki) ---
                var savedPaths = new List<string>();
                if (Request.Form.TryGetValue("SavedPhotoPathsJson", out var savedPathsValues))
                {
                    var savedPathsJson = savedPathsValues.ToString();
                    if (!string.IsNullOrEmpty(savedPathsJson))
                    {
                        savedPaths = JsonConvert.DeserializeObject<List<string>>(savedPathsJson) ?? new List<string>();
                    }
                }

                // Upewnij się, że są relatywne i unikalne
                savedPaths = savedPaths
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.StartsWith("/") ? p : "/" + p.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string webRoot = _environment.WebRootPath;

                // --- 2) Skopiuj zachowane pliki do folderu temp ---
                string tempFolder = Path.Combine(webRoot, "temp");
                if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

                // mapa: relatywna ścieżka -> tymczasowy pełny plik (w temp)
                var retainedTempMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var relPath in savedPaths)
                {
                    try
                    {
                        var sourceFull = Path.Combine(webRoot, relPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                        if (System.IO.File.Exists(sourceFull))
                        {
                            var tempName = Guid.NewGuid().ToString("N") + "_" + Path.GetFileName(sourceFull);
                            var tempFull = Path.Combine(tempFolder, tempName);
                            System.IO.File.Copy(sourceFull, tempFull, overwrite: true);
                            retainedTempMap[relPath] = tempFull;
                        }
                        else
                        {
                            _logger.LogWarning("Saved photo path not found on disk (skipped copy to temp): {p}", sourceFull);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error copying saved photo to temp: {p}", relPath);
                    }
                }

                // --- 3) Pobierz wszystkie stare rekordy zdjęć z DB (car.Photos powinien być wcześniej załadowany) ---
                var oldPhotos = car.Photos.ToList(); // car to Twój załadowany obiekt

                // 4) Usuń wszystkie stare rekordy z DB (zgodnie z Twoim wymaganiem)
                //    lecz najpierw zapisz listę fizycznych plików do usunięcia (aby nie usunąć tych, które są skopiowane do temp)
                var filesToDelete = new List<string>();
                foreach (var p in oldPhotos)
                {
                    var rel = p.PhotoPath;
                    // jeśli relPath został skopiowany do temp, i tak chcemy usunąć oryginał po skopiowaniu.
                    var full = Path.Combine(webRoot, rel.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    filesToDelete.Add(full);
                }

                // Usuń rekordy z DB
                _context.Photos.RemoveRange(oldPhotos);
                await _context.SaveChangesAsync(); // zapisujemy usunięcie starych rekordów

                // Usuń fizyczne pliki oryginalne (wszystkie stare)
                foreach (var fp in filesToDelete)
                {
                    try
                    {
                        if (System.IO.File.Exists(fp))
                            System.IO.File.Delete(fp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Nie udało się usunąć starego pliku {p}", fp);
                    }
                }

                // --- 5) Przygotuj folder docelowy i nazewnictwo ---
                string carsFolder = Path.Combine(webRoot, "cars");
                if (!Directory.Exists(carsFolder)) Directory.CreateDirectory(carsFolder);
                var logoPath = Path.Combine(webRoot, "logo.png");

                // Pobierz aktualne nazwy marki/modelu (bazując na model.CarModelId z formularza)
                var carModelForNames = await _context.CarModels.Include(m => m.Brand).FirstOrDefaultAsync(m => m.Id == model.Car.CarModelId);
                string modelName = carModelForNames?.Name ?? "BrakModelu";
                string brandName = carModelForNames?.Brand?.Name ?? "BrakMarki";

                // --- 6) Zbuduj listę wynikową photos (najpierw przeniesione z temp -> cars, potem nowe pliki z Request.Form.Files) ---
                // --- 4️⃣ Przeniesienie zdjęć z temp i zapis nowych --- 
                if (!Directory.Exists(carsFolder))
                    Directory.CreateDirectory(carsFolder);
                var finalPhotos = new List<Photo>();

                // Pobierz markę i model
                var carModel = await _context.CarModels
                    .Include(m => m.Brand)
                    .FirstOrDefaultAsync(m => m.Id == model.Car.CarModelId);


                int photoIndex = 1;

                // --- 📸 Przenieś zachowane zdjęcia z temp w tej samej kolejności, w jakiej były w savedPaths ---
                foreach (var relPath in savedPaths)
                {
                    if (retainedTempMap.TryGetValue(relPath, out var tempFull))
                    {
                        var newFileName = $"{car.Id}_{brandName}_{modelName}_{car.Year}_{photoIndex}.jpg";
                        var newPath = Path.Combine(carsFolder, newFileName);

                        System.IO.File.Move(tempFull, newPath);

                        finalPhotos.Add(new Photo
                        {
                            CarId = car.Id,
                            Title = newFileName,
                            PhotoPath = $"/cars/{newFileName}",
                            IsMain = false
                        });

                        photoIndex++;
                    }
                }


                // --- 📸 Zapisz nowe zdjęcia (z przetwarzaniem) ---
                foreach (var file in Request.Form.Files)
                {
                    var newFileName = $"{car.Id}_{brandName}_{modelName}_{car.Year}_{photoIndex}.jpg";
                    var newPath = Path.Combine(carsFolder, newFileName);

                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        ms.Position = 0;
                        using (var processedStream = await PhotoUploadHelper.OverlayLogoStreamAsync(ms, logoPath))
                        {
                            await _photoHelper.CompressToMaxSizeAsync(processedStream, newPath, 200 * 1024);
                        }
                    }

                    finalPhotos.Add(new Photo
                    {
                        CarId = car.Id,
                        Title = newFileName,
                        PhotoPath = $"/cars/{newFileName}",
                        IsMain = false
                    });

                    photoIndex++;
                }

                // --- 🧹 Usuń pliki tymczasowe po zakończeniu ---
                foreach (var file in Directory.GetFiles(tempFolder))
                {
                    System.IO.File.Delete(file);
                }


                // --- 7) Ustawienie zdjęcia głównego zgodnie z MainPhotoIndex (index odnosi się do kolejności: savedPaths then new files) ---
                int mainPhotoIndex = 0;
                int.TryParse(Request.Form["MainPhotoIndex"], out mainPhotoIndex);
                if (mainPhotoIndex >= 0 && mainPhotoIndex < finalPhotos.Count)
                {
                   finalPhotos[mainPhotoIndex].IsMain = true;
                }

                // 8) Dodaj nowe rekordy do DB i zapisz
                _context.Photos.AddRange(finalPhotos);
                await _context.SaveChangesAsync();

                // 9) Oczyść temp (usuń wszystkie pliki tymczasowe)
                try
                {
                    if (Directory.Exists(tempFolder))
                    {
                        foreach (var f in Directory.GetFiles(tempFolder))
                        {
                            try { System.IO.File.Delete(f); } catch { /* ignoruj */ }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Problem z czyszczeniem folderu temp");
                }


                TempData["Success"] = "Auto zostało zaktualizowane.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd w EditCar");
                return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCar(int id, CancellationToken ct)
        {
            // Pobierz auto wraz z powiązaniami
            var car = await _context.Cars
                .Include(c => c.Photos)
                .Include(c => c.CarEquipments)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (car == null)
            {
                TempData["Error"] = "Nie znaleziono auta o podanym ID.";
                return RedirectToAction("CarList");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                // 🔹 1. Usuń pliki zdjęć (SavedPhotoPaths)
                if (car.Photos != null && car.Photos.Any())
                {
                    foreach (var photo in car.Photos.ToList())
                    {
                        if (!string.IsNullOrWhiteSpace(photo.PhotoPath))
                        {
                            try
                            {
                                var physicalPath = Path.Combine(_environment.WebRootPath, photo.PhotoPath.TrimStart('/', '\\'));
                                if (System.IO.File.Exists(physicalPath))
                                    System.IO.File.Delete(physicalPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Nie udało się usunąć pliku {Path}", photo.PhotoPath);
                            }
                        }
                    }

                    _context.Photos.RemoveRange(car.Photos);
                }

                // 🔹 2. Usuń powiązane rekordy z CarEquipments
                if (car.CarEquipments != null && car.CarEquipments.Any())
                {
                    _context.CarEquipments.RemoveRange(car.CarEquipments);
                }

                // 🔹 3. Usuń auto
                _context.Cars.Remove(car);

                // 🔹 4. Zapisz i zatwierdź transakcję
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                TempData["Success"] = "Auto oraz powiązane dane zostały usunięte.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Błąd podczas usuwania auta o ID {Id}", id);
                TempData["Error"] = "Wystąpił błąd podczas usuwania auta.";
            }

            return RedirectToAction("CarList", "Home");
        }


        [HttpGet]
        public JsonResult GetCarModelsByBrand(int brandId)
        {
            var carModels = _context.CarModels
                .Where(cm => cm.BrandId == brandId)
                .Select(cm => new { cm.Id, cm.Name })
                .OrderBy(cm=>cm.Name)
                .ToList();

            return Json(carModels);
        }

        [HttpGet]
        public JsonResult GetVersionsByModel(int modelId)
        {
            var versions = _context.Versions
    .Where(v => v.CarModelId == modelId)
    .OrderBy(v => v.Name)
    .Select(v => new { id = v.Id, name = v.Name })
    .ToList();

            // DEBUG: tymczasowe wypisanie na konsolę serwera
            foreach (var version in versions)
            {
                Console.WriteLine($"[{version.id}] {version.name}");
            }
            return Json(versions);
        }

        
        public IActionResult ManageBrand(int id)
        {
            var brand = _context.Brands
    .Include(b => b.CarModels)
        .ThenInclude(m => m.Versions)
    .Where(b => b.Id == id)
    .Select(b => new Brand
    {
        Id = b.Id,
        Name = b.Name,
        Icon = b.Icon,
        CarModels = b.CarModels
            .OrderBy(m => m.Name) // Sortowanie modeli alfabetycznie
            .Select(m => new Models.CarModel
            {
                Id = m.Id,
                Name = m.Name,
                BrandId = m.BrandId,
                Versions = m.Versions.OrderBy(v => v.Name).ToList() // Sortowanie wersji alfabetycznie
            })
            .ToList()
    })
    .FirstOrDefault();

            if (brand == null)
                return NotFound();

            return View(brand);
        }

        [HttpGet]
        public IActionResult GetBrands()
        {
            var brands = _context.Brands
                .Select(b => new { b.Id, b.Name })
                .ToList();

            return Json(brands);
        }
        
        [HttpPost]
        public async Task<IActionResult> EditBrandFull(int Id, string? Name, IFormFile? ImageFile)
        {
            var brand = await _context.Brands.FindAsync(Id);
            if (brand == null) return NotFound("Nie znaleziono marki.");

            try
            {
                string uploadDir = Path.Combine(_environment.WebRootPath, "brand");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                string oldIconPath = null;
                string newFileName = null;
                string newFilePath = null;
                string newIconPath = null;

                // --------------------------
                // 🔹 1. AKTUALIZACJA NAZWY
                // --------------------------
                bool nameChanged = !string.IsNullOrWhiteSpace(Name) && Name != brand.Name;

                if (nameChanged)
                {
                    // zapamiętaj starą ikonę
                    if (!string.IsNullOrEmpty(brand.Icon))
                    {
                        oldIconPath = Path.Combine(_environment.WebRootPath, brand.Icon.TrimStart('/'));

                        // wyciągamy rozszerzenie starego pliku
                        string fileExtension = Path.GetExtension(oldIconPath);
                        string normalizedFileName = FileHelper.NormalizeFileName(Name);
                        newFileName = $"{Id}_{normalizedFileName}{fileExtension}";
                        newFilePath = Path.Combine(uploadDir, newFileName);
                        newIconPath = "/brand/" + newFileName;
                    }

                    // aktualizujemy nazwę w bazie
                    brand.Name = Name;
                }

                // --------------------------
                // 🔹 2. AKTUALIZACJA IKONY
                // --------------------------
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // jeśli nie zmienialiśmy nazwy, bierzemy aktualną z bazy
                    string baseName = nameChanged ? Name : brand.Name;
                    string fileExtension = Path.GetExtension(ImageFile.FileName);
                    string normalizedFileName = FileHelper.NormalizeFileName(baseName);
                    newFileName = $"{Id}_{normalizedFileName}{fileExtension}";
                    newFilePath = Path.Combine(uploadDir, newFileName);
                    newIconPath = "/brand/" + newFileName;

                    // usuwamy stary plik
                    if (!string.IsNullOrEmpty(brand.Icon))
                    {
                        string oldFile = Path.Combine(_environment.WebRootPath, brand.Icon.TrimStart('/'));
                        if (System.IO.File.Exists(oldFile))
                            System.IO.File.Delete(oldFile);
                    }

                    // zapis nowego pliku
                    using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    brand.Icon = newIconPath;
                }
                else if (nameChanged && oldIconPath != null && System.IO.File.Exists(oldIconPath))
                {
                    // jeśli nie przesłano nowego pliku, ale zmieniono nazwę — przenosimy istniejący plik
                    System.IO.File.Move(oldIconPath, newFilePath, true);
                    brand.Icon = newIconPath;
                }

                // --------------------------
                // 🔹 3. ZAPIS DO BAZY
                // --------------------------
                _context.Brands.Update(brand);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Marka została zaktualizowana." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
                return StatusCode(500, $"Błąd podczas edycji marki: {ex.Message}");
            }
        }

        
        [HttpPost]
        public async Task<IActionResult> EditModel(int Id, string Name, int BrandId)
        {
            try
            {
                
                var carModel = await _context.CarModels.FindAsync(Id);
                if (carModel == null) return NotFound();
                // Aktualizacja rekordu
                carModel.Name = Name;
                _context.CarModels.Update(carModel);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wewnętrzny: {ex.Message}");
            }
        }
        
        [HttpPost]
        public IActionResult CreateModel(Models.CarModel carModel)
        {
            try
            {
                // Sprawdzamy, czy model już istnieje w bazie danych dla danej marki
                var existingModel = _context.CarModels
                    .FirstOrDefault(m => m.Name == carModel.Name);

                if (existingModel != null)
                {
                    // Jeśli model już istnieje, zwróć odpowiedni komunikat
                    return BadRequest("Model już istnieje.");
                }

                // Jeśli model nie istnieje, dodajemy go do bazy
                _context.CarModels.Add(carModel);
                _context.SaveChanges();

                return Ok(new { id = carModel.Id, name = carModel.Name });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wewnętrzny: {ex.Message}");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> DeleteModel(int id)
        {
            var carModel = await _context.CarModels.FindAsync(id);
            if (carModel == null) return NotFound();

            var carsUsingModel = _context.Cars
                .Where(c => c.CarModelId == id)
                .Select(c => c.Title)
                .ToList();

            var versionUsingModel = _context.Versions
                .Where(v => v.CarModelId == id)
                .Select(v => v.Name)
                .ToList();

            if (carsUsingModel.Any())
            {
                return Json(new
                {
                    success = false,
                    message = $"Nie można usunąć modelu ze względu na powiązanie z autami: {string.Join(", ", carsUsingModel)}"
                });
            }

            if (versionUsingModel.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Model ma powiązane wersje. Czy chcesz je również usunąć?",
                    versions = versionUsingModel
                });
            }

            _context.CarModels.Remove(carModel);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        
        [HttpPost]
        public async Task<IActionResult> DeleteModelWithVersions(int id)
        {
            var carModel = await _context.CarModels.Include(m => m.Versions).FirstOrDefaultAsync(m => m.Id == id);

            if (carModel == null)
            {
                return NotFound("Model nie istnieje.");
            }

            _context.Versions.RemoveRange(carModel.Versions);
            _context.CarModels.Remove(carModel);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        
        [HttpPost]
        public async Task<IActionResult> EditVersion(int Id, string Name)
        {
            var version = await _context.Versions.FindAsync(Id);
            if (version == null) return NotFound();
            try
            {
                // Aktualizacja rekordu
                version.Name = Name;
                _context.Versions.Update(version);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wewnętrzny: {ex.Message}");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> DeleteVersion(int id)
        {
            var version = await _context.Versions.FindAsync(id);
            if (version == null) return NotFound();

            // Sprawdzamy, czy model jest przypisany do auta
            var carsUsingVersion = _context.Cars
                .Include(c => c.Version)
                .Where(c => c.VersionId == id)
                .Select(c => c.Title)
                .ToList();

            if (carsUsingVersion.Any())
            {
                return BadRequest($"Nie można usunąć modelu ze względu na powiązanie z podanymi autami: {string.Join(", ", carsUsingVersion)}");
            }

            // Usuwamy rekord z bazy
            _context.Versions.Remove(version);
            await _context.SaveChangesAsync();
            return Ok();
        }
        
        [HttpPost]
        public IActionResult CreateVersion(Models.Version version)
        {
            try
            {
                // Sprawdzamy, czy generacja już istnieje w bazie danych dla danego modelu
                var existingVersion = _context.Versions
                    .FirstOrDefault(m => m.Name == version.Name && m.CarModelId==version.CarModelId);

                if (existingVersion != null)
                {
                    // Jeśli generacja już istnieje, zwróć odpowiedni komunikat
                    return BadRequest("Generacja już istnieje.");
                }
                _context.Versions.Add(version);
                _context.SaveChanges();
                return Ok(new { id = version.Id, name = version.Name });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        public IActionResult Equipment()
        {
            var equipments = _context.Equipments
                 .Include(a => a.EquipmentType)
                 .OrderBy(a => a.Name)
                 .ToList();
            var equipmentTypes = _context.EquipmentTypes.OrderBy(c=>c.Name).ToList();

            ViewBag.EquipmentTypes = equipmentTypes;
            return View(equipments);
        }
        
        [HttpPost]
        public IActionResult CreateEquipment(Equipment equipment, IFormFile imageFile)
        {
            try
            {
                equipment.Icon = "default.png";
                _context.Equipments.Add(equipment);
                _context.SaveChanges();
                string normalizedFileName = FileHelper.NormalizeFileName(equipment.Name);
                string fileExtension = System.IO.Path.GetExtension(imageFile.FileName);
                string finalFileName = $"{equipment.Id}_{normalizedFileName}{fileExtension}";
                string uploadDir = System.IO.Path.Combine(_environment.WebRootPath, "equipment");

                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                string filePath = System.IO.Path.Combine(uploadDir, finalFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }

                // Przypisujemy nową nazwę pliku do rekordu i zapisujemy ponownie
                equipment.Icon = "/equipment/" + finalFileName;
                _context.SaveChanges();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wewnętrzny: {ex.Message}");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> EditEquipment(int Id, string Name, int EquipmentTypeId, IFormFile ImageFile)
        {
            var equipment = await _context.Equipments.FindAsync(Id);
            if (equipment == null) return NotFound();
            string uploadDir = System.IO.Path.Combine(_environment.WebRootPath, "equipment");
            string normalizedFileName = FileHelper.NormalizeFileName(Name);
            string fileExtension = System.IO.Path.GetExtension(equipment.Icon);
            string newFileName = $"{Id}_{normalizedFileName}{fileExtension}";
            string newFilePath = System.IO.Path.Combine(uploadDir, newFileName);

            try
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Usuwanie starego pliku jeśli istnieje
                    if (!string.IsNullOrEmpty(equipment.Icon))
                    {
                        string oldFilePath = System.IO.Path.Combine(_environment.WebRootPath, equipment.Icon.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Zapis nowego pliku
                    using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }
                }
                else if (!string.IsNullOrEmpty(equipment.Icon) && !equipment.Icon.EndsWith(newFileName))
                {
                    // Jeśli zmieniono nazwę wyposażenia, zmieniamy też nazwę pliku
                    string oldFilePath = System.IO.Path.Combine(_environment.WebRootPath, equipment.Icon.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Move(oldFilePath, newFilePath);
                    }
                }

                // Aktualizacja rekordu
                equipment.Name = Name;
                equipment.EquipmentTypeId = EquipmentTypeId;
                equipment.Icon = "/equipment/" + newFileName;

                _context.Equipments.Update(equipment);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wewnętrzny: {ex.Message}");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null) return NotFound();

            // Sprawdzamy, czy wyposażenie jest przypisane do jakiegoś auta
            var carsUsingEquipment = _context.CarEquipments
                .Include(c => c.Car)
                .Where(c => c.EquipmentId == id)
                .Select(c => c.Car.Title)
                .ToList();

            if (carsUsingEquipment.Any())
            {
                return BadRequest($"Nie można usunąć wyposażenia ze względu na powiązanie z podanymi autami: {string.Join(", ", carsUsingEquipment)}");
            }

            // Usunięcie ikony z serwera
            if (!string.IsNullOrEmpty(equipment.Icon))
            {
                string iconPath = System.IO.Path.Combine(_environment.WebRootPath, equipment.Icon.TrimStart('/'));
                if (System.IO.File.Exists(iconPath))
                {
                    System.IO.File.Delete(iconPath);
                }
            }

            // Usuwamy rekord z bazy
            _context.Equipments.Remove(equipment);
            await _context.SaveChangesAsync();
            return Ok();
        }

        
        public IActionResult Brand()
        {
           var brands = _context.Brands
                 .Include(a => a.Cars)
                 .OrderBy(a => a.Name)
                 .ToList();
            return View(brands);
        }
        
        [HttpPost]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound("Nie znaleziono");
            }
            // Sprawdzamy, czy marka jest przypisane do jakiegoś modelu
            var modelBrand = _context.CarModels
                .Include(c => c.Brand)
                .Where(c => c.BrandId == id)
                .Select(c => c.Name)
                .ToList();

            if (modelBrand.Any())
            {
                return BadRequest($"Nie można usunąć ze względu na powiązanie z danymi modelami aut: {string.Join(", ", modelBrand)}");
            }
            
            try
            {
                // Sprawdzenie, czy marka ma przypisany obrazek i nie jest to domyślny plik
                if (!string.IsNullOrEmpty(brand.Icon) && brand.Icon != "/brand/default.png")
                {
                    string filePath = System.IO.Path.Combine(_environment.WebRootPath, brand.Icon.TrimStart('/'));

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath); // Usunięcie pliku
                    }
                }

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wewnętrzny: {ex.Message}");
            }
        }
        
        [HttpPost]
        public IActionResult CreateBrand(Brand brand, IFormFile imageFile)
        {
            try
            {
                brand.Icon = "default.png";
                _context.Brands.Add(brand);
                _context.SaveChanges();
                string normalizedFileName = FileHelper.NormalizeFileName(brand.Name);
                string fileExtension = System.IO.Path.GetExtension(imageFile.FileName);
                string finalFileName = $"{brand.Id}_{normalizedFileName}{fileExtension}";
                string uploadDir = System.IO.Path.Combine(_environment.WebRootPath, "brand");

                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                string filePath = System.IO.Path.Combine(uploadDir, finalFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }

                // Przypisujemy nową nazwę pliku do rekordu i zapisujemy ponownie
                brand.Icon = "/brand/" + finalFileName;
                _context.SaveChanges();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wewnętrzny: {ex.Message}");
            }
        }
    }

}
