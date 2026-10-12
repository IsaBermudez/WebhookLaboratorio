using gestionUsuariosApi.Configuration;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.DTOs;
using Shared.Helpers;

namespace gestionUsuariosApi.Services;

public class UsuarioService(WebhookSenderService sender, GestionUsuariosDbContext db)
{
    public async Task<UsuarioResponse> CrearUsuarioAsync(CrearUsuarioRequest request, CancellationToken ct)
    {
        var error = UsuarioValidator.ObtenerError(request.Nombre, request.Email);
        if (error is not null)
            throw new ApiException(400, ErrorCodes.SolicitudInvalida, error);

        var usuario = new CrearUsuarioRequest { Nombre = request.Nombre.Trim(), Email = request.Email.Trim() };

        // El emisor no persiste ni genera IDs: el receptor guarda y devuelve los IDs generados por la BD.
        var resultado = await sender.EnviarUsuarioCreadoAsync(usuario, ct);

        return new UsuarioResponse { Id = resultado.UsuarioId, Nombre = usuario.Nombre, Email = usuario.Email };
    }

    public Task<List<UsuarioResponse>> ListarAsync(CancellationToken ct) =>
        db.Usuarios.AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UsuarioResponse { Id = u.Id, Nombre = u.Nombre, Email = u.Email })
            .ToListAsync(ct);

    public async Task<UsuarioResponse> ObtenerAsync(int id, CancellationToken ct)
    {
        return await db.Usuarios.AsNoTracking()
                   .Where(u => u.Id == id)
                   .Select(u => new UsuarioResponse { Id = u.Id, Nombre = u.Nombre, Email = u.Email })
                   .FirstOrDefaultAsync(ct)
               ?? throw new ApiException(404, ErrorCodes.NoEncontrado, $"No existe el usuario con id {id}.");
    }
}
