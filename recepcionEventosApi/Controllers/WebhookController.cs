using System.Text;
using Microsoft.AspNetCore.Mvc;
using recepcionEventosApi.Services;
using Shared.Constants;

namespace recepcionEventosApi.Controllers;

[ApiController]
public class WebhookController(ValidacionWebhookService validacion, EventoService eventoService) : ControllerBase
{
    [HttpPost("/recibirEventoUsuarioCreado")]
    public async Task<IActionResult> RecibirEventoUsuarioCreado(CancellationToken ct)
    {
        // Se lee el body completo tal cual llegó: es exactamente lo que firmó el emisor.
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(ct);

        string? firma = Request.Headers[WebhookConstants.SignatureHeader].FirstOrDefault();

        var dto = validacion.Validar(body, firma);
        var resultado = await eventoService.ProcesarUsuarioCreadoAsync(dto, ct);
        return Ok(resultado);
    }
}
