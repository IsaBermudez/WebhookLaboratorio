using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using recepcionEventosApi.Configuration;
using recepcionEventosApi.Repositories;
using recepcionEventosApi.Security;
using recepcionEventosApi.Services;
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

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")));

builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<EventoRepository>();
builder.Services.AddScoped<EventoService>();
builder.Services.AddSingleton<HmacAuthenticationService>();
builder.Services.AddScoped<ValidacionWebhookService>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapControllers();

if (app.Configuration.GetValue("Database:EnsureCreated", true))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
}

app.Run();
