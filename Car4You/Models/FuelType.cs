namespace Car4You.Models
{
    public class FuelType
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Car> Car { get; set; }
    }
}
