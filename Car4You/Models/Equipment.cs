namespace Car4You.Models
{
    public class Equipment
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Icon { get; set; }
        public int EquipmentTypeId { get; set; }
        public EquipmentType EquipmentType { get; set; }

        public ICollection<CarEquipment> CarEquipment { get; set; }
    }
}
