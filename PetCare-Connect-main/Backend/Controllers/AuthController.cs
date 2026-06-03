using Microsoft.AspNetCore.Mvc;
using PetCareConnect.API.Data;
using PetCareConnect.API.Models;
using PetCareConnect.API.Services;

namespace PetCareConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly PetCareDb _db;
    private readonly AuthService _auth;

    public AuthController(
        PetCareDb db,
        AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    // =====================================================
    // LOGIN
    // =====================================================

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest req)
    {
        try
        {
            var usuario =
                await _db.BuscarUsuarioPorLoginAsync(
                    req.Login
                );

            if (usuario == null)
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Usuário não encontrado."
                });
            }

            if (!usuario.Ativo)
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Usuário desativado."
                });
            }

            var senhaOk =
                AuthService.VerificarSenha(
                    req.Senha,
                    usuario.SenhaHash
                );

            if (!senhaOk)
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Senha inválida."
                });
            }

            if (
                !string.Equals(
                    usuario.Perfil,
                    req.Perfil,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Perfil incorreto."
                });
            }

            var token =
                _auth.GerarToken(usuario);

            return Ok(new
            {
                sucesso = true,

                dados = new
                {
                    token,

                    usuarioId =
                        usuario.Id,

                    nome =
                        usuario.Nome,

                    login =
                        usuario.Login,

                    perfil =
                        usuario.Perfil
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                sucesso = false,
                mensagem = ex.Message
            });
        }
    }

    // =====================================================
    // CADASTRO
    // =====================================================

    [HttpPost("cadastro")]
    public async Task<IActionResult> Cadastro(
        [FromBody] CadastroRequest req)
    {
        try
        {
            if (
                string.IsNullOrWhiteSpace(req.Nome) ||
                string.IsNullOrWhiteSpace(req.Login) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Senha)
            )
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Preencha todos os campos."
                });
            }

            var existente =
                await _db.BuscarPorLoginAsync(
                    req.Login
                );

            if (existente != null)
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Login já cadastrado."
                });
            }

            var usuario = new Usuario
            {
                Nome = req.Nome,
                Login = req.Login,
                Email = req.Email,
                Perfil = "Tutor",
                Ativo = true,

                SenhaHash =
                    AuthService.HashSenha(
                        req.Senha
                    )
            };

            var novoId =
                await _db.CriarUsuarioAsync(
                    usuario
                );

            return Ok(new
            {
                sucesso = true,
                mensagem = "Conta criada com sucesso.",
                usuarioId = novoId
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                sucesso = false,
                mensagem = ex.Message
            });
        }
    }

    // =====================================================
    // RECUPERAR SENHA
    // =====================================================

    [HttpPost("recuperar-senha")]
    public async Task<IActionResult> RecuperarSenha(
        [FromBody] RecuperarSenhaRequest req)
    {
        try
        {
            if (
                string.IsNullOrWhiteSpace(req.Login) ||
                string.IsNullOrWhiteSpace(req.Email)
            )
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Preencha login e email."
                });
            }

            var usuario =
                await _db.BuscarUsuarioPorLoginAsync(
                    req.Login
                );

            if (
                usuario == null ||
                usuario.Email != req.Email
            )
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Usuário não encontrado."
                });
            }

            // senha temporária
            var novaSenha = "123456";

            // gera nova hash
            var novaHash =
                AuthService.HashSenha(
                    novaSenha
                );

            // atualiza no banco
            var ok =
                await _db.AtualizarSenhaAsync(
                    usuario.Id,
                    novaHash
                );

            if (!ok)
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Erro ao atualizar senha."
                });
            }

            return Ok(new
            {
                sucesso = true,
                mensagem =
                    $"Nova senha temporária: {novaSenha}"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                sucesso = false,
                mensagem = ex.Message
            });
        }
    }

    // =====================================================
    // NOVA SENHA
    // =====================================================

    [HttpPost("nova-senha")]
    public async Task<IActionResult> NovaSenha(
        [FromBody] NovaSenhaRequest req)
    {
        try
        {
            if (
                string.IsNullOrWhiteSpace(req.Login) ||
                string.IsNullOrWhiteSpace(req.NovaSenha)
            )
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Preencha os campos."
                });
            }

            var usuario =
                await _db.BuscarUsuarioPorLoginAsync(
                    req.Login
                );

            if (usuario == null)
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Usuário não encontrado."
                });
            }

            // gera nova hash
            var novaHash =
                AuthService.HashSenha(
                    req.NovaSenha
                );

            // atualiza no banco
            var ok =
                await _db.AtualizarSenhaPorLoginAsync(
                    req.Login,
                    novaHash
                );

            if (!ok)
            {
                return BadRequest(new
                {
                    sucesso = false,
                    mensagem = "Erro ao alterar senha."
                });
            }

            return Ok(new
            {
                sucesso = true,
                mensagem = "Senha alterada com sucesso."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                sucesso = false,
                mensagem = ex.Message
            });
        }
    }
}

// =====================================================
// DTO LOGIN
// =====================================================

public class LoginRequest
{
    public string Login { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;

    public string Perfil { get; set; } = string.Empty;
}

// =====================================================
// DTO CADASTRO
// =====================================================

public class CadastroRequest
{
    public string Nome { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;
}

// =====================================================
// DTO RECUPERAR SENHA
// =====================================================

public class RecuperarSenhaRequest
{
    public string Login { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

// =====================================================
// DTO NOVA SENHA
// =====================================================

public class NovaSenhaRequest
{
    public string Login { get; set; } = string.Empty;

    public string NovaSenha { get; set; } = string.Empty;
}