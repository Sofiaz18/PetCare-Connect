using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PetCareConnect.API.Models;

namespace PetCareConnect.API.Services;

public class AuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config) => _config = config;

    /// <summary>Gera token JWT para o usuário autenticado.</summary>
    public string GerarToken(Usuario usuario)
    {
        var key   = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, usuario.Nome),

            new Claim("login", usuario.Login),

            new Claim("perfil", usuario.Perfil),

            new Claim(ClaimTypes.Role, usuario.Perfil),

            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expHours = int.Parse(_config["Jwt:ExpiresInHours"] ?? "8");

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(expHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Verifica a senha usando BCrypt.</summary>
    public static string HashSenha(string senha)
    {
        return BCrypt.Net.BCrypt.HashPassword(senha);
    }

    public static bool VerificarSenha(
        string senha,
        string hash
    )
    {
        return BCrypt.Net.BCrypt.Verify(
            senha,
            hash
        );
    }
}