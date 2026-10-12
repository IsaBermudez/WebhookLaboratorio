using Microsoft.Extensions.Options;
using Shared.Helpers;
using Shared.WebhookModels;

namespace recepcionEventosApi.Security;

/// <summary>Valida la cabecera X-Signature recalculando HMAC-SHA256 sobre el body completo.</summary>
public class HmacAuthenticationService(IOptions<WebhookOptions> options)
{
    public bool ValidarFirma(string body, string? firmaRecibida) =>
        HmacHelper.FirmaEsValida(body, options.Value.Secret, firmaRecibida);
}
