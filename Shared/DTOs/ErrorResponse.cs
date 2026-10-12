namespace Shared.DTOs;

/// <summary>Formato único de error de ambas APIs: { "mensaje": "...", "codigo": "..." }.</summary>
public class ErrorResponse
{
    public string Mensaje { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}
