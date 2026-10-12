using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.DTOs;
using Shared.Helpers;

namespace Shared.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);

            // 404 de ruta inexistente
            var status = context.Response.StatusCode;
            if (status >= 400 && !context.Response.HasStarted
                && context.Response.ContentLength is null
                && string.IsNullOrEmpty(context.Response.ContentType))
            {
                await EscribirAsync(context, status, CodigoPorEstado(status), MensajePorEstado(status));
            }
        }
        catch (ApiException ex) when (!context.Response.HasStarted)
        {
            await EscribirAsync(context, ex.StatusCode, ex.Codigo, ex.Message);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            logger.LogError(ex, "Error no controlado procesando {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
            await EscribirAsync(context, StatusCodes.Status500InternalServerError, ErrorCodes.ErrorInterno, "Error interno del servidor.");
        }
    }

    private static async Task EscribirAsync(HttpContext context, int status, string codigo, string mensaje)
    {
        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ErrorResponse { Mensaje = mensaje, Codigo = codigo });
    }

    private static string CodigoPorEstado(int status) => status switch
    {
        400 => ErrorCodes.SolicitudInvalida,
        401 => ErrorCodes.FirmaInvalida,
        404 => ErrorCodes.NoEncontrado,
        409 => ErrorCodes.EventoDuplicado,
        _ => ErrorCodes.ErrorInterno
    };

    private static string MensajePorEstado(int status) => status switch
    {
        400 => "La solicitud no es válida.",
        401 => "No autorizado.",
        404 => "Recurso no encontrado.",
        409 => "Conflicto con el estado actual del recurso.",
        405 => "Método no permitido.",
        _ => "Error procesando la solicitud."
    };
}
