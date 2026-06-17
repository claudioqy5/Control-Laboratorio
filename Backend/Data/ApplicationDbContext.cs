using ControlLaboratorio.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlLaboratorio.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Alumno> Alumnos { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Sesion> Sesiones { get; set; }
        public DbSet<Libro> Libros { get; set; }
        public DbSet<Prestamo> Prestamos { get; set; }
        public DbSet<Favorito> Favoritos { get; set; }
        public DbSet<Multa> Multas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Alumno>()
                .HasIndex(a => a.CodigoUniversitario)
                .IsUnique();

            modelBuilder.Entity<Equipo>()
                .HasIndex(e => e.NombreRed)
                .IsUnique();

            modelBuilder.Entity<Sesion>()
                .HasIndex(s => s.Fecha);

            modelBuilder.Entity<Sesion>()
                .HasIndex(s => s.AlumnoID);

            // Configuraciones de Biblioteca
            modelBuilder.Entity<Libro>()
                .HasIndex(l => l.NroRegistro)
                .IsUnique();

            modelBuilder.Entity<Libro>()
                .HasIndex(l => l.CodigoBarras)
                .IsUnique();

            modelBuilder.Entity<Prestamo>()
                .HasIndex(p => p.AlumnoID);

            modelBuilder.Entity<Prestamo>()
                .HasIndex(p => p.LibroID);

            modelBuilder.Entity<Favorito>()
                .HasIndex(f => f.AlumnoID);

            modelBuilder.Entity<Favorito>()
                .HasIndex(f => f.LibroID);

            modelBuilder.Entity<Multa>()
                .HasIndex(m => m.AlumnoID);
        }
    }
}
