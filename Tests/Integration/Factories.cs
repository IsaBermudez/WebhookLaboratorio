using gestionUsuariosApi.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using recepcionEventosApi.Configuration;

// Las APIs usan Program con top-level statements (internal), y ambas se llaman igual, así que
// WebApplicationFactory<Program> sería ambiguo. TEntryPoint solo se usa para localizar el ensamblado:
// basta con un tipo público de cada API.
using EmisorEntryPoint = gestionUsuariosApi.Clients.RecepcionEventosClient;
using ReceptorEntryPoint = recepcionEventosApi.Configuration.ApplicationDbContext;

namespace Tests.Integration;

public static class TestConfig
{
    /// <summary>Secreto HMAC compartido por ambas APIs durante las pruebas.</summary>
    public const string Secret = "secreto-de-pruebas-automatizadas";
}

/// <summary>recepcionEventosApi real, apuntando a la BD temporal de la prueba (SQL Server).</summary>
public sealed class ReceptorFactory(string connectionString) : WebApplicationFactory<ReceptorEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Webhook:Secret"] = TestConfig.Secret }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            // Los IDs los genera SQL Server (IDENTITY(1,1)). El Program del receptor ejecuta
            // EnsureCreated al arrancar y crea la BD temporal y sus tablas.
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));
        });
    }
}

/// <summary>gestionUsuariosApi real: comparte la misma BD y entrega el webhook al handler indicado.</summary>
public sealed class EmisorFactory(string connectionString, Func<HttpMessageHandler> receptorHandler)
    : WebApplicationFactory<EmisorEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Webhook:Secret"] = TestConfig.Secret }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<GestionUsuariosDbContext>>();
            services.AddDbContext<GestionUsuariosDbContext>(o => o.UseSqlServer(connectionString));

            // En lugar de HTTP real, el webhook llega al receptor en memoria (o a un receptor "caído").
            services.AddHttpClient<EmisorEntryPoint>().ConfigurePrimaryHttpMessageHandler(receptorHandler);
        });
    }
}
