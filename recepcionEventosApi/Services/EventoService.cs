using recepcionEventosApi.Configuration;
using recepcionEventosApi.DTOs;
using recepcionEventosApi.Entities;
using recepcionEventosApi.Repositories;
using Shared.Constants;
using Shared.DTOs;
using Shared.Helpers;
using Shared.WebhookModels;

namespace recepcionEventosApi.Services;

public class EventoService(UsuarioRepository usuarios, EventoRepository eventos, ApplicationDbContext db)
{
    public async Task<WebhookProcesadoResponse> ProcesarUsuarioCreadoAsync(UsuarioCreadoWebhookDto dto, CancellationToken ct)
    {
        var nombre = dto.Data.Nombre.Trim();
        var email = dto.Data.Email.Trim();

        // El esquema aprobado no guarda un identificador del evento: un evento duplicado
        // es el que intenta registrar un usuario cuyo email ya existe.
        if (await usuarios.ExistePorEmailAsync(email, ct))
            throw new ApiException(409, ErrorCodes.EventoDuplicado, "El evento ya fue procesado: el usuario ya existe.");

        var ahora = DateTime.UtcNow;
        var usuario = new Usuario { Nombre = nombre, Email = email, FechaCreacion = ahora };
        var evento = new Evento
        {
            EventType = dto.EventType,
            Estado = WebhookConstants.EstadoProcesado,
            FechaRecepcion = ahora
        };

        usuarios.Agregar(usuario);
        eventos.Agregar(evento);
        await db.SaveChangesAsync(ct); // un solo SaveChanges = ambos INSERT en una transacción; SQL Server asigna los IDs

        return new WebhookProcesadoResponse
        {
            UsuarioId = usuario.Id,
            EventoId = evento.Id,
            Mensaje = "Evento procesado correctamente."
        };
    }

    public async Task<List<EventoResponse>> ListarAsync(CancellationToken ct) =>
        (await eventos.ListarAsync(ct)).Select(ToResponse).ToList();

    public async Task<EventoResponse> ObtenerAsync(int id, CancellationToken ct)
    {
        var evento = await eventos.ObtenerPorIdAsync(id, ct)
            ?? throw new ApiException(404, ErrorCodes.NoEncontrado, $"No existe el evento con id {id}.");
        return ToResponse(evento);
    }

    private static EventoResponse ToResponse(Evento e) => new()
    {
        Id = e.Id,
        EventType = e.EventType,
        Estado = e.Estado,
        FechaRecepcion = e.FechaRecepcion
    };
}
