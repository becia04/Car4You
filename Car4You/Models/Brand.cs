namespace Car4You.Models
{
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public ICollection<Car> Cars { get; set; }
        public ICollection<CarModel> CarModels { get; set; }
    }
}
