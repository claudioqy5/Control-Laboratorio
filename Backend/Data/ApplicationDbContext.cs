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

            // Seed data if needed, but the user said DB already created.
            // However, we need to ensure the PC is in the Equipos table for the agent to work.
        }
    }
}
