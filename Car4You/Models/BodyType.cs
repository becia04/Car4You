namespace Car4You.Models
{
    public class BodyType
    {
        public int Id { get; set; } 
        public string Name { get; set; }
        public string Icon { get; set; }

        public ICollection<Car> Cars {  get; set; }
    }
}
