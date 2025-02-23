namespace Car4You.Models
{
    public class CarEquipment
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public Equipment Equipment { get; set; }

        public int CarId { get; set; }
        public Car Car { get; set; }

    }
}
