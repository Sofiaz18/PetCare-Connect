namespace PetCareConnect.API.Models;

// ── Usuário ─────────────────────────────────────────────────
public class Usuario
{
    public int      Id        { get; set; }
    public string   Nome      { get; set; } = "";
    public string   Login     { get; set; } = "";
    public string   SenhaHash { get; set; } = "";
    public string   Perfil    { get; set; } = "";   // Tutor | Veterinario | Admin
    public string?  Email     { get; set; }
    public bool     Ativo     { get; set; } = true;
    public DateTime CriadoEm  { get; set; }
}

// ── Pet ──────────────────────────────────────────────────────
public class Pet
{
    public int      Id       { get; set; }
    public string   Nome     { get; set; } = "";
    public string   Tipo     { get; set; } = "";
    public int      Idade    { get; set; }
    public decimal  Peso     { get; set; }
    public int      TutorId  { get; set; }
    public DateTime CriadoEm { get; set; }

    // Não mapeado — preenchido manualmente via JOIN
    public string TutorNome { get; set; } = "";
}

// ── Consulta ─────────────────────────────────────────────────
public class Consulta
{
    public int      Id              { get; set; }
    public int      PetId           { get; set; }
    public int      VeterinarioId   { get; set; }
    public DateTime Data            { get; set; }
    public TimeSpan Hora            { get; set; }
    public string   Status          { get; set; } = "Marcada";
    public string?  Observacoes     { get; set; }
    public DateTime CriadoEm       { get; set; }

    // Não mapeado
    public string PetNome      { get; set; } = "";
    public string VetNome      { get; set; } = "";
}

// ── Vacina ───────────────────────────────────────────────────
public class Vacina
{
    public int       Id            { get; set; }
    public int       PetId         { get; set; }
    public string    Nome          { get; set; } = "";
    public DateTime? DataAplicacao { get; set; }
    public string?   Observacao    { get; set; }
    public bool      Aplicada      { get; set; }
    public DateTime  CriadoEm     { get; set; }
}

// ── Histórico ────────────────────────────────────────────────
public class Historico
{
    public int      Id          { get; set; }
    public int      PetId       { get; set; }
    public string   Descricao   { get; set; } = "";
    public DateTime DataEvento  { get; set; }
    public DateTime CriadoEm   { get; set; }
}

// ── Aviso ────────────────────────────────────────────────────
public class Aviso
{
    public int      Id       { get; set; }
    public string   Titulo   { get; set; } = "";
    public string   Texto    { get; set; } = "";
    public string   Tipo     { get; set; } = "geral";
    public int?     TutorId  { get; set; }
    public int?     PetId    { get; set; }
    public DateTime CriadoEm { get; set; }
    public bool     Lido     { get; set; } = false;   // preenchido no serviço
}

// ── Relatório ────────────────────────────────────────────────
public class Relatorio
{
    public int      Id          { get; set; }
    public int      PetId       { get; set; }
    public string   Texto       { get; set; } = "";
    public DateTime Atualizacao { get; set; }

    // Não mapeado
    public string PetNome   { get; set; } = "";
    public string TutorNome { get; set; } = "";
}
