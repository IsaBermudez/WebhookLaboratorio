using Microsoft.AspNetCore.Mvc;
using recepcionEventosApi.DTOs;
using recepcionEventosApi.Services;

namespace recepcionEventosApi.Controllers;

[ApiController]
public class EventosController(EventoService eventoService) : ControllerBase
{
    [HttpGet("/consultarEventos")]
    public async Task<ActionResult<List<EventoResponse>>> ConsultarEventos(CancellationToken ct) =>
        Ok(await eventoService.ListarAsync(ct));

    [HttpGet("/consultarEventoId/{id:int}")]
    public async Task<ActionResult<EventoResponse>> ConsultarEventoId(int id, CancellationToken ct) =>
        Ok(await eventoService.ObtenerAsync(id, ct));
}
