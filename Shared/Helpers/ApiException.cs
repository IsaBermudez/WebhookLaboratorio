namespace Shared.Helpers;

/// <summary>Error de negocio/protocolo con el código HTTP y el código de error que debe devolver la API.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string Codigo { get; }

    public ApiException(int statusCode, string codigo, string mensaje) : base(mensaje)
    {
        StatusCode = statusCode;
        Codigo = codigo;
    }
}
