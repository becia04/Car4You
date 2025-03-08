using System.ComponentModel.DataAnnotations.Schema;

namespace Car4You.Models
{
    public class Car
    {
        public int Id { get; set; }
        public string Title { get; set; } //Tytuł
        public string Name { get; set; } //Model
        public string Description { get; set; } //Opis
        public int Year { get; set; } //Rok produkcji
        public int Mileage { get; set; } //Przebieg
        public int CarModelId { get; set; }
        [ForeignKey("CarModelId")]
        public CarModel CarModel { get; set; }
        public int FuelId { get; set; } //Paliwo
        [ForeignKey("FuelId")]
        public FuelType FuelType { get; set; }
        public int GearboxId { get; set; } //Rodzaj skrzyni biegów
        [ForeignKey("GearboxId")]
        public Gearbox Gearbox { get; set; }
        public int BodyId { get; set; } //Nadwozie
        [ForeignKey("BodyId")]
        public BodyType BodyType { get; set; }
        public int CubicCapacity { get; set; } //Pojemność skokowa
        public int EnginePower { get; set; } //Moc silnika
        public string Version { get; set; } //Wersja (1.5T 4WD Lifestyle) 
        public string Color { get; set; }
        public string ColorType { get; set; } //Metalik i inne
        public int Door {  get; set; } //Liczba drzwi
        public int Seat { get; set; } //Liczba miejsc
        public string Generation { get; set; } //Generacja (V (2018-2023))
        public string Drive { get; set; } //Napęd (4x4)
        public string Origin { get; set; } //Kraj pochodzenia
        public bool FirstOwner { get; set; }
        public int OldPrice { get; set; }
        public int NewPrice { get; set; }
        public bool IsHidden {  get; set; }
        public DateTime PublishDate { get; set; }
        public string VIN {  get; set; }


        public ICollection<CarEquipment> CarEquipment { get; set; }
    }
}
