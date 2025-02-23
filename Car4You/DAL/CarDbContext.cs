using Car4You.Models;
using Microsoft.EntityFrameworkCore;

namespace Car4You.DAL
{
    public class CarDbContext : DbContext
    {
        public CarDbContext(DbContextOptions<CarDbContext>options):base (options) { }

        public DbSet<Car> Cars { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<BodyType> BodyTypes { get; set; }
        public DbSet<FuelType> FuelTypes { get; set; }
        public DbSet<Gearbox> Gearboxes { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<CarEquipment> CarEquipments { get; set; }
        public DbSet<EquipmentType> EquipmentTypes { get; set; }
        public DbSet<Photo> Photos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CarEquipment>()
                .HasKey(ce => new { ce.CarId, ce.EquipmentId });
            base.OnModelCreating(modelBuilder);
        }
    }
}
