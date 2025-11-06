using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ECommerce.Models;

namespace ECommerce.Data
{
    // Asegúrate de que hereda de IdentityDbContext<IdentityUser>
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; } = default!;
        public DbSet<Order> Orders { get; set; } = default!;
        public DbSet<OrderItem> OrderItems { get; set; } = default!;


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 🎯 SOLUCIÓN a las advertencias de decimales (Price y Total)
            // Esto asegura que se usa el tipo SQL Server 'decimal' con precisión 18 y escala 2 (18,2)
            // para evitar el truncamiento de valores monetarios.

            // Para Product.Price
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Para Order.Total
            builder.Entity<Order>()
                .Property(o => o.Total)
                .HasColumnType("decimal(18,2)");

            // (Si OrderItem.UnitPrice no tiene [Column(TypeName = "decimal(18,2)")] en su modelo)
            // builder.Entity<OrderItem>()
            //    .Property(i => i.UnitPrice)
            //    .HasColumnType("decimal(18,2)");
        }
    }
}