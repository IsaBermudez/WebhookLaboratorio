using System.Net.Mail;

namespace Shared.Helpers;

public static class UsuarioValidator
{
    /// <summary>Devuelve el mensaje de error, o null si los datos son válidos.</summary>
    public static string? ObtenerError(string? nombre, string? email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return "El nombre es obligatorio.";
        if (nombre.Trim().Length > 100)
            return "El nombre no puede superar 100 caracteres.";

        if (string.IsNullOrWhiteSpace(email))
            return "El email es obligatorio.";
        var emailLimpio = email.Trim();
        if (emailLimpio.Length > 150)
            return "El email no puede superar 150 caracteres.";
        if (!MailAddress.TryCreate(emailLimpio, out var direccion) || direccion.Address != emailLimpio)
            return "El email no tiene un formato válido.";

        return null;
    }
}
