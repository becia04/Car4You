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

        public IActionResult Equipment()
        {
            var equipments = _context.Equipments
                 .Include(a => a.EquipmentType)
                 .OrderBy(a => a.Name)
                 .ToList();
            var equipmentTypes = _context.EquipmentTypes.ToList();

            ViewBag.EquipmentTypes = equipmentTypes;
            return View(equipments);
        }

        [HttpPost]
        public IActionResult CreateEquipment(Equipment equipment, IFormFile imageFile)
        {
            if (string.IsNullOrWhiteSpace(equipment.Name))
            {
                return BadRequest("Equipment name is required.");
            }
            if (_context.Equipments.Any(e => e.Name == equipment.Name))
            {
                return BadRequest("This name already exists.");
            }
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("Image file is required.");
            }
            if (equipment.EquipmentTypeId <= 0)
            {
                return BadRequest("Invalid equipment type.");
            }

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
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditEquipment(int Id, string Name, int EquipmentTypeId, IFormFile ImageFile)
        {
            var equipment = await _context.Equipments.FindAsync(Id);
            if (equipment == null) return NotFound();

            if (string.IsNullOrWhiteSpace(Name))
            {
                return BadRequest(new { field = "Name", message = "Nazwa jest wymagana." });
            }
            if (_context.Equipments.Any(e => e.Id != Id && e.Name == Name))
            {
                return BadRequest("This name already exists.");
            }
            if (EquipmentTypeId <= 0)
            {
                return BadRequest("Invalid equipment type.");
            }

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
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null) return NotFound();

            // Sprawdzamy, czy wyposażenie jest przypisane do jakiegoś auta
            var carsUsingEquipment = _context.CarEquipments
                .Include(c=>c.Car)
                .Where(c => c.EquipmentId == id)
                .Select(c => c.Car.Name)
                .ToList();

            if (carsUsingEquipment.Any())
            {
                return BadRequest($"Cannot delete this equipment because it is assigned to the following cars: {string.Join(", ", carsUsingEquipment)}");
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


    }
}
