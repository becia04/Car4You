using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Car4You.Models
{
    public class Version
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CarModelId { get; set; }
        [ValidateNever]
        public CarModel CarModel { get; set; }

        public ICollection<Car> Cars {  get; set; }
    }
}
