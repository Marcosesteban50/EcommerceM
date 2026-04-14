using EcommerceAPI.Modelos;
using EcommerceAPI.Modelos.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {


        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Producto>(x =>
            {
                x.Property(c => c.Id).ValueGeneratedOnAdd();

                x.HasOne(p => p.Usuario)
               .WithMany()
               .HasForeignKey(p => p.UsuarioId);



                x.Property(p => p.Nombre)
                   .IsRequired()
                   .HasMaxLength(100);

                x.Property(x => x.Precio)
                .HasColumnType("decimal(18,2)");


            });

            modelBuilder.Entity<Producto>()
            .HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Categoria>(x =>
            {

                x.Property(c => c.Id).ValueGeneratedOnAdd();
            });


            modelBuilder.Entity<Orden>(x =>
            {
                x.Property(c => c.Total)
                .HasColumnType("decimal(18,2)");

                x.HasOne(p => p.EstadoOrden)
             .WithMany()
             .HasForeignKey(p => p.EstadoOrdenId);

                x.HasOne(p => p.EstadoPago)
           .WithMany()
           .HasForeignKey(p => p.EstadoPagoId);

            });

            modelBuilder.Entity<OrdenItems>(x =>
            {
                x.Property(c => c.PrecioUnitario).
                HasColumnType("decimal(18,2)");

            });

            modelBuilder.Entity<EstadoOrden>(x =>
            {
                // Relación Orden -> EstadoOrden
                x.Property(o => o.Id).ValueGeneratedOnAdd();
                    
            });


            modelBuilder.Entity<EstadoPago>(x =>
            {
                // Relación Pago -> EstadoPago
                x.Property(o => o.Id).ValueGeneratedOnAdd();

            });


            modelBuilder.Entity<ProductoHistorial>(x =>
            {
                x.Property(c => c.Id).ValueGeneratedOnAdd();
            });


            
        }


        public DbSet<Producto> Productos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Carrito> Carritos { get; set; }
        public DbSet<CarritoItems> CarritoItems { get; set; }

        public DbSet<Orden> Ordenes { get; set; }
        public DbSet<PerfilUsuario> PerfilesUsuarios { get; set; }
        public DbSet<EstadoOrden> EstadoOrdenes { get; set; }
        public DbSet<EstadoPago> EstadoPagos { get; set; }

        public DbSet<ProductoHistorial> ProductoHistorial { get; set; }



    }
}
