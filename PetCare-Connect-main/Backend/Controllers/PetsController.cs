using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCareConnect.API.Data;
using PetCareConnect.API.DTOs;
using PetCareConnect.API.Models;

namespace PetCareConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PetsController : ControllerBase
{
    private readonly PetCareDb _db;

    public PetsController(PetCareDb db)
    {
        _db = db;
    }

    // =====================================================
    // USER ID DO TOKEN JWT
    // =====================================================

    private int UserId
    {
        get
        {
            var claim =
                User.Claims.FirstOrDefault(c =>
                    c.Type == "sub"
                    || c.Type == "id"
                    || c.Type.Contains("nameidentifier"));

            return int.Parse(claim?.Value ?? "0");
        }
    }

    private string Perfil =>
        User.FindFirst("perfil")?.Value ?? "";

    // =====================================================
    // LISTAR PETS
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try
        {
            Console.WriteLine($"USER ID TOKEN: {UserId}");
            Console.WriteLine($"PERFIL: {Perfil}");

            int? filtroTutor =
                Perfil == "Tutor"
                    ? UserId
                    : null;

            var pets =
                await _db.ListarPetsAsync(filtroTutor);

            var resp = pets.Select(p => new PetResponse(
                p.Id,
                p.Nome,
                p.Tipo,
                p.Idade,
                p.Peso,
                p.TutorId,
                p.TutorNome,
                p.CriadoEm
            ));

            return Ok(
                new ApiResponse<IEnumerable<PetResponse>>(
                    true,
                    "OK",
                    resp
                )
            );
        }
        catch(Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse(
                    false,
                    ex.Message
                )
            );
        }
    }

    // =====================================================
    // BUSCAR PET
    // =====================================================

    [HttpGet("{id}")]
    public async Task<IActionResult> Buscar(int id)
    {
        try
        {
            var pet =
                await _db.BuscarPetPorIdAsync(id);

            if(pet is null)
            {
                return NotFound(
                    new ApiResponse(
                        false,
                        "Pet não encontrado."
                    )
                );
            }

            if(
                Perfil == "Tutor"
                &&
                pet.TutorId != UserId
            )
            {
                return Forbid();
            }

            return Ok(
                new ApiResponse<PetResponse>(
                    true,
                    "OK",
                    new PetResponse(
                        pet.Id,
                        pet.Nome,
                        pet.Tipo,
                        pet.Idade,
                        pet.Peso,
                        pet.TutorId,
                        pet.TutorNome,
                        pet.CriadoEm
                    )
                )
            );
        }
        catch(Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse(
                    false,
                    ex.Message
                )
            );
        }
    }

    // =====================================================
    // CRIAR PET
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> Criar(
        [FromBody] CriarPetRequest req
    )
    {
        try
        {
            if(
                string.IsNullOrWhiteSpace(req.Nome)
                ||
                string.IsNullOrWhiteSpace(req.Tipo)
            )
            {
                return BadRequest(
                    new ApiResponse(
                        false,
                        "Nome e tipo são obrigatórios."
                    )
                );
            }

            if(req.Idade <= 0)
            {
                return BadRequest(
                    new ApiResponse(
                        false,
                        "Idade deve ser maior que zero."
                    )
                );
            }

            if(req.Peso < 0)
            {
                return BadRequest(
                    new ApiResponse(
                        false,
                        "Peso não pode ser negativo."
                    )
                );
            }

            Console.WriteLine($"TUTOR ID RECEBIDO: {req.TutorId}");

            var id =
                await _db.CriarPetAsync(
                    new Pet
                    {
                        Nome = req.Nome,
                        Tipo = req.Tipo,
                        Idade = req.Idade,
                        Peso = req.Peso,
                        TutorId = req.TutorId
                    }
                );

            return Ok(
                new ApiResponse<int>(
                    true,
                    "Pet cadastrado com sucesso.",
                    id
                )
            );
        }
        catch(Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse(
                    false,
                    ex.Message
                )
            );
        }
    }

    // =====================================================
    // ATUALIZAR PET
    // =====================================================

    [HttpPatch("{id}")]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] AtualizarPetRequest req
    )
    {
        try
        {
            var pet =
                await _db.BuscarPetPorIdAsync(id);

            if(pet is null)
            {
                return NotFound(
                    new ApiResponse(
                        false,
                        "Pet não encontrado."
                    )
                );
            }

            if(
                Perfil == "Tutor"
                &&
                pet.TutorId != UserId
            )
            {
                return Forbid();
            }

            await _db.AtualizarPetAsync(
                id,
                req.Idade,
                req.Peso
            );

            return Ok(
                new ApiResponse(
                    true,
                    "Pet atualizado."
                )
            );
        }
        catch(Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse(
                    false,
                    ex.Message
                )
            );
        }
    }

    // =====================================================
    // REMOVER PET
    // =====================================================

    [HttpDelete("{id}")]
    [Authorize(Roles = "Tutor,Admin")]
    public async Task<IActionResult> Remover(int id)
    {
        try
        {
            var pet =
                await _db.BuscarPetPorIdAsync(id);

            if(pet is null)
            {
                return NotFound(
                    new ApiResponse(
                        false,
                        "Pet não encontrado."
                    )
                );
            }

            if(
                Perfil == "Tutor"
                &&
                pet.TutorId != UserId
            )
            {
                return Forbid();
            }

            await _db.RemoverPetAsync(id);

            return Ok(
                new ApiResponse(
                    true,
                    "Pet removido."
                )
            );
        }
        catch(Exception ex)
        {
            return StatusCode(
                500,
                new ApiResponse(
                    false,
                    ex.Message
                )
            );
        }
    }
}