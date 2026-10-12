using Microsoft.Extensions.Options;
using Shared.Helpers;
using Shared.WebhookModels;

namespace recepcionEventosApi.Security;

public class HmacAuthenticationService(IOptions<WebhookOptions> options)
{
    public bool ValidarFirma(string body, string? firmaRecibida) =>
        HmacHelper.FirmaEsValida(body, options.Value.Secret, firmaRecibida);
}
