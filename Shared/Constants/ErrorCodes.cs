namespace Shared.Constants;

public static class ErrorCodes
{
    public const string SolicitudInvalida = "SOLICITUD_INVALIDA";       // 400
    public const string FirmaInvalida = "FIRMA_INVALIDA";               // 401
    public const string NoEncontrado = "NO_ENCONTRADO";                 // 404
    public const string EventoDuplicado = "EVENTO_DUPLICADO";           // 409
    public const string ErrorInterno = "ERROR_INTERNO";                 // 500
    public const string WebhookRechazado = "WEBHOOK_RECHAZADO";         // 502
    public const string ReceptorNoDisponible = "RECEPTOR_NO_DISPONIBLE"; // 503
}
