namespace gestionUsuariosApi.Models;

/// <summary>Vista de solo lectura de la tabla Usuarios (la escritura es del receptor).</summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}
