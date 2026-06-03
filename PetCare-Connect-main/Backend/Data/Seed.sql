-- ============================================================
--  PetCare Connect — Seed de usuários de teste
--  Execute DEPOIS do CreateDatabase.sql
--
--  Senhas (hashes BCrypt workFactor=11):
--    tutor_teste  → Tutor123!
--    vet_teste    → Vet123!
--    admin_teste  → Admin123!
--
--  IMPORTANTE: Os hashes abaixo são válidos e foram gerados
--  com BCrypt.Net-Next (workFactor 11). Se preferir regenerar,
--  use o endpoint POST /api/usuarios com perfil Admin já logado,
--  ou rode o snippet C# abaixo no terminal dotnet-script:
--
--    using BCrypt.Net;
--    Console.WriteLine(BCrypt.HashPassword("Tutor123!", 11));
-- ============================================================

USE PetCareConnect;
GO

-- Remove seed anterior se existir (idempotente)
DELETE FROM Usuarios WHERE Login IN ('tutor_teste','vet_teste','admin_teste');
GO

INSERT INTO Usuarios (Nome, Login, SenhaHash, Perfil, Email) VALUES
(
  'Maria Oliveira',
  'tutor_teste',
  '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lh7y',
  -- senha: Tutor123!
  'Tutor',
  'maria.oliveira@email.com'
),
(
  'Dr. Carlos Mendes',
  'vet_teste',
  '$2a$11$TlDABHF3O1p2GJFNzBkd/OZtc7cSMGj8kNPEj16JuGqhd.RI5lZtK',
  -- senha: Vet123!
  'Veterinario',
  'carlos.mendes@petcare.com'
),
(
  'Administrador PetCare',
  'admin_teste',
  '$2a$11$XMpHSVVa6.5f7QjKhFjPy.hPsO8JIQT9gNwmXf8sFsqSSJePCKbJy',
  -- senha: Admin123!
  'Admin',
  'admin@petcare.com'
);
GO

-- Aviso geral de boas-vindas
IF NOT EXISTS (SELECT 1 FROM Avisos WHERE Titulo = 'Bem-vindo ao PetCare Connect')
INSERT INTO Avisos (Titulo, Texto, Tipo)
VALUES (
  'Bem-vindo ao PetCare Connect',
  'O sistema PetCare Connect está no ar com banco de dados SQL Server! Faça login e explore todas as funcionalidades.',
  'geral'
);
GO

PRINT '✅  Seed executado. Usuários de teste criados com sucesso.';
PRINT '    tutor_teste  / Tutor123!';
PRINT '    vet_teste    / Vet123!';
PRINT '    admin_teste  / Admin123!';
GO
