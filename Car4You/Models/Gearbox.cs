namespace Car4You.Models
{
    public class Gearbox
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Car> Car { get; set; }
    }
}
