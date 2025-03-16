using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car4You.Models
{
    public class Car
    {
        public int Id { get; set; }
        public string Title { get; set; } //Tytuł
        public string Description { get; set; } //Opis

        [Required(ErrorMessage = "Rok jest wymagany")]
        [Range(1900, int.MaxValue, ErrorMessage = "Rok produkcji musi być większy niż 1900.")]
        public int Year { get; set; } //Rok produkcji

        [Required(ErrorMessage = "Przebieg jest wymagany")]
        [Range(0, 999999, ErrorMessage = "Przebieg musi być liczbą całkowitą z zakresu od 0 do 999 999 km.")]
        public int Mileage { get; set; } //Przebieg

        public int FuelId { get; set; } //Paliwo
        [ForeignKey("FuelId")]
        public FuelType FuelType { get; set; }

        public int GearboxId { get; set; } //Rodzaj skrzyni biegów
        [ForeignKey("GearboxId")]
        public Gearbox Gearbox { get; set; }

        public int BodyId { get; set; } //Nadwozie
        [ForeignKey("BodyId")]
        public BodyType BodyType { get; set; }


        public int CarModelId { get; set; }
        [ForeignKey("CarModelId")]
        public CarModel CarModel { get; set; }
        public int ?VersionId { get; set; } //Wersja (1.5T 4WD Lifestyle)
        [ForeignKey("VersionId")]
        public Version Version { get; set; }

        [Range(100, 8000, ErrorMessage = "Pojemność skokowa musi być liczbą z zakresu od 0 do 8000 cm³.")]
        public int CubicCapacity { get; set; } //Pojemność skokowa

        [Range(1, 2000, ErrorMessage = "Moc silnika musi być liczbą z zakresu od 1 do 2000 KM.")]
        public int EnginePower { get; set; } //Moc silnika

        [Required(ErrorMessage = "Kolor jest wymagany.")]
        [StringLength(50, ErrorMessage = "Kolor nie może mieć więcej niż 50 znaków.")]
        public string Color { get; set; }

        public string ColorType { get; set; } //Metalik i inne

        [Required(ErrorMessage = "Liczba drzwi jest wymagana")]
        [Range(2, 5, ErrorMessage = "Liczba drzwi musi być z zakresu od 0 do 5")]
        public int Door {  get; set; } //Liczba drzwi

        [Required(ErrorMessage = "Liczba miejsc jest wymagana")]
        [Range(2, 9, ErrorMessage = "Liczba miejsc musi być z zakresu od 2 do 9")]
        public int Seat { get; set; } //Liczba miejsc

        [Required(ErrorMessage = "Wersja jest wymagana")]
        [StringLength(50, ErrorMessage = "Wersja nie może mieć więcej niż 50 znaków.")]
        public string Generation { get; set; } //Zmiana planów, to jest jako wersja

        public string Drive { get; set; } //Napęd (4x4)

        public string Origin { get; set; } //Kraj pochodzenia

        public bool FirstOwner { get; set; }

        [Required(ErrorMessage = "Cena jest wymagana")]
        [Range(0, int.MaxValue, ErrorMessage = "Cena musi być większa niż 0")]
        public int OldPrice { get; set; }
        public int NewPrice { get; set; }
        public bool IsHidden {  get; set; }
        public DateTime PublishDate { get; set; }
        [StringLength(17, ErrorMessage = "Vin nie może mieć więcej niż 17 znaków")]
        public string VIN {  get; set; }


        public ICollection<CarEquipment> CarEquipment { get; set; }
    }
}
