namespace Car4You.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string PhotoPath { get; set; }
        public bool IsMain { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; }
    }
}
