using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using PetCareConnect.API.Models;

namespace PetCareConnect.API.Data;

public class PetCareDb
{
    private readonly IConfiguration _config;

    public PetCareDb(IConfiguration config)
    {
        _config = config;
    }

    private IDbConnection Connection =>
        new SqlConnection(
            _config.GetConnectionString("DefaultConnection")
        );

    // =====================================================
    // USUÁRIOS
    // =====================================================

    public async Task<Usuario?> BuscarUsuarioPorLoginAsync(string login)
    {
        using var conn = Connection;
        return await conn.QueryFirstOrDefaultAsync<Usuario>(
            "SELECT * FROM Usuarios WHERE Login = @login",
            new { login }
        );
    }

    public async Task<Usuario?> BuscarPorLoginAsync(string login)
        => await BuscarUsuarioPorLoginAsync(login);

    public async Task<Usuario?> BuscarPorIdAsync(int id)
    {
        using var conn = Connection;
        return await conn.QueryFirstOrDefaultAsync<Usuario>(
            "SELECT * FROM Usuarios WHERE Id = @id",
            new { id }
        );
    }

    public async Task<IEnumerable<Usuario>> ListarUsuariosAsync(string? perfil = null)
    {
        using var conn = Connection;
        var sql = "SELECT * FROM Usuarios";
        if (!string.IsNullOrEmpty(perfil))
            sql += " WHERE Perfil = @perfil ORDER BY Nome";
        else
            sql += " ORDER BY Nome";
        return await conn.QueryAsync<Usuario>(sql, new { perfil });
    }

    public async Task<int> CriarUsuarioAsync(Usuario usuario)
    {
        using var conn = Connection;
        var sql = @"
            INSERT INTO Usuarios (Nome, Login, Email, SenhaHash, Perfil, Ativo, CriadoEm)
            VALUES (@Nome, @Login, @Email, @SenhaHash, @Perfil, 1, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await conn.ExecuteScalarAsync<int>(sql, usuario);
    }

    public async Task<bool> ToggleAtivoAsync(int id, bool ativo)
    {
        using var conn = Connection;
        var rows = await conn.ExecuteAsync(
            "UPDATE Usuarios SET Ativo = @ativo WHERE Id = @id",
            new { id, ativo }
        );
        return rows > 0;
    }

    public async Task<bool> AtualizarSenhaAsync(int usuarioId, string novaHash)
    {
        using var conn = Connection;
        var rows = await conn.ExecuteAsync(
            "UPDATE Usuarios SET SenhaHash = @novaHash WHERE Id = @usuarioId",
            new { usuarioId, novaHash }
        );
        return rows > 0;
    }

    public async Task<bool> AtualizarSenhaPorLoginAsync(string login, string novaHash)
    {
        using var conn = Connection;
        var rows = await conn.ExecuteAsync(
            "UPDATE Usuarios SET SenhaHash = @novaHash WHERE Login = @login",
            new { login, novaHash }
        );
        return rows > 0;
    }

    public async Task<Usuario?> BuscarUsuarioPorEmailAsync(string email)
    {
        using var conn = Connection;
        return await conn.QueryFirstOrDefaultAsync<Usuario>(
            "SELECT * FROM Usuarios WHERE Email = @email",
            new { email }
        );
    }

    // =====================================================
    // PETS
    // =====================================================

    public async Task<IEnumerable<Pet>> ListarPetsAsync(int? tutorId = null)
    {
        using var conn = Connection;
        var sql = @"
            SELECT p.*, u.Nome AS TutorNome
            FROM Pets p
            INNER JOIN Usuarios u ON u.Id = p.TutorId";
        if (tutorId.HasValue)
            sql += " WHERE p.TutorId = @tutorId";
        sql += " ORDER BY p.Nome";
        return await conn.QueryAsync<Pet>(sql, new { tutorId });
    }

    public async Task<Pet?> BuscarPetPorIdAsync(int id)
    {
        using var conn = Connection;
        return await conn.QueryFirstOrDefaultAsync<Pet>(@"
            SELECT p.*, u.Nome AS TutorNome
            FROM Pets p
            INNER JOIN Usuarios u ON u.Id = p.TutorId
            WHERE p.Id = @id",
            new { id }
        );
    }

    public async Task<int> CriarPetAsync(Pet pet)
    {
        using var conn = Connection;
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Pets (Nome, Tipo, Idade, Peso, TutorId)
            VALUES (@Nome, @Tipo, @Idade, @Peso, @TutorId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            pet
        );
    }

    public async Task AtualizarPetAsync(int id, int idade, decimal peso)
    {
        using var conn = Connection;
        await conn.ExecuteAsync(
            "UPDATE Pets SET Idade = @idade, Peso = @peso WHERE Id = @id",
            new { id, idade, peso }
        );
    }

    public async Task RemoverPetAsync(int id)
    {
        using var conn = Connection;
        // Remove dependentes na ordem correta (FK)
        await conn.ExecuteAsync("DELETE FROM AvisosLidos WHERE AvisoId IN (SELECT Id FROM Avisos WHERE PetId = @id)", new { id });
        await conn.ExecuteAsync("DELETE FROM Avisos    WHERE PetId = @id", new { id });
        await conn.ExecuteAsync("DELETE FROM Vacinas   WHERE PetId = @id", new { id });
        await conn.ExecuteAsync("DELETE FROM Historico WHERE PetId = @id", new { id });
        await conn.ExecuteAsync("DELETE FROM Relatorios WHERE PetId = @id", new { id });
        await conn.ExecuteAsync("DELETE FROM Consultas WHERE PetId = @id", new { id });
        await conn.ExecuteAsync("DELETE FROM Pets      WHERE Id    = @id", new { id });
    }

    // =====================================================
    // CONSULTAS
    // =====================================================

    public async Task<IEnumerable<Consulta>> ListarConsultasAsync(
        int? petId, int? vetId, int? tutorId)
    {
        using var conn = Connection;
        var sql = @"
            SELECT c.*, p.Nome AS PetNome, u.Nome AS VetNome
            FROM Consultas c
            INNER JOIN Pets     p ON p.Id = c.PetId
            INNER JOIN Usuarios u ON u.Id = c.VeterinarioId
            WHERE 1=1";

        if (petId.HasValue)   sql += " AND c.PetId = @petId";
        if (vetId.HasValue)   sql += " AND c.VeterinarioId = @vetId";
        if (tutorId.HasValue) sql += " AND p.TutorId = @tutorId";
        sql += " ORDER BY c.Data DESC, c.Hora";

        return await conn.QueryAsync<Consulta>(sql, new { petId, vetId, tutorId });
    }

    public async Task<bool> VerificarConflitoHorarioAsync(
        int vetId, DateTime data, TimeSpan hora)
    {
        using var conn = Connection;
        var total = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Consultas
            WHERE VeterinarioId = @vetId
              AND Data = @data
              AND Hora = @hora
              AND Status != 'Cancelada'",
            new { vetId, data, hora }
        );
        return total > 0;
    }

    public async Task<int> CriarConsultaAsync(Consulta consulta)
    {
        using var conn = Connection;
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Consultas (PetId, VeterinarioId, Data, Hora, Status)
            VALUES (@PetId, @VeterinarioId, @Data, @Hora, @Status);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            consulta
        );
    }

    public async Task<bool> AtualizarStatusConsultaAsync(
        int id, string status, string? obs)
    {
        using var conn = Connection;
        var rows = await conn.ExecuteAsync(
            "UPDATE Consultas SET Status = @status, Observacoes = @obs WHERE Id = @id",
            new { id, status, obs }
        );
        return rows > 0;
    }

    // =====================================================
    // VACINAS
    // =====================================================

    public async Task<IEnumerable<Vacina>> ListarVacinasPorPetAsync(int petId)
    {
        using var conn = Connection;
        return await conn.QueryAsync<Vacina>(
            "SELECT * FROM Vacinas WHERE PetId = @petId ORDER BY DataAplicacao DESC",
            new { petId }
        );
    }

    public async Task<int> CriarVacinaAsync(Vacina vacina)
    {
        using var conn = Connection;
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Vacinas (PetId, Nome, DataAplicacao, Observacao, Aplicada)
            VALUES (@PetId, @Nome, @DataAplicacao, @Observacao, @Aplicada);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            vacina
        );
    }

    public async Task<bool> AplicarVacinaAsync(int id, DateTime data, string? obs)
    {
        using var conn = Connection;
        var rows = await conn.ExecuteAsync(
            "UPDATE Vacinas SET Aplicada = 1, DataAplicacao = @data, Observacao = @obs WHERE Id = @id",
            new { id, data, obs }
        );
        return rows > 0;
    }

    public async Task<bool> AtualizarVacinaAsync(
        int id,
        string nome,
        DateTime? dataAplicacao,
        string? observacao)
    {
        using var conn = Connection;

        var linhas = await conn.ExecuteAsync(@"
            UPDATE Vacinas
            SET Nome = @nome,
                DataAplicacao = @dataAplicacao,
                Observacao = @observacao
            WHERE Id = @id",
            new
            {
                id,
                nome,
                dataAplicacao,
                observacao
            });

        return linhas > 0;
    }

    public async Task<bool> ExcluirVacinaAsync(int id)
    {
        using var conn = Connection;

        var linhas = await conn.ExecuteAsync(@"
            DELETE FROM Vacinas
            WHERE Id = @id",
            new { id });

        return linhas > 0;
    }

    // =====================================================
    // HISTÓRICO  (tabela: Historico — sem 's')
    // =====================================================

    public async Task<IEnumerable<Historico>> ListarHistoricoPorPetAsync(int petId)
    {
        using var conn = Connection;
        return await conn.QueryAsync<Historico>(
            "SELECT * FROM Historico WHERE PetId = @petId ORDER BY DataEvento DESC",
            new { petId }
        );
    }

    public async Task<int> AdicionarHistoricoAsync(Historico historico)
    {
        using var conn = Connection;
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Historico (PetId, Descricao, DataEvento)
            VALUES (@PetId, @Descricao, @DataEvento);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            historico
        );
    }

    public async Task<bool> ExcluirHistoricoAsync(int id)
    {
        using var conn = Connection;

        var linhas = await conn.ExecuteAsync(@"
            DELETE FROM Historico
            WHERE Id = @id",
            new { id });

        return linhas > 0;
    }

    // =====================================================
    // AVISOS  (leitura via tabela AvisosLidos)
    // =====================================================

    public async Task<IEnumerable<Aviso>> ListarAvisosAsync(
        int userId, string perfil, int tutorId)
    {
        using var conn = Connection;
        // Tutores veem avisos gerais + os direcionados a eles
        // Vet/Admin veem todos
        string sql;
        if (perfil == "Tutor")
        {
            sql = @"
                SELECT a.*,
                    CASE WHEN al.UsuarioId IS NOT NULL
                         THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT)
                    END AS Lido
                FROM Avisos a
                LEFT JOIN AvisosLidos al
                    ON al.AvisoId = a.Id AND al.UsuarioId = @userId
                WHERE a.Tipo = 'geral' OR a.TutorId = @tutorId
                ORDER BY a.CriadoEm DESC";
        }
        else
        {
            sql = @"
                SELECT a.*,
                    CASE WHEN al.UsuarioId IS NOT NULL
                         THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT)
                    END AS Lido
                FROM Avisos a
                LEFT JOIN AvisosLidos al
                    ON al.AvisoId = a.Id AND al.UsuarioId = @userId
                ORDER BY a.CriadoEm DESC";
        }
        return await conn.QueryAsync<Aviso>(sql, new { userId, tutorId });
    }

    public async Task<int> CriarAvisoAsync(Aviso aviso)
    {
        using var conn = Connection;
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Avisos (Titulo, Texto, Tipo, TutorId, PetId)
            VALUES (@Titulo, @Texto, @Tipo, @TutorId, @PetId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            aviso
        );
    }

    public async Task MarcarAvisoComoLidoAsync(int avisoId, int userId)
    {
        using var conn = Connection;
        // Insere na tabela N:N AvisosLidos (ignora duplicata)
        await conn.ExecuteAsync(@"
            IF NOT EXISTS (
                SELECT 1 FROM AvisosLidos
                WHERE AvisoId = @avisoId AND UsuarioId = @userId
            )
            INSERT INTO AvisosLidos (AvisoId, UsuarioId)
            VALUES (@avisoId, @userId)",
            new { avisoId, userId }
        );
    }

    // =====================================================
    // RELATÓRIOS
    // =====================================================

    public async Task<IEnumerable<Relatorio>> ListarRelatoriosAsync(
        int? petId, int? tutorId)
    {
        using var conn = Connection;
        var sql = @"
            SELECT r.*, p.Nome AS PetNome, u.Nome AS TutorNome
            FROM Relatorios r
            INNER JOIN Pets     p ON p.Id = r.PetId
            INNER JOIN Usuarios u ON u.Id = p.TutorId
            WHERE 1=1";

        if (petId.HasValue)   sql += " AND r.PetId   = @petId";
        if (tutorId.HasValue) sql += " AND p.TutorId = @tutorId";
        sql += " ORDER BY r.Atualizacao DESC";

        return await conn.QueryAsync<Relatorio>(sql, new { petId, tutorId });
    }

    public async Task UpsertRelatorioAsync(int petId, string texto)
    {
        using var conn = Connection;
        await conn.ExecuteAsync(@"
            IF EXISTS (SELECT 1 FROM Relatorios WHERE PetId = @petId)
                UPDATE Relatorios
                SET Texto = @texto, Atualizacao = GETDATE()
                WHERE PetId = @petId
            ELSE
                INSERT INTO Relatorios (PetId, Texto)
                VALUES (@petId, @texto)",
            new { petId, texto }
        );
    }

    public async Task<bool> AtualizarRelatorioAsync(
    int petId,
    string texto)
{
    using var conn = Connection;

    var linhas = await conn.ExecuteAsync(@"
        UPDATE Relatorios
        SET Texto = @texto,
            Atualizacao = GETDATE()
        WHERE PetId = @petId",
        new { petId, texto });

    return linhas > 0;
}

public async Task<bool> ExcluirRelatorioAsync(int petId)
{
    using var conn = Connection;

    var linhas = await conn.ExecuteAsync(@"
        DELETE FROM Relatorios
        WHERE PetId = @petId",
        new { petId });

    return linhas > 0;
}

    // =====================================================
    // ESTATÍSTICAS (Admin Dashboard)
    // =====================================================

    public async Task<Dictionary<string, int>> EstatisticasAsync()
    {
        using var conn = Connection;
        var rows = await conn.QueryAsync<(string K, int V)>(@"
            SELECT 'Usuarios'         AS K, COUNT(*) AS V FROM Usuarios
            UNION ALL
            SELECT 'Pets',                  COUNT(*)       FROM Pets
            UNION ALL
            SELECT 'Consultas',             COUNT(*)       FROM Consultas
            UNION ALL
            SELECT 'ConsultasHoje',         COUNT(*)       FROM Consultas
                WHERE Data = CAST(GETDATE() AS DATE)
            UNION ALL
            SELECT 'VacinasAplicadas',      COUNT(*)       FROM Vacinas WHERE Aplicada = 1
            UNION ALL
            SELECT 'Tutores',               COUNT(*)       FROM Usuarios WHERE Perfil = 'Tutor'
            UNION ALL
            SELECT 'Veterinarios',          COUNT(*)       FROM Usuarios WHERE Perfil = 'Veterinario'");

        return rows.ToDictionary(x => x.K, x => x.V);
    }


    public async Task<bool> AtualizarUsuarioAsync(
        int id,
        string nome,
        string email,
        string perfil,
        bool ativo)
    {
        using var conn = Connection;

        var linhas = await conn.ExecuteAsync(@"
            UPDATE Usuarios
            SET
                Nome = @nome,
                Email = @email,
                Perfil = @perfil,
                Ativo = @ativo
            WHERE Id = @id",
            new
            {
                id,
                nome,
                email,
                perfil,
                ativo
            });

        return linhas > 0;
    }
}