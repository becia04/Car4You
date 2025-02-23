namespace Car4You.Models
{
    public class EquipmentType
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Equipment> Equipment { get; set; }
    }
}
