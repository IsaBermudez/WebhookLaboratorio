using Microsoft.EntityFrameworkCore;
using recepcionEventosApi.Configuration;
using recepcionEventosApi.Entities;

namespace recepcionEventosApi.Repositories;

public class UsuarioRepository(ApplicationDbContext db)
{
    public void Agregar(Usuario usuario) => db.Usuarios.Add(usuario);

    public Task<bool> ExistePorEmailAsync(string email, CancellationToken ct) =>
        db.Usuarios.AnyAsync(u => u.Email == email, ct);
}
