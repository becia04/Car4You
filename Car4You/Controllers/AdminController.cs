using Car4You.DAL;
using Car4You.Helper;
using Car4You.Helpers;
using Car4You.Models;
using Car4You.ViewModels;
using Microsoft.AspNetCore.Hosting;
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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static Car4You.ViewModels.CarViewModel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Car4You.Controllers
{
    public class AdminController : Controller
    {
        private readonly CarDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;
        private readonly PhotoUploadHelper _photoHelper;

        public AdminController(CarDbContext context, IWebHostEnvironment environment, ILogger<AdminController> logger, PhotoUploadHelper photoHelper)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _photoHelper = photoHelper;
        }

        public IActionResult Index()
        {
            return View();
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
            TempData.Remove("TempPhotos");
            _photoHelper.ClearTemp();

            return View("CarForm", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditCar(int id)
        {
            var car = await _context.Cars
                .Include(c => c.BodyTypes)
                .Include(c=>c.Photos)
                .Include(c => c.Version)
                .Include(c => c.CarEquipments)
                .Include(c => c.CarModel)
                    .ThenInclude(cm => cm.Versions)
                .Include(c => c.CarModel)
                            .ThenInclude(cm => cm.Brand)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null)
            {
                return NotFound();
            }

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
                SavedPhotoPaths = car.Photos.Select(p => new TempPhoto
                {
                    Src = p.PhotoPath,
                    IsMain = p.IsMain
                }).ToList()
            };

            return View("CarForm", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Zapobiega atakom CSRF
        public async Task<IActionResult>SaveCar(CarViewModel model, int MainPhotoIndex)
        {
            bool isEditing = model.Car.Id > 0;
            //Rok
            if (model.Car.Year < 1900)
            {
                ModelState.AddModelError("Car.Year", "Rok produkcji musi być większy od 1900");
            }
            else if (model.Car.Year > DateTime.Now.Year)
            {
                ModelState.AddModelError("Car.Year", $"Rok produkcji musi być mniejszy od {DateTime.Now.Year}");
            }

            //Przebieg
            if (model.Car.Mileage <= 0)
            {
                ModelState.AddModelError("Car.Mileage", "Przebieg musi być większy od 0 km");
            }
            else if (model.Car.Mileage > 999999)
            {
                ModelState.AddModelError("Car.Mileage", "Przebieg musi być mniejszy od 999 999 km");
            }

            //Pojemność skokowa
            if (model.Car.CubicCapacity <= 100)
            {
                ModelState.AddModelError("Car.CubicCapacity", "Pojemność skokowa musi być większa od 100 cm³");
            }
            else if (model.Car.CubicCapacity > 8000)
            {
                ModelState.AddModelError("Car.CubicCapacity", "Pojemność skokowa musi być mniejsza od 8000 cm³");
            }

            //Moc silnika
            if (model.Car.EnginePower <= 0)
            {
                ModelState.AddModelError("Car.EnginePower", "Moc silnika musi być większa od 0 KM");
            }
            else if (model.Car.EnginePower > 2000)
            {
                ModelState.AddModelError("Car.EnginePower", "Moc silnika musi być mniejsza od 2000 KM");
            }

            //Drzwi
            if(model.Car.Door<2)
            {
                ModelState.AddModelError("Car.Door", "Liczba drzwi musi być większa od 1");
            }
            else if (model.Car.Door > 5)
            {
                ModelState.AddModelError("Car.Door", "Liczba drzwi mniejsza od 5");
            }

            //Liczba miejsc
            if(model.Car.Seat<2)
            {
                ModelState.AddModelError("Car.Seat", "Ilość miejsc musi być większa od 1");
            }
            else if (model.Car.Seat > 9)
            {
                ModelState.AddModelError("Car.Seat", "Ilość miejsc musi być mniejsza od 9");
            }

            //Cena
            if(model.Car.OldPrice<1)
            {
                ModelState.AddModelError("Car.OldPrice", "Cena musi być większa od 0 zł");
            }
           else if(model.Car.OldPrice>999999)
            {
                ModelState.AddModelError("Car.OldPrice", "Cena musi być mniejsza od 999 999 zł");
            }

            //Zdjęcia
            // Obsługa zdjęć
            if (model.CarPhotos != null && model.CarPhotos.Count > 0)
            {
                var uploaded = await _photoHelper.ProcessUploadedPhotosAsync(model.CarPhotos, MainPhotoIndex);
                model.SavedPhotoPaths = uploaded;
                _photoHelper.SaveToTempData(TempData, uploaded);
            }
            else
            {
                var restored = _photoHelper.RestoreFromTempData(TempData);
                if (restored == null || restored.Count == 0)
                {
                    if (isEditing)
                    {
                        // wczytaj zdjęcia z bazy, jeżeli edytujemy
                        model.SavedPhotoPaths = _context.Photos
                            .Where(p => p.CarId == model.Car.Id)
                            .Select(p => new TempPhoto
                            {
                                Src = p.PhotoPath,
                                IsMain = p.IsMain
                            })
                            .ToList();
                    }
                    else
                    {
                        model.SavedPhotoPaths = restored;
                    }
                }
                else
                {
                    model.SavedPhotoPaths = restored;
                }
            }


            // Walidacja zdjęć
            int totalCount = model.SavedPhotoPaths?.Count ?? 0;

            if (!isEditing) // tylko przy dodawaniu
            {
                if (totalCount == 0)
                    ModelState.AddModelError("CarPhotos", "Zdjęcia są wymagane.");
            }
            if (totalCount > 21)
            {
                ModelState.AddModelError("CarPhotos", "Liczba zdjęć musi być mniejsza lub równa 20.");
            }


            // Aktualizacja IsMain
            for (int i = 0; i < (model.SavedPhotoPaths?.Count ?? 0); i++)
            {
                model.SavedPhotoPaths[i].IsMain = (i == MainPhotoIndex);
            }
            _photoHelper.SaveToTempData(TempData, model.SavedPhotoPaths);


            if (!ModelState.IsValid)
            {
                var equipmentTypes = _context.EquipmentTypes.OrderBy(e => e.Name).ToList();  // Pobranie typów ekwipunku
                var equipmentList = _context.Equipments.Include(e => e.EquipmentType).OrderBy(e => e.Name).ToList(); // Pobranie ekwipunków z ich typami

                // Grupowanie ekwipunków według typu
                var groupedEquipment = equipmentTypes.Select(equipmentType => new EquipmentGroup
                {
                    EquipmentType = equipmentType,
                    Equipments = equipmentList.Where(e => e.EquipmentTypeId == equipmentType.Id).ToList()
                }).ToList();

                // Przekazanie wypełnionego modelu
                model.Car = model.Car ?? new Car();

                model.Brands = _context.Brands.OrderBy(m => m.Name).ToList();
                model.CarModels = new List<Models.CarModel>();
                model.BodyTypes = await _context.BodyTypes.OrderBy(b => b.Name).ToListAsync();
                model.Versions = new List<Models.Version>();
                model.EquipmentTypes = equipmentTypes;
                model.GroupedEquipment = groupedEquipment;


                return View("CarForm", model);

            }
            try
            {
                if (isEditing==false)
                {
                    // Przypisz aktualną datę publikacji
                    model.Car.PublishDate = DateTime.Now;
                    model.Car.NewPrice = model.Car.OldPrice;
                    model.Car.CarModel = null;

                    // Zapisz auto do bazy danych
                    _context.Cars.Add(model.Car);
                    await _context.SaveChangesAsync(); // Zapisujemy auto do bazy, by mieć jego ID

                    // Obsługa wyposażenia
                    if (model.SelectedEquipmentIds != null)
                    {
                        var carEquipments = model.SelectedEquipmentIds
                            .Select(equipmentId => new CarEquipment
                            {
                                CarId = model.Car.Id,
                                EquipmentId = equipmentId
                            })
                            .ToList();

                        _context.CarEquipments.AddRange(carEquipments);
                    }

                    // Obsługa zdjęć
                    if (model.SavedPhotoPaths != null && model.SavedPhotoPaths.Any())
                    {
                        var photos = new List<Photo>();

                        var car = _context.Cars
                            .Include(c => c.CarModel)
                            .ThenInclude(m => m.Brand)
                            .FirstOrDefault(c => c.Id == model.Car.Id);

                        if (car == null)
                        {
                            _logger.LogError($"Nie znaleziono auta o ID {model.Car.Id}");
                            return StatusCode(500, "Błąd zapisu zdjęć");
                        }

                        string brand = car.CarModel?.Brand?.Name ?? "BrakMarki";
                        string modelName = car.CarModel?.Name ?? "BrakModelu";
                        string generation = car.CarModel?.Versions?.FirstOrDefault()?.Name ?? "";
                        string year = car.Year.ToString();

                        string carsFolder = System.IO.Path.Combine(_environment.WebRootPath, "cars");
                        if (!Directory.Exists(carsFolder))
                            Directory.CreateDirectory(carsFolder);

                        int photoIndex = 0;

                        foreach (var tempPhoto in model.SavedPhotoPaths)
                        {
                            string orignalFileName =
                                $"{car.Id}_{brand}_{modelName}{(string.IsNullOrEmpty(generation) ? "" : $"_{generation}")}_{year}_{photoIndex + 1}";

                            var fileNameWithoutExtension = FileHelper.NormalizeFileName(orignalFileName);
                            var fileName = $"{fileNameWithoutExtension}.jpg";

                            var sourcePath = System.IO.Path.Combine(
                                _environment.WebRootPath,
                                "temp",
                                System.IO.Path.GetFileName(tempPhoto.Src)
                            );

                            var targetPath = System.IO.Path.Combine(carsFolder, fileName);

                            try
                            {
                                if (System.IO.File.Exists(sourcePath))
                                {
                                    var logoPath = System.IO.Path.Combine(_environment.WebRootPath, "logo.png");
                                    await PhotoUploadHelper.OverlayLogoAsync(sourcePath, logoPath, targetPath);
                                }
                                else
                                {
                                    _logger.LogWarning($"Nie znaleziono pliku tymczasowego: {sourcePath}");
                                    continue;
                                }

                                photos.Add(new Photo
                                {
                                    CarId = car.Id,
                                    Title = fileName,
                                    PhotoPath = $"/cars/{fileName}",
                                    IsMain = tempPhoto.IsMain
                                });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Błąd podczas kopiowania zdjęcia: {sourcePath} → {targetPath}");
                            }

                            photoIndex++;
                        }

                        _context.Photos.AddRange(photos);
                    }

                    // 🧹 Czyszczenie plików tymczasowych
                    TempData.Remove("TempPhotos");
                    _photoHelper.ClearTemp();

                    // ✅ Przekierowanie lub komunikat sukcesu
                    TempData["Success"] = "Auto zostało dodane.";

                    await _context.SaveChangesAsync(); // Zapisujemy wyposażenie i zdjęcia

                    return RedirectToAction("Index");
                }

                //Edycja istniejącego auta
                else
                {
                    var car = await _context.Cars
     .Include(c => c.CarEquipments)
     .Include(c => c.Photos)
     .Include(c => c.CarModel)
         .ThenInclude(m => m.Brand)
     .Include(c => c.CarModel)
         .ThenInclude(m => m.Versions)
     .FirstOrDefaultAsync(c => c.Id == model.Car.Id);

                    if (car == null)
                        return NotFound();

                    // 🔹 Kluczowa linijka — nadpisuje tylko proste właściwości (bez relacji)
                    _context.Entry(car).CurrentValues.SetValues(model.Car);

                    // 🔹 Upewnij się, że EF nie nadpisze relacji
                    _context.Entry(car).Collection(c => c.CarEquipments).IsModified = false;
                    _context.Entry(car).Collection(c => c.Photos).IsModified = false;
                    _context.Entry(car).Reference(c => c.CarModel).IsModified = false;

                    // 🔹 Dodatkowe logiki biznesowe
                    car.PublishDate = DateTime.Now;
                    car.NewPrice = model.Car.OldPrice;

                    await _context.SaveChangesAsync();


                    // 🔹 Wyposażenie — aktualizujemy tylko różnice
                    if (model.SelectedEquipmentIds != null)
                    {
                        var existingEquipIds = car.CarEquipments.Select(e => e.EquipmentId).ToList();
                        var toRemove = car.CarEquipments.Where(e => !model.SelectedEquipmentIds.Contains(e.EquipmentId)).ToList();
                        var toAdd = model.SelectedEquipmentIds
                            .Where(id => !existingEquipIds.Contains(id))
                            .Select(id => new CarEquipment { CarId = car.Id, EquipmentId = id })
                            .ToList();

                        if (toRemove.Any()) _context.CarEquipments.RemoveRange(toRemove);
                        if (toAdd.Any()) _context.CarEquipments.AddRange(toAdd);
                    }


                    // 🔹 Zdjęcia — tylko różnice, nie kasujemy wszystkich
                    if (model.SavedPhotoPaths != null && model.SavedPhotoPaths.Any())
                    {
                        var existingPhotos = car.Photos.ToList();
                        var incomingNames = model.SavedPhotoPaths.Select(p => Path.GetFileName(p.Src)).ToList();

                        var toRemove = existingPhotos
                            .Where(p => !incomingNames.Contains(Path.GetFileName(p.PhotoPath)))
                            .ToList();

                        if (toRemove.Any())
                            _context.Photos.RemoveRange(toRemove);

                        var existingNames = existingPhotos.Select(p => Path.GetFileName(p.PhotoPath)).ToList();
                        var photosToAdd = new List<Photo>();

                        string carsFolder = Path.Combine(_environment.WebRootPath, "cars");
                        if (!Directory.Exists(carsFolder))
                            Directory.CreateDirectory(carsFolder);

                        int photoIndex = 0;
                        foreach (var tempPhoto in model.SavedPhotoPaths)
                        {
                            string tempFileName = Path.GetFileName(tempPhoto.Src);
                            if (existingNames.Contains(tempFileName))
                            {
                                photoIndex++;
                                continue;
                            }

                            string fileNameBase = $"{car.Id}_{car.CarModel?.Brand?.Name}_{car.CarModel?.Name}_{car.Year}_{photoIndex + 1}";
                            var fileName = $"{FileHelper.NormalizeFileName(fileNameBase)}.jpg";

                            var sourcePath = Path.Combine(_environment.WebRootPath, "temp", tempFileName);
                            var targetPath = Path.Combine(carsFolder, fileName);

                            if (System.IO.File.Exists(sourcePath))
                            {
                                var logoPath = Path.Combine(_environment.WebRootPath, "logo.png");
                                await PhotoUploadHelper.OverlayLogoAsync(sourcePath, logoPath, targetPath);

                                photosToAdd.Add(new Photo
                                {
                                    CarId = car.Id,
                                    Title = fileName,
                                    PhotoPath = $"/cars/{fileName}",
                                    IsMain = tempPhoto.IsMain
                                });
                            }

                            photoIndex++;
                        }

                        // 🔹 Aktualizacja flagi IsMain dla istniejących zdjęć
                        if (model.SavedPhotoPaths != null && model.SavedPhotoPaths.Any())
                        {
                            var dbPhotos = await _context.Photos
                                .Where(p => p.CarId == car.Id)
                                .ToListAsync();

                            foreach (var dbPhoto in dbPhotos)
                            {
                                var updatedPhoto = model.SavedPhotoPaths
                                    .FirstOrDefault(p => Path.GetFileName(p.Src).Equals(Path.GetFileName(dbPhoto.PhotoPath), StringComparison.OrdinalIgnoreCase));

                                if (updatedPhoto != null && dbPhoto.IsMain != updatedPhoto.IsMain)
                                {
                                    dbPhoto.IsMain = updatedPhoto.IsMain;
                                    _context.Entry(dbPhoto).Property(p => p.IsMain).IsModified = true;
                                }
                            }
                        }


                        if (photosToAdd.Any())
                            _context.Photos.AddRange(photosToAdd);

                    }

                    TempData.Remove("TempPhotos");
                    _photoHelper.ClearTemp();

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Auto zostało zaktualizowane.";
                    return RedirectToAction("Index");

                }
            }
            catch (Exception ex)
            {
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

            return RedirectToAction("CarList");
        }

        [HttpGet("/admin/temp-preview")]
        public IActionResult TempPreview(string file)
        {
            var tempPath = System.IO.Path.Combine(_environment.ContentRootPath, "App_TempUploads", file);

            if (!System.IO.File.Exists(tempPath))
                return NotFound();

            var contentType = "image/" + System.IO.Path.GetExtension(file).Trim('.'); // np. image/png
            return PhysicalFile(tempPath, contentType);
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
        public async Task<IActionResult> EditBrandName(int Id, string Name)
        {
            var brand = await _context.Brands.FindAsync(Id);
            if (brand == null) return NotFound();

            try
            {
                string uploadDir = System.IO.Path.Combine(_environment.WebRootPath, "brand");
                string oldFilePath = null;
                string newFilePath = null;
                string newFileName = null;

                // Sprawdzamy, czy marka ma przypisaną ikonę
                if (!string.IsNullOrEmpty(brand.Icon))
                {
                    // Pobieramy stare rozszerzenie pliku
                    string fileExtension = System.IO.Path.GetExtension(brand.Icon);

                    // Tworzymy nową nazwę pliku
                    string normalizedFileName = FileHelper.NormalizeFileName(Name);
                    newFileName = $"{Id}_{normalizedFileName}{fileExtension}";

                    // Ścieżki do starego i nowego pliku
                    oldFilePath = System.IO.Path.Combine(_environment.WebRootPath, brand.Icon.TrimStart('/'));
                    newFilePath = System.IO.Path.Combine(uploadDir, newFileName);
                }

                // Aktualizujemy nazwę marki w bazie
                brand.Name = Name;

                // Jeśli plik istnieje i nowa nazwa się zmienia
                if (!string.IsNullOrEmpty(oldFilePath) && System.IO.File.Exists(oldFilePath))
                {
                    // Zmieniamy nazwę pliku (przenosimy go)
                    System.IO.File.Move(oldFilePath, newFilePath);

                    // Aktualizujemy ścieżkę w bazie
                    brand.Icon = "/brand/" + newFileName;
                }

                _context.Brands.Update(brand);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wewnętrzny: {ex.Message}");
            }
        }


        [HttpPost]
        public async Task<IActionResult> EditBrandIcon(int Id, IFormFile ImageFile)
        {
            if (ImageFile == null || ImageFile.Length == 0)
            {
                return BadRequest("Nie przesłano pliku.");
            }

            var brand = await _context.Brands.FindAsync(Id);
            if (brand == null) return NotFound();

            string name = await _context.Brands
                .Where(i => i.Id == Id)
                .Select(i => i.Name)
                .FirstOrDefaultAsync();

            string uploadDir = System.IO.Path.Combine(_environment.WebRootPath, "brand");

            // Upewnij się, że katalog istnieje
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            // Pobierz rozszerzenie z nowego pliku
            string fileExtension = System.IO.Path.GetExtension(ImageFile.FileName);
            string normalizedFileName = FileHelper.NormalizeFileName(name);
            string newFileName = $"{Id}_{normalizedFileName}{fileExtension}";
            string newFilePath = System.IO.Path.Combine(uploadDir, newFileName);

            try
            {
                // Usunięcie starego pliku (jeśli istnieje)
                if (!string.IsNullOrEmpty(brand.Icon))
                {
                    string oldFilePath = System.IO.Path.Combine(_environment.WebRootPath, brand.Icon.TrimStart('/'));
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

                // Aktualizacja ścieżki w bazie danych
                brand.Icon = "/brand/" + newFileName;
                _context.Brands.Update(brand);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                // Logowanie szczegółów błędu
                Console.WriteLine($"Błąd: {ex.Message}");
                Console.WriteLine($"Szczegóły błędu: {ex.InnerException?.Message}");
                return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
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


    }

}
