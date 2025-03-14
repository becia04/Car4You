using Car4You.Models;

namespace Car4You.ViewModels
{
    public class CarViewModel
    {
        public Car Car { get; set; }
        public List<Brand> Brands { get; set; }
        public List<CarModel> CarModels { get; set; }
        public List<FuelType> FuelTypes { get; set; }
        public List<Gearbox> Gearboxes { get; set; }
        public List<BodyType> BodyTypes { get; set; }
        public List<Models.Version> Versions { get; set; }
        public List<Equipment> Equipments { get; set; }
    }
}
