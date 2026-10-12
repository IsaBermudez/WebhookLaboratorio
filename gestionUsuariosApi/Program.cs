using gestionUsuariosApi.Clients;
using gestionUsuariosApi.Configuration;
using gestionUsuariosApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Constants;
using Shared.DTOs;
using Shared.Middleware;
using Shared.WebhookModels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
        o.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(
            new ErrorResponse { Mensaje = "La solicitud no es válida.", Codigo = ErrorCodes.SolicitudInvalida }));

builder.Services.Configure<WebhookOptions>(builder.Configuration.GetSection("Webhook"));
builder.Services.Configure<RecepcionEventosOptions>(builder.Configuration.GetSection("RecepcionEventos"));

builder.Services.AddDbContext<GestionUsuariosDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")));

builder.Services.AddHttpClient<RecepcionEventosClient>((sp, client) =>
{
    var opciones = sp.GetRequiredService<IOptions<RecepcionEventosOptions>>().Value;
    client.BaseAddress = new Uri(opciones.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos);
});

builder.Services.AddScoped<WebhookSenderService>();
builder.Services.AddScoped<UsuarioService>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
