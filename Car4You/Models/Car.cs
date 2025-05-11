using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car4You.Models
{
    public class Car
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Tytuł jest wymagany")]
        public string Title { get; set; } //Tytuł
        public string ?Description { get; set; } //Opis

        [Required(ErrorMessage = "Rok produkcji jest wymagany")]
        public int? Year { get; set; } //Rok produkcji

        [Required(ErrorMessage = "Przebieg jest wymagany")]
        public int? Mileage { get; set; } //Przebieg

        public string FuelType { get; set; } //Paliwo

        public string Gearbox { get; set; } //Rodzaj skrzyni biegów

        public int BodyId { get; set; } //Nadwozie
        [ForeignKey("BodyId")]
        [ValidateNever]
        public BodyType BodyTypes { get; set; }

        public int CarModelId { get; set; }
        [ForeignKey("CarModelId")]
        [ValidateNever]
        public CarModel CarModel { get; set; }

        public int ?VersionId { get; set; } //Wersja (1.5T 4WD Lifestyle)
        [ForeignKey("VersionId")]
        [ValidateNever]
        public Version Version { get; set; }

        [Required(ErrorMessage = "Pojemność skokowa jest wymagana")]
        public int ?CubicCapacity { get; set; } //Pojemność skokowa

        [Required(ErrorMessage = "Moc silnika jest wymagana")]
        public int ?EnginePower { get; set; } //Moc silnika

        public string Color { get; set; }

        public string ColorType { get; set; } //Metalik i inne

        [Required(ErrorMessage = "Liczba drzwi jest wymagana")]
        public int? Door {  get; set; } //Liczba drzwi

        [Required(ErrorMessage = "Liczba miejsc jest wymagana")]
        public int? Seat { get; set; } //Liczba miejsc

        //[Required(ErrorMessage = "Wersja jest wymagana")]
        //[StringLength(50, ErrorMessage = "Wersja nie może mieć więcej niż 50 znaków.")]
        public string ?Generation { get; set; } //Zmiana planów, to jest jako wersja

        public string Drive { get; set; } //Napęd (4x4)

        public string Origin { get; set; } //Kraj pochodzenia

        public bool FirstOwner { get; set; }
        public bool? PolishPlate { get; set; }

        [Required(ErrorMessage = "Cena jest wymagana")]
        public int? OldPrice { get; set; }
        public int? NewPrice { get; set; }
        public bool IsHidden {  get; set; }
        public DateTime PublishDate { get; set; }
        [StringLength(17, ErrorMessage = "Vin nie może mieć więcej niż 17 znaków")]
        public string ?VIN {  get; set; }
        public bool ?AccidentFree { get; set; }

        [ValidateNever]
        public ICollection<CarEquipment> CarEquipments { get; set; }
        [ValidateNever]
        public ICollection<Photo> Photos { get; set; }
    }
}
