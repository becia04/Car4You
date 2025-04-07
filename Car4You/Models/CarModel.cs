using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Car4You.Models
{
    public class CarModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BrandId { get; set; }
        [ValidateNever]
        public Brand Brand { get; set; }

        public ICollection<Car> Cars {  get; set; }
        public ICollection<Version> Versions {  get; set; }
    }
}
