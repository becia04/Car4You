using Car4You.Controllers;
using Car4You.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace Car4You.ViewModels
{

    public class CarViewModel
    {
        public Car Car { get; set; }
        [ValidateNever]
        public List<Brand> Brands { get; set; }
        [ValidateNever]
        public List<CarModel> CarModels { get; set; }
        public List<string> FuelTypes { get; set; }
        public List<string> Gearboxes { get; set; }
        [ValidateNever]
        public List<BodyType> BodyTypes { get; set; }
        [ValidateNever]
        public List<Models.Version> Versions { get; set; }
        [ValidateNever]
        public List<EquipmentType> EquipmentTypes { get; set; }
        [ValidateNever]
        public List<EquipmentGroup> GroupedEquipment { get; set; }
        public List<string> Countries { get; set; }
        public string SelectedColor { get; set; }
        public List<string> Colors { get; set; }
        public List<int> SelectedEquipmentIds { get; set; } = new();
        public List<IFormFile>? CarPhotos { get; set; }
        public class EquipmentGroup
        {
            [ValidateNever]
            public EquipmentType EquipmentType { get; set; }
            public List<Equipment> Equipments { get; set; }
        }
        public CarViewModel()
        {
            FuelTypes = new List<string>
            {
                "Beznzyna", "Benzyzna+CNG", "Benzyna+LPG", "Diesel"
            };

            Gearboxes = new List<string>
            {
                "Manualna", "Automatyczna"
            };
            
            //Kraje
            Countries = new List<string>
        {
            "Albania", "Andora", "Austria", "Belgia", "Białoruś", "Bośnia i Hercegowina", "Bułgaria",
            "Chorwacja", "Czarnogóra", "Czechy", "Dania", "Estonia", "Finlandia", "Francja", "Grecja",
            "Hiszpania", "Holandia", "Irlandia", "Islandia", "Kosowo", "Liechtenstein", "Litwa",
            "Luksemburg", "Łotwa", "Macedonia Północna", "Malta", "Mołdawia", "Monako", "Norwegia",
            "Portugalia", "Rumunia", "San Marino", "Serbia", "Słowacja", "Słowenia", "Szwajcaria",
            "Szwecja", "Ukraina", "Watykan", "Węgry", "Wielka Brytania", "Włochy",
            "Stany Zjednoczone"
        };
            // Usuwamy duplikaty (gdyby np. Polska/Niemcy pojawiły się dwa razy) i sortujemy poza Polską i Niemcami
            Countries = Countries.Distinct().OrderBy(c => c).ToList();
            Countries.Insert(0, "Polska");
            Countries.Insert(0, "Niemcy");

            //Lista kolorów
            SelectedColor = "Czarny"; // Domyślnie wybrany kolor

            Colors = new List<string>
        {
            "Biały", "Czarny", "Czerwony", "Niebieski", "Zielony", "Srebrny", "Szary", "Grafitowy",
            "Pomarańczowy", "Żółty", "Fioletowy", "Brązowy", "Beżowy", "Złoty", "Turkusowy", "Burgund"
        };

            // Sortowanie listy alfabetycznie bez domyślnego koloru na początku
            Colors = Colors.Distinct().OrderBy(c => c).ToList();

        }

    }
}
