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
using Car4You.Migrations;
using static Car4You.ViewModels.CarViewModel;

namespace Car4You.Controllers
{
    public class AdminController : Controller
    {
        private readonly CarDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminController(CarDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> AddCar()
        {
            var carModels = await _context.CarModels.Include(m => m.Brand).OrderBy(m=>m.Name).ToListAsync();
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
                Brands = carModels.Select(m => m.Brand).Distinct().ToList(),
                CarModels = carModels,
                FuelTypes = await _context.FuelTypes.OrderBy(f => f.Name).ToListAsync(),
                Gearboxes = await _context.Gearboxes.OrderBy(g => g.Name).ToListAsync(),
                BodyTypes = await _context.BodyTypes.OrderBy(b => b.Name).ToListAsync(),
                Versions = await _context.Versions.ToListAsync(),
                EquipmentTypes = equipmentTypes,
                GroupedEquipment = groupedEquipment,
                MostCommonYear = _context.Cars
                .GroupBy(c => c.Year)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault(),

                MostCommonEnginePower = _context.Cars
                .GroupBy(c => c.EnginePower)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault(),

                MostCommonCubicCapacity = _context.Cars
                .GroupBy(c => c.CubicCapacity)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault()
            };

            return View(viewModel);
        }



        [HttpPost]
        [ValidateAntiForgeryToken] // Zapobiega atakom CSRF
        public IActionResult AddCar(CarViewModel model, List<IFormFile> photos, int? mainPhotoIndex)
        {
            // Sprawdzenie, czy rok produkcji jest w prawidłowym zakresie
            if (model.Car.Year > DateTime.Now.Year + 1)
            {
                ModelState.AddModelError("Car.Year", $"Rok produkcji nie może być większy niż {DateTime.Now.Year + 1}.");
            }
            if (!ModelState.IsValid)
            {
                // Jeśli formularz zawiera błędy, ponownie wypełnij ViewModel i zwróć widok
                model.Brands = _context.Brands.ToList();
                model.CarModels = _context.CarModels.ToList();
                model.FuelTypes = _context.FuelTypes.OrderBy(f=>f.Name).ToList();
                model.Gearboxes = _context.Gearboxes.OrderBy(g=>g.Name).ToList();
                model.BodyTypes = _context.BodyTypes.OrderBy(b=>b.Name).ToList();
                model.Versions = _context.Versions.ToList();
                return View(model);
            }

            // Przypisz aktualną datę publikacji
            model.Car.PublishDate = DateTime.Now;

            // Zapisz auto do bazy danych
            _context.Cars.Add(model.Car);
            _context.SaveChanges();
            // Obsługa zdjęć
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            int index = 0;

            foreach (var photo in photos)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    photo.CopyTo(fileStream);
                }

                _context.Photos.Add(new Photo
                {
                    CarId = model.Car.Id,
                    PhotoPath = "/uploads/" + uniqueFileName,
                    IsMain = index == mainPhotoIndex
                });

                index++;
            }

            _context.SaveChanges();

            return Json(new { success = true });
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
    .Include(b => b.CarModel)
        .ThenInclude(m => m.Version)
    .Where(b => b.Id == id)
    .Select(b => new Brand
    {
        Id = b.Id,
        Name = b.Name,
        CarModel = b.CarModel
            .OrderBy(m => m.Name) // Sortowanie modeli alfabetycznie
            .Select(m => new Models.CarModel
            {
                Id = m.Id,
                Name = m.Name,
                BrandId = m.BrandId,
                Version = m.Version.OrderBy(v => v.Name).ToList() // Sortowanie wersji alfabetycznie
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

                return Ok();
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
            var carModel = await _context.CarModels.Include(m => m.Version).FirstOrDefaultAsync(m => m.Id == id);

            if (carModel == null)
            {
                return NotFound("Model nie istnieje.");
            }

            _context.Versions.RemoveRange(carModel.Version);
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
                return Ok();
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
                 .Include(a => a.Car)
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
