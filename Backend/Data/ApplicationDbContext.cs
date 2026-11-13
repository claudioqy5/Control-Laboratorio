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
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<ScanLog> ScanLogs { get; set; }
        public DbSet<UsuarioAdmin> UsuariosAdmin { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ConferenciaMedica> ConferenciasMedicas { get; set; }
        public DbSet<ParticipacionConferencia> ParticipacionesConferencia { get; set; }
        public DbSet<ObjetoPerdido> ObjetosPerdidos { get; set; }
        public DbSet<BoletinSlide> BoletinSlides { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UsuarioAdmin>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(r => r.Token)
                .IsUnique();

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

            modelBuilder.Entity<Libro>()
                .HasIndex(l => l.NroRegistro);

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

            modelBuilder.Entity<Prestamo>()
                .HasOne(p => p.Libro)
                .WithMany(l => l.Prestamos)
                .HasForeignKey(p => p.LibroID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Multa>()
                .HasOne(m => m.Prestamo)
                .WithMany(p => p.Multas)
                .HasForeignKey(m => m.PrestamoID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ParticipacionConferencia>()
                .HasIndex(p => new { p.IdConferencia, p.NumeroDocumento })
                .IsUnique();

            modelBuilder.Entity<ParticipacionConferencia>()
                .HasIndex(p => new { p.IdConferencia, p.NumeroDocumento, p.CodigoURP });

            modelBuilder.Entity<ParticipacionConferencia>()
                .HasOne(p => p.Conferencia)
                .WithMany(c => c.Participaciones)
                .HasForeignKey(p => p.IdConferencia)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ParticipacionConferencia>()
                .HasOne(p => p.Alumno)
                .WithMany()
                .HasForeignKey(p => p.AlumnoID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ObjetoPerdido>()
                .HasIndex(o => new { o.EstadoObjeto, o.FechaHoraHallazgo });

            modelBuilder.Entity<ObjetoPerdido>()
                .HasIndex(o => o.AlumnoID);

            modelBuilder.Entity<ObjetoPerdido>()
                .HasIndex(o => o.CodigoURPReceptor);

            modelBuilder.Entity<ObjetoPerdido>()
                .HasOne(o => o.UsuarioHallazgo)
                .WithMany()
                .HasForeignKey(o => o.IdUsuarioHallazgo)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ObjetoPerdido>()
                .HasOne(o => o.UsuarioEntrega)
                .WithMany()
                .HasForeignKey(o => o.IdUsuarioEntrega)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ObjetoPerdido>()
                .HasOne(o => o.Alumno)
                .WithMany()
                .HasForeignKey(o => o.AlumnoID)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
