using Microsoft.EntityFrameworkCore;
using recepcionEventosApi.Configuration;
using recepcionEventosApi.Entities;

namespace recepcionEventosApi.Repositories;

public class EventoRepository(ApplicationDbContext db)
{
    public void Agregar(Evento evento) => db.Eventos.Add(evento);

    public Task<List<Evento>> ListarAsync(CancellationToken ct) =>
        db.Eventos.AsNoTracking().OrderBy(e => e.Id).ToListAsync(ct);

    public Task<Evento?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        db.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
}
