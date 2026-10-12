namespace gestionUsuariosApi.Configuration;

public class RecepcionEventosOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 10;
}
