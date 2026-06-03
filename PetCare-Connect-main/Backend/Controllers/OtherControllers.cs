using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCareConnect.API.Data;
using PetCareConnect.API.DTOs;
using PetCareConnect.API.Models;

namespace PetCareConnect.API.Controllers;

// ═══════════════════════════════════════════════════════════════
//  CONSULTAS
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsultasController : ControllerBase
{
    private readonly PetCareDb _db;
    public ConsultasController(PetCareDb db) => _db = db;

    private int UserId
{
    get
    {
        var sub =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(sub, out var id)
            ? id
            : 0;
    }
}
    private string Perfil => User.FindFirst("perfil")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? petId,
        [FromQuery] int? vetId)
    {
        try
        {
            int? tutorFiltro = Perfil == "Tutor"        ? UserId : null;
            int? vetFiltro   = Perfil == "Veterinario"  ? UserId : vetId;

            var lista = await _db.ListarConsultasAsync(petId, vetFiltro, tutorFiltro);

            var resp = lista.Select(c => new ConsultaResponse(
                c.Id, c.PetId, c.PetNome, c.VeterinarioId, c.VetNome,
                c.Data.ToString("dd/MM/yyyy"),
                c.Hora.ToString(@"hh\:mm"),
                c.Status, c.Observacoes));

            return Ok(new ApiResponse<IEnumerable<ConsultaResponse>>(true, "OK", resp));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse(false, ex.Message));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> Criar([FromBody] CriarConsultaRequest req)
    {
        try
        {
            if (!DateTime.TryParseExact(req.Data, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var data))
                return BadRequest(new ApiResponse(false, "Data inválida. Use dd/MM/yyyy."));

            if (data.Date < DateTime.Today)
                return BadRequest(new ApiResponse(false, "Escolha uma data futura."));

            // Aceita HH:mm com hora de 0-23
            if (!TimeSpan.TryParse(req.Hora, out var hora))
                return BadRequest(new ApiResponse(false, "Hora inválida. Use HH:mm."));

            var conflito = await _db.VerificarConflitoHorarioAsync(req.VeterinarioId, data, hora);
            if (conflito)
                return Conflict(new ApiResponse(false, "Horário já reservado para este veterinário."));

            var pet = await _db.BuscarPetPorIdAsync(req.PetId);
            if (pet is null)
                return NotFound(new ApiResponse(false, "Pet não encontrado."));

            var id = await _db.CriarConsultaAsync(new Consulta
            {
                PetId         = req.PetId,
                VeterinarioId = req.VeterinarioId,
                Data          = data,
                Hora          = hora,
                Status        = "Marcada"
            });

            // Histórico automático
            await _db.AdicionarHistoricoAsync(new Historico
            {
                PetId      = req.PetId,
                Descricao  = $"Consulta agendada para {data:dd/MM/yyyy} às {req.Hora}.",
                DataEvento = DateTime.Today
            });

            // Aviso para o tutor
            await _db.CriarAvisoAsync(new Aviso
            {
                Titulo  = "Consulta agendada",
                Texto   = $"Consulta de {pet.Nome} agendada para {data:dd/MM/yyyy} às {req.Hora}.",
                Tipo    = "consulta",
                TutorId = pet.TutorId,
                PetId   = req.PetId
            });

            return Ok(new ApiResponse<int>(true, "Consulta agendada.", id));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse(false, ex.Message));
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Tutor,Veterinario,Admin")]
    public async Task<IActionResult> AtualizarStatus(
        int id,
        [FromQuery] string status,
        [FromQuery] string? obs)
    {
        var validos = new[] { "Marcada", "Realizada", "Cancelada" };
        if (!validos.Contains(status))
            return BadRequest(new ApiResponse(false, "Status inválido."));

        var ok = await _db.AtualizarStatusConsultaAsync(id, status, obs);
        return ok
            ? Ok(new ApiResponse(true, "Status atualizado."))
            : NotFound(new ApiResponse(false, "Consulta não encontrada."));
    }
}

// ═══════════════════════════════════════════════════════════════
//  VACINAS
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VacinasController : ControllerBase
{
    private readonly PetCareDb _db;
    public VacinasController(PetCareDb db) => _db = db;

    [HttpGet("{petId}")]
    public async Task<IActionResult> Listar(int petId)
    {
        var lista = await _db.ListarVacinasPorPetAsync(petId);
        return Ok(new ApiResponse<IEnumerable<Vacina>>(true, "OK", lista));
    }

    [HttpPost]
    [Authorize(Roles = "Veterinario,Admin")]
    public async Task<IActionResult> Criar([FromBody] CriarVacinaRequest req)
    {
        try
        {
            DateTime? dataAplicacao = null;
            if (!string.IsNullOrEmpty(req.DataAplicacao))
                dataAplicacao = DateTime.ParseExact(
                    req.DataAplicacao, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture);

            var id = await _db.CriarVacinaAsync(new Vacina
            {
                PetId         = req.PetId,
                Nome          = req.Nome,
                DataAplicacao = dataAplicacao,
                Observacao    = req.Observacao,
                Aplicada      = req.Aplicada
            });

            return Ok(new ApiResponse<int>(true, "Vacina registrada.", id));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse(false, ex.Message));
        }
    }

    [HttpPatch("{id}/aplicar")]
    [Authorize(Roles = "Veterinario,Admin")]
    public async Task<IActionResult> Aplicar(
        int id,
        [FromQuery] string data,
        [FromQuery] string? obs)
    {
        if (!DateTime.TryParseExact(data, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            return BadRequest(new ApiResponse(false, "Data inválida."));

        var ok = await _db.AplicarVacinaAsync(id, dt, obs);
        return ok
            ? Ok(new ApiResponse(true, "Vacina aplicada."))
            : NotFound(new ApiResponse(false, "Vacina não encontrada."));
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "Veterinario,Admin")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] CriarVacinaRequest req)
    {
        DateTime? dataAplicacao = null;

        if (!string.IsNullOrWhiteSpace(req.DataAplicacao))
        {
            if (DateTime.TryParse(req.DataAplicacao, out var dt))
            {
                dataAplicacao = dt;
            }
        }

        var ok = await _db.AtualizarVacinaAsync(
            id,
            req.Nome,
            dataAplicacao,
            req.Observacao
        );

        return ok
            ? Ok(new ApiResponse(true, "Vacina atualizada."))
            : NotFound(new ApiResponse(false, "Vacina não encontrada."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Veterinario,Admin")]
    public async Task<IActionResult> Excluir(int id)
    {
        var ok = await _db.ExcluirVacinaAsync(id);

        return ok
            ? Ok(new ApiResponse(true, "Vacina excluída."))
            : NotFound(new ApiResponse(false, "Vacina não encontrada."));
    }
}

// ═══════════════════════════════════════════════════════════════
//  HISTÓRICO
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HistoricoController : ControllerBase
{
    private readonly PetCareDb _db;
    public HistoricoController(PetCareDb db) => _db = db;

    [HttpGet("{petId}")]
    public async Task<IActionResult> Listar(int petId)
    {
        var lista = await _db.ListarHistoricoPorPetAsync(petId);
        return Ok(new ApiResponse<IEnumerable<Historico>>(true, "OK", lista));
    }

    [HttpPost]
    [Authorize(Roles = "Veterinario,Admin")]
    public async Task<IActionResult> Adicionar([FromBody] CriarHistoricoRequest req)
    {
        try
        {
            DateTime dataEvento = DateTime.Today;
            if (!string.IsNullOrEmpty(req.DataEvento))
                dataEvento = DateTime.ParseExact(
                    req.DataEvento, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture);

            var id = await _db.AdicionarHistoricoAsync(new Historico
            {
                PetId      = req.PetId,
                Descricao  = req.Descricao,
                DataEvento = dataEvento
            });

            return Ok(new ApiResponse<int>(true, "Histórico adicionado.", id));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse(false, ex.Message));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Veterinario,Admin")]
    public async Task<IActionResult> Excluir(int id)
    {
        var ok = await _db.ExcluirHistoricoAsync(id);

        return ok
            ? Ok(new ApiResponse(true, "Histórico excluído."))
            : NotFound(new ApiResponse(false, "Histórico não encontrado."));
    }
}

// ═══════════════════════════════════════════════════════════════
//  AVISOS
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AvisosController : ControllerBase
{
    private readonly PetCareDb _db;

    public AvisosController(PetCareDb db)
    {
        _db = db;
    }

    private int UserId
    {
        get
        {
            var sub =
                User.FindFirst("sub")?.Value ??
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(sub, out var id)
                ? id
                : 0;
        }
    }

    private string Perfil =>
        User.FindFirst("perfil")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lista = await _db.ListarAvisosAsync(
            UserId,
            Perfil,
            UserId
        );

        return Ok(
            new ApiResponse<IEnumerable<Aviso>>(
                true,
                "OK",
                lista
            )
        );
    }

    [HttpPost]
    [Authorize(Roles = "Veterinario,Admin")]
    public async Task<IActionResult> Criar(
        [FromBody] CriarAvisoRequest req)
    {
        var id = await _db.CriarAvisoAsync(
            new Aviso
            {
                Titulo = req.Titulo,
                Texto = req.Texto,
                Tipo = req.Tipo,
                TutorId = req.TutorId,
                PetId = req.PetId
            });

        return Ok(
            new ApiResponse<int>(
                true,
                "Aviso criado.",
                id
            )
        );
    }

    [HttpPost("{id}/lido")]
    public async Task<IActionResult> MarcarLido(int id)
    {
        await _db.MarcarAvisoComoLidoAsync(
            id,
            UserId
        );

        return Ok(
            new ApiResponse(
                true,
                "Marcado como lido."
            )
        );
    }
}

// ═══════════════════════════════════════════════════════════════
//  RELATÓRIOS
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelatoriosController : ControllerBase
{
    private readonly PetCareDb _db;
    public RelatoriosController(PetCareDb db) => _db = db;

    private int    UserId => int.Parse(User.FindFirst("sub")?.Value ?? "0");
    private string Perfil => User.FindFirst("perfil")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? petId)
    {
        int? tutorFiltro = null;
        var lista = await _db.ListarRelatoriosAsync(petId, tutorFiltro);
        return Ok(new ApiResponse<IEnumerable<Relatorio>>(true, "OK", lista));
    }

    [HttpPost]
    [Authorize(Roles = "Veterinario,Admin")]
    public async Task<IActionResult> Salvar([FromBody] SalvarRelatorioRequest req)
    {
        await _db.UpsertRelatorioAsync(req.PetId, req.Texto);
        return Ok(new ApiResponse(true, "Relatório salvo."));
    }

    [HttpPatch("{petId}")]
[Authorize(Roles = "Veterinario,Admin")]
public async Task<IActionResult> Atualizar(
    int petId,
    [FromBody] SalvarRelatorioRequest req)
{
    var ok = await _db.AtualizarRelatorioAsync(
        petId,
        req.Texto
    );

    return ok
        ? Ok(new ApiResponse(true, "Relatório atualizado."))
        : NotFound(new ApiResponse(false, "Relatório não encontrado."));
}

[HttpDelete("{petId}")]
[Authorize(Roles = "Veterinario,Admin")]
public async Task<IActionResult> Excluir(int petId)
{
    var ok = await _db.ExcluirRelatorioAsync(petId);

    return ok
        ? Ok(new ApiResponse(true, "Relatório excluído."))
        : NotFound(new ApiResponse(false, "Relatório não encontrado."));
}
}

// ═══════════════════════════════════════════════════════════════
//  ADMIN — Dashboard e Estatísticas
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly PetCareDb _db;
    public AdminController(PetCareDb db) => _db = db;

    [HttpGet("estatisticas")]
    public async Task<IActionResult> Estatisticas()
    {
        var stats = await _db.EstatisticasAsync();
        return Ok(new ApiResponse<Dictionary<string, int>>(true, "OK", stats));
    }
}
