using Microsoft.EntityFrameworkCore;
using recepcionEventosApi.Entities;

namespace recepcionEventosApi.Configuration;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Evento> Eventos => Set<Evento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn(1, 1).ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.Email).HasColumnType("varchar(150)").IsRequired();
            e.Property(x => x.FechaCreacion).HasColumnType("datetime").IsRequired();
        });

        modelBuilder.Entity<Evento>(e =>
        {
            e.ToTable("Eventos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn(1, 1).ValueGeneratedOnAdd();
            e.Property(x => x.EventType).HasColumnType("varchar(100)").IsRequired();
            e.Property(x => x.Estado).HasColumnType("varchar(20)").IsRequired();
            e.Property(x => x.FechaRecepcion).HasColumnType("datetime").IsRequired();
        });
    }
}
