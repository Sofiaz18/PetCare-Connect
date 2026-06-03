using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCareConnect.API.Data;
using PetCareConnect.API.DTOs;
using PetCareConnect.API.Models;
using PetCareConnect.API.Services;

namespace PetCareConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly PetCareDb _db;

    public UsuariosController(PetCareDb db) => _db = db;

    // GET api/usuarios?perfil=Veterinario
    [HttpGet]
    [Authorize(Roles = "Admin,Veterinario")]
    public async Task<IActionResult> Listar([FromQuery] string? perfil)
    {
        var lista = await _db.ListarUsuariosAsync(perfil);
        var resp = lista.Select(u => new UsuarioResponse(
            u.Id, u.Nome, u.Login, u.Perfil, u.Email, u.Ativo, u.CriadoEm));
        return Ok(new ApiResponse<IEnumerable<UsuarioResponse>>(true, "OK", resp));
    }

    // GET api/usuarios/veterinarios  — usado pelo front para popular dropdown
    [HttpGet("veterinarios")]
    public async Task<IActionResult> ListarVets()
    {
        var lista = await _db.ListarUsuariosAsync("Veterinario");
        var resp  = lista.Select(u => new UsuarioResponse(
            u.Id, u.Nome, u.Login, u.Perfil, u.Email, u.Ativo, u.CriadoEm));
        return Ok(new ApiResponse<IEnumerable<UsuarioResponse>>(true, "OK", resp));
    }

    // POST api/usuarios  — somente Admin cria usuários
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Criar([FromBody] CriarUsuarioRequest req)
    {
        var perfisValidos = new[] { "Tutor", "Veterinario", "Admin" };
        if (!perfisValidos.Contains(req.Perfil))
            return BadRequest(new ApiResponse(false, "Perfil inválido."));

        var existente = await _db.BuscarPorLoginAsync(req.Login);
        if (existente is not null)
            return Conflict(new ApiResponse(false, "Login já cadastrado."));

        var novoId = await _db.CriarUsuarioAsync(new Usuario
        {
            Nome      = req.Nome,
            Login     = req.Login,
            SenhaHash = AuthService.HashSenha(req.Senha),
            Perfil    = req.Perfil,
            Email     = req.Email
        });

        return Ok(new ApiResponse<int>(true, "Usuário criado com sucesso.", novoId));
    }

    // PATCH api/usuarios/{id}/ativo
    [HttpPatch("{id}/ativo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleAtivo(int id, [FromBody] bool ativo)
    {
        var ok = await _db.ToggleAtivoAsync(id, ativo);
        return ok
            ? Ok(new ApiResponse(true, "Usuário atualizado."))
            : NotFound(new ApiResponse(false, "Usuário não encontrado."));
    }

[HttpPut("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Atualizar(
    int id,
    [FromBody] AtualizarUsuarioRequest req)
{
    var ok = await _db.AtualizarUsuarioAsync(
        id,
        req.Nome,
        req.Email,
        req.Perfil,
        req.Ativo
    );

    return ok
        ? Ok(new ApiResponse(true, "Usuário atualizado."))
        : NotFound(new ApiResponse(false, "Usuário não encontrado."));
}

}
