using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car4You.Models
{
    public class CarEquipment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        [ValidateNever]
        public Equipment Equipment { get; set; }

        public int CarId { get; set; }
        public Car Car { get; set; }

    }
}
