using System.Security.Cryptography;
using System.Text;

namespace Shared.Helpers;

public static class HmacHelper
{
    public static string CalcularFirma(string payload, string secret)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool FirmaEsValida(string body, string secret, string? firmaRecibida)
    {
        if (string.IsNullOrWhiteSpace(firmaRecibida))
            return false;

        byte[] recibida;
        try
        {
            recibida = Convert.FromHexString(firmaRecibida.Trim());
        }
        catch (FormatException)
        {
            return false;
        }

        var esperada = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body));
        return CryptographicOperations.FixedTimeEquals(esperada, recibida);
    }
}
