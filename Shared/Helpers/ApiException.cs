namespace Shared.Helpers;

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
