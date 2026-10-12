using Microsoft.EntityFrameworkCore;
using gestionUsuariosApi.Models;

namespace gestionUsuariosApi.Configuration;

public class GestionUsuariosDbContext(DbContextOptions<GestionUsuariosDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Nombre).HasColumnType("varchar(100)");
            e.Property(x => x.Email).HasColumnType("varchar(150)");
            e.Property(x => x.FechaCreacion).HasColumnType("datetime");
        });
    }
}
