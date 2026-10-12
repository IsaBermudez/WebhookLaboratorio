using gestionUsuariosApi.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using recepcionEventosApi.Configuration;

using EmisorEntryPoint = gestionUsuariosApi.Clients.RecepcionEventosClient;
using ReceptorEntryPoint = recepcionEventosApi.Configuration.ApplicationDbContext;

namespace Tests.Integration;

public static class TestConfig
{
    //Secreto HMAC compartido por ambas APIs durante las pruebas
    public const string Secret = "secreto-de-pruebas-automatizadas";
}

public sealed class ReceptorFactory(string connectionString) : WebApplicationFactory<ReceptorEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Webhook:Secret"] = TestConfig.Secret }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));
        });
    }
}

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
            services.AddHttpClient<EmisorEntryPoint>().ConfigurePrimaryHttpMessageHandler(receptorHandler);
        });
    }
}
