using Car4You.DAL;
using Car4You.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Car4You.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Car4You.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Diagnostics.Metrics;
using System.Globalization;
using static Car4You.ViewModels.CarViewModel;
using Microsoft.VisualBasic;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Text.RegularExpressions;
using System.Text;

namespace Car4You.Controllers
{
    public class AdminController : Controller
    {
        private readonly CarDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;

        public AdminController(CarDbContext context, IWebHostEnvironment environment, ILogger<AdminController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> AddCar()
        {
            var equipmentTypes = _context.EquipmentTypes.OrderBy(e=>e.Name).ToList();  // Pobranie typów ekwipunku
            var equipmentList = _context.Equipments.Include(e => e.EquipmentType).OrderBy(e=>e.Name).ToList(); // Pobranie ekwipunków z ich typami

            // Grupowanie ekwipunków według typu
            var groupedEquipment = equipmentTypes.Select(equipmentType => new EquipmentGroup
            {
                EquipmentType = equipmentType,
                Equipments = equipmentList.Where(e => e.EquipmentTypeId == equipmentType.Id).ToList()
            }).ToList();

            var viewModel = new CarViewModel
            {
                Car = new Car(), // Nowy obiekt auta
                Brands = await _context.Brands.OrderBy(b=>b.Name).ToListAsync(),
                CarModels = new List<Models.CarModel>(),
                BodyTypes = await _context.BodyTypes.OrderBy(b => b.Name).ToListAsync(),
                Versions= new List<Models.Version>(),
                EquipmentTypes = equipmentTypes,
                GroupedEquipment = groupedEquipment,
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Zapobiega atakom CSRF
        public async Task<IActionResult>AddCar(CarViewModel model, int MainPhotoIndex)
        {
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
            if(model.CarPhotos == null)
            {
                ModelState.AddModelError("CarPhotos", "Zdjęcia są wymagane");
            }
            else if (model.CarPhotos.Count > 21)
            {
                ModelState.AddModelError("CarPhotos", "Liczba zdjęć musi być mniejsza od 21");
            }

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


                return View(model);

            }
            try
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
                        }).ToList();

                    _context.CarEquipments.AddRange(carEquipments);
                }

                // Obsługa zdjęć
                if (model.CarPhotos != null && model.CarPhotos.Count > 0)
                {
                    var photos = new List<Photo>();

                    // Pobieramy markę, model i rok auta
                    var car = _context.Cars.Include(c => c.CarModel)
                                            .ThenInclude(m => m.Brand) // Pobieramy markę z modelu
                                            .FirstOrDefault(c => c.Id == model.Car.Id);

                    if (car == null)
                    {
                        _logger.LogError($"Nie znaleziono auta o ID {model.Car.Id}");
                        return StatusCode(500, "Błąd zapisu zdjęć");
                    }

                    string brand = car.CarModel?.Brand?.Name ?? "BrakMarki";
                    string modelName = car.CarModel?.Name ?? "BrakModelu";
                    string generation = car.CarModel?.Versions?.FirstOrDefault()?.Name ?? ""; // Pobranie pierwszej generacji, jeśli istnieje
                    string year = car.Year.ToString();

                    int photoIndex = 0;

                    foreach (var file in model.CarPhotos)
                    {
                        string orignalFileName = $"{car.Id}_{brand}_{modelName}{(string.IsNullOrEmpty(generation) ? "" : $"_{generation}")}_{year}_{photoIndex + 1}";

                        var fileNameWithoutExtension = $"{RemoveDiacritics(orignalFileName.Replace(" ", "_")).ToLower()}";
                        fileNameWithoutExtension = Regex.Replace(fileNameWithoutExtension, "[^a-zA-Z0-9_]", "");
                        var fileName = $"{fileNameWithoutExtension}.jpg";

                        var savePath = Path.Combine(_environment.WebRootPath, "cars", fileName);

                        using (var stream = new FileStream(savePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        photos.Add(new Photo
                        {
                            CarId = car.Id,
                            Title = fileName,
                            PhotoPath = Path.Combine("cars", fileName).Replace("\\", "/"), // ✅ to zapisujemy do bazy
                            IsMain = (photoIndex == MainPhotoIndex)
                        });

                        photoIndex++;
                    }


                    _context.Photos.AddRange(photos);
                }



                await _context.SaveChangesAsync(); // Zapisujemy wyposażenie i zdjęcia
                return RedirectToAction("Index");

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        public static string RemoveDiacritics(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string normalizedString = input.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
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
                                   .Select(v => new { id = v.Id, name = v.Name })
                                   .OrderBy(v=>v.name)
                                   .ToList();
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
                string uploadDir = Path.Combine(_environment.WebRootPath, "brand");
                string oldFilePath = null;
                string newFilePath = null;
                string newFileName = null;

                // Sprawdzamy, czy marka ma przypisaną ikonę
                if (!string.IsNullOrEmpty(brand.Icon))
                {
                    // Pobieramy stare rozszerzenie pliku
                    string fileExtension = Path.GetExtension(brand.Icon);

                    // Tworzymy nową nazwę pliku
                    string normalizedFileName = FileHelper.NormalizeFileName(Name);
                    newFileName = $"{Id}_{normalizedFileName}{fileExtension}";

                    // Ścieżki do starego i nowego pliku
                    oldFilePath = Path.Combine(_environment.WebRootPath, brand.Icon.TrimStart('/'));
                    newFilePath = Path.Combine(uploadDir, newFileName);
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

            string uploadDir = Path.Combine(_environment.WebRootPath, "brand");

            // Upewnij się, że katalog istnieje
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            // Pobierz rozszerzenie z nowego pliku
            string fileExtension = Path.GetExtension(ImageFile.FileName);
            string normalizedFileName = FileHelper.NormalizeFileName(name);
            string newFileName = $"{Id}_{normalizedFileName}{fileExtension}";
            string newFilePath = Path.Combine(uploadDir, newFileName);

            try
            {
                // Usunięcie starego pliku (jeśli istnieje)
                if (!string.IsNullOrEmpty(brand.Icon))
                {
                    string oldFilePath = Path.Combine(_environment.WebRootPath, brand.Icon.TrimStart('/'));
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
                string fileExtension = Path.GetExtension(imageFile.FileName);
                string finalFileName = $"{equipment.Id}_{normalizedFileName}{fileExtension}";
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
            string uploadDir = Path.Combine(_environment.WebRootPath, "equipment");
            string normalizedFileName = FileHelper.NormalizeFileName(Name);
            string fileExtension = Path.GetExtension(equipment.Icon);
            string newFileName = $"{Id}_{normalizedFileName}{fileExtension}";
            string newFilePath = Path.Combine(uploadDir, newFileName);

            try
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Usuwanie starego pliku jeśli istnieje
                    if (!string.IsNullOrEmpty(equipment.Icon))
                    {
                        string oldFilePath = Path.Combine(_environment.WebRootPath, equipment.Icon.TrimStart('/'));
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
                    string oldFilePath = Path.Combine(_environment.WebRootPath, equipment.Icon.TrimStart('/'));
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
                string iconPath = Path.Combine(_environment.WebRootPath, equipment.Icon.TrimStart('/'));
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
                    string filePath = Path.Combine(_environment.WebRootPath, brand.Icon.TrimStart('/'));

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


        /*
        public IActionResult CarModel()
        {
            var carModels = _context.CarModels
                 .Include(a => a.Brand)
                 .OrderBy(a => a.Name)
                 .ToList();
            var brand = _context.Brands.OrderBy(c=>c.Name).ToList();

            ViewBag.Brand = brand;
            return View(carModels);
        }


        public IActionResult Version()
        {
            var version = _context.Versions
                 .Include(a => a.CarModel)
                 .Include(c=>c.CarModel.Brand)
                 .OrderBy(a => a.CarModel.Brand.Name)
                 .ThenBy(a=>a.CarModel.Name)
                 .ThenBy(a=>a.Name)
                 .ToList();

            ViewBag.Brand = _context.Brands.OrderBy(c=>c.Name).ToList();
            ViewBag.CarModel = _context.CarModels.OrderBy(c => c.Name).ToList();
            return View(version);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBrand(Brand brand, IFormFile ImageFile)
        {
            try
            {
                brand.Icon = "default.png"; // Tymczasowe ustawienie ikony
                _context.Brands.Add(brand);
                await _context.SaveChangesAsync(); // Teraz mamy ID

                // Tworzenie nazwy pliku
                string normalizedFileName = FileHelper.NormalizeFileName(brand.Name);
                string fileExtension = Path.GetExtension(ImageFile.FileName);
                string finalFileName = $"{brand.Id}_{normalizedFileName}{fileExtension}";
                string uploadDir = Path.Combine(_environment.WebRootPath, "brand");

                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                string filePath = Path.Combine(uploadDir, finalFileName);

                // Zapisywanie pliku
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fileStream);
                }

                // Aktualizacja ścieżki do ikony
                brand.Icon = "/brand/" + finalFileName;
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
        public async Task<IActionResult> EditBrand(int Id, string Name, IFormFile ImageFile)
        {
            var brand = await _context.Brands.FindAsync(Id);
            if (brand == null) return NotFound();


            string uploadDir = Path.Combine(_environment.WebRootPath, "brand");
            string normalizedFileName = FileHelper.NormalizeFileName(Name);
            string fileExtension = Path.GetExtension(brand.Icon);
            string newFileName = $"{Id}_{normalizedFileName}{fileExtension}";
            string newFilePath = Path.Combine(uploadDir, newFileName);

            try
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Usuwanie starego pliku jeśli istnieje
                    if (!string.IsNullOrEmpty(brand.Icon))
                    {
                        string oldFilePath = Path.Combine(_environment.WebRootPath, brand.Icon.TrimStart('/'));
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
                else if (!string.IsNullOrEmpty(brand.Icon) && !brand.Icon.EndsWith(newFileName))
                {
                    // Jeśli zmieniono nazwę wyposażenia, zmieniamy też nazwę pliku
                    string oldFilePath = Path.Combine(_environment.WebRootPath, brand.Icon.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Move(oldFilePath, newFilePath);
                    }
                }

                // Aktualizacja rekordu
                brand.Name = Name;
                brand.Icon = "/brand/" + newFileName;

                _context.Brands.Update(brand);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wewnętrzny: {ex.Message}");
            }

        }

         

        
        */

    }

}
