using Car4You.Controllers;
using Car4You.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace Car4You.ViewModels
{

    public class CarViewModel
    {
        public Car Car { get; set; }
        public List<Brand> Brands { get; set; }
        public List<CarModel> CarModels { get; set; }
        public List<FuelType> FuelTypes { get; set; }
        public List<Gearbox> Gearboxes { get; set; }
        public List<BodyType> BodyTypes { get; set; }
        public List<Models.Version> Versions { get; set; }
        public List<EquipmentType> EquipmentTypes { get; set; }
        public List<EquipmentGroup> GroupedEquipment { get; set; }
        public string SelectedCountry { get; set; }
        public List<string> Countries { get; set; }
        public string SelectedColor { get; set; }
        public List<string> Colors { get; set; }
        public int MostCommonYear { get; set; }
        public int MostCommonEnginePower { get; set; }
        public int MostCommonCubicCapacity { get; set; }

        public class EquipmentGroup
        {
            public EquipmentType EquipmentType { get; set; }
            public List<Equipment> Equipments { get; set; }
        }
        public CarViewModel()
        {
            SelectedCountry = "Niemcy"; // Domyślnie wybrane Niemcy

            Countries = new List<string>
        {
            "Polska",
            "Niemcy",
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
            Countries.Remove("Polska");
            Countries.Remove("Niemcy");
            Countries.Insert(0, "Niemcy");
            Countries.Insert(0, "Polska");
            
            //Lista kolorów
            SelectedColor = "Czarny"; // Domyślnie wybrany kolor

            Colors = new List<string>
        {
            "Biały", "Czarny", "Czerwony", "Niebieski", "Zielony", "Srebrny", "Szary", "Grafitowy",
            "Pomarańczowy", "Żółty", "Fioletowy", "Brązowy", "Beżowy", "Złoty", "Turkusowy", "Burgund"
        };

            // Sortowanie listy alfabetycznie bez domyślnego koloru na początku
            Colors = Colors.Distinct().OrderBy(c => c).ToList();
            Colors.Remove("Czarny");
            Colors.Insert(0, "Czarny");

        }

    }
}
