using gestionUsuariosApi.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace gestionUsuariosApi.Controllers;

[ApiController]
public class UsuariosController(UsuarioService usuarioService) : ControllerBase
{
    [HttpPost("/crearUsuario")]
    public async Task<ActionResult<UsuarioResponse>> CrearUsuario([FromBody] CrearUsuarioRequest request, CancellationToken ct) =>
        Ok(await usuarioService.CrearUsuarioAsync(request, ct));

    [HttpGet("/consultarUsuarios")]
    public async Task<ActionResult<List<UsuarioResponse>>> ConsultarUsuarios(CancellationToken ct) =>
        Ok(await usuarioService.ListarAsync(ct));

    [HttpGet("/consultarUsuarioId/{id:int}")]
    public async Task<ActionResult<UsuarioResponse>> ConsultarUsuarioId(int id, CancellationToken ct) =>
        Ok(await usuarioService.ObtenerAsync(id, ct));
}
