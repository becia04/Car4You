namespace Car4You.Models
{
    public class CarModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BrandId { get; set; }
        public Brand Brand { get; set; }

        public ICollection<Car> Car {  get; set; }
    }
}
