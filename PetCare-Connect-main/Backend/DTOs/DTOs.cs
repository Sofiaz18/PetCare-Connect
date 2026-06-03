namespace PetCareConnect.API.DTOs;

// ── Usuário ─────────────────────────────────────────────────
public record CriarUsuarioRequest(
    string  Nome,
    string  Login,
    string  Senha,
    string  Perfil,
    string? Email
);

public record UsuarioResponse(
    int      Id,
    string   Nome,
    string   Login,
    string   Perfil,
    string?  Email,
    bool     Ativo,
    DateTime CriadoEm
);

// ── Pet ──────────────────────────────────────────────────────
public record CriarPetRequest(
    string  Nome,
    string  Tipo,
    int     Idade,
    decimal Peso,
    int     TutorId
);

public record AtualizarPetRequest(
    int     Idade,
    decimal Peso
);

public record PetResponse(
    int      Id,
    string   Nome,
    string   Tipo,
    int      Idade,
    decimal  Peso,
    int      TutorId,
    string   TutorNome,
    DateTime CriadoEm
);

// ── Consulta ─────────────────────────────────────────────────
public record CriarConsultaRequest(
    int    PetId,
    int    VeterinarioId,
    string Data,   // dd/MM/yyyy
    string Hora    // HH:mm
);

public record ConsultaResponse(
    int     Id,
    int     PetId,
    string  PetNome,
    int     VeterinarioId,
    string  VetNome,
    string  Data,
    string  Hora,
    string  Status,
    string? Observacoes
);

// ── Vacina ───────────────────────────────────────────────────
public record CriarVacinaRequest(
    int     PetId,
    string  Nome,
    string? DataAplicacao,  // dd/MM/yyyy ou null
    string? Observacao,
    bool    Aplicada
);

// ── Histórico ────────────────────────────────────────────────
public record CriarHistoricoRequest(
    int     PetId,
    string  Descricao,
    string? DataEvento  // dd/MM/yyyy ou null
);

// ── Aviso ────────────────────────────────────────────────────
public record CriarAvisoRequest(
    string  Titulo,
    string  Texto,
    string  Tipo,
    int?    TutorId,
    int?    PetId
);

// ── Relatório ────────────────────────────────────────────────
public record SalvarRelatorioRequest(
    int    PetId,
    string Texto
);

// ── Genérico ─────────────────────────────────────────────────
public record ApiResponse<T>(bool Sucesso, string Mensagem, T? Dados);
public record ApiResponse(bool Sucesso, string Mensagem);

public record AtualizarUsuarioRequest(
    string Nome,
    string Email,
    string Perfil,
    bool Ativo
);