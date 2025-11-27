using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ECommerce.Models;

namespace ECommerce.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; } = default!;
        public DbSet<Order> Orders { get; set; } = default!;
        public DbSet<OrderItem> OrderItems { get; set; } = default!;
        
        // ✅ Ahora esto funcionará porque ya definimos la clase AppSetting en Order.cs
        public DbSet<AppSetting> AppSettings { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuración de decimales para evitar warnings y errores de precisión
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Order>()
                .Property(o => o.Total)
                .HasColumnType("decimal(18,2)");
                
            builder.Entity<Order>()
                .Property(o => o.ShippingCost) // ✅ Configurar el nuevo campo
                .HasColumnType("decimal(18,2)");

            // Seed (Datos iniciales) para el costo de envío
            builder.Entity<AppSetting>().HasData(
                new AppSetting { Id = 1, Key = "ShippingCost", Value = "6.00" }
            );
        }
    }
}