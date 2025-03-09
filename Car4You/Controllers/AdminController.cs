using Car4You.DAL;
using Car4You.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Car4You.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        [HttpPost]
        public IActionResult CreateModel(CarModel carModel)
        {
            try
            {
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
        public async Task<IActionResult> EditModel(int Id, string Name, int BrandId)
        {
            var carModel = await _context.CarModels.FindAsync(Id);
            if (carModel == null) return NotFound();
            try
            {
                // Aktualizacja rekordu
                carModel.Name = Name;
                carModel.BrandId = BrandId;

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
        public async Task<IActionResult> DeleteModel(int id)
        {
            var carModel = await _context.CarModels.FindAsync(id);
            if (carModel == null) return NotFound();

            // Sprawdzamy, czy model jest przypisany do auta
            var carsUsingModel = _context.Cars
                .Include(c => c.CarModel)
                .Where(c => c.CarModelId == id)
                .Select(c => c.Title)
                .ToList();

            if (carsUsingModel.Any())
            {
                return BadRequest($"Nie można usunąć modelu ze względu na powiązanie z podanymi autami: {string.Join(", ", carsUsingModel)}");
            }

            // Usuwamy rekord z bazy
            _context.CarModels.Remove(carModel);
            await _context.SaveChangesAsync();
            return Ok();
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
        public IActionResult CreateVersion(Models.Version version)
        {
            try
            {
                _context.Versions.Add(version);
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd wewnętrzny: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditVersion(int Id, string Name, int carModelId)
        {
            var version = await _context.Versions.FindAsync(Id);
            if (version == null) return NotFound();
            try
            {
                // Aktualizacja rekordu
                version.Name = Name;
                version.CarModelId = carModelId;

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

        [HttpGet]
        public JsonResult GetCarModelsByBrand(int brandId)
        {
            var carModels = _context.CarModels
                .Where(cm => cm.BrandId == brandId)
                .Select(cm => new { cm.Id, cm.Name })
                .ToList();

            return Json(carModels);
        }
    }
}
