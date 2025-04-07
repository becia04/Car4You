using Car4You.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Car4You.Models
{
    public class Equipment
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Nazwa jest wymagana")]
        [StringLength(100, ErrorMessage = "Nazwa nie może mieć więcej niż 100 znaków")]
        public string Name { get; set; }
        public string Icon { get; set; }
        [Required(ErrorMessage = "Wybierz typ wyposażenia")]
        public int EquipmentTypeId { get; set; }
        [ValidateNever]
        public EquipmentType EquipmentType { get; set; }

        public ICollection<CarEquipment> CarEquipments { get; set; }
    }

    }
