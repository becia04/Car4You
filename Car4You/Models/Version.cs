namespace Car4You.Models
{
    public class Version
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CarModelId { get; set; }
        public CarModel CarModel { get; set; }

        public ICollection<Car> Car {  get; set; }
    }
}
