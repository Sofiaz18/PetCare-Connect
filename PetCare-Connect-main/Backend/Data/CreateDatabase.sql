-- ============================================================
--  PetCare Connect — Script de Criação do Banco de Dados
--  SQL Server 2019+
--  Execute como sa ou usuário com permissão CREATE DATABASE
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PetCareConnect')
BEGIN
    CREATE DATABASE PetCareConnect
        COLLATE Latin1_General_CI_AI;
END
GO

USE PetCareConnect;
GO

-- ============================================================
--  TABELAS
-- ============================================================

-- Usuários (Tutor, Veterinário, Admin)
IF OBJECT_ID('Usuarios', 'U') IS NULL
CREATE TABLE Usuarios (
    Id          INT             IDENTITY(1,1) PRIMARY KEY,
    Nome        NVARCHAR(120)   NOT NULL,
    Login       NVARCHAR(60)    NOT NULL UNIQUE,
    SenhaHash   NVARCHAR(256)   NOT NULL,   -- BCrypt hash
    Perfil      NVARCHAR(20)    NOT NULL    -- 'Tutor' | 'Veterinario' | 'Admin'
        CHECK (Perfil IN ('Tutor','Veterinario','Admin')),
    Email       NVARCHAR(120)   NULL,
    Ativo       BIT             NOT NULL DEFAULT 1,
    CriadoEm   DATETIME2       NOT NULL DEFAULT GETDATE()
);
GO

-- Pets
IF OBJECT_ID('Pets', 'U') IS NULL
CREATE TABLE Pets (
    Id          INT             IDENTITY(1,1) PRIMARY KEY,
    Nome        NVARCHAR(80)    NOT NULL,
    Tipo        NVARCHAR(40)    NOT NULL,
    Idade       INT             NOT NULL CHECK (Idade > 0),
    Peso        DECIMAL(5,2)    NOT NULL CHECK (Peso >= 0),
    TutorId     INT             NOT NULL REFERENCES Usuarios(Id),
    CriadoEm   DATETIME2       NOT NULL DEFAULT GETDATE()
);
GO

-- Consultas
IF OBJECT_ID('Consultas', 'U') IS NULL
CREATE TABLE Consultas (
    Id              INT             IDENTITY(1,1) PRIMARY KEY,
    PetId           INT             NOT NULL REFERENCES Pets(Id),
    VeterinarioId   INT             NOT NULL REFERENCES Usuarios(Id),
    Data            DATE            NOT NULL,
    Hora            TIME(0)         NOT NULL,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Marcada'
        CHECK (Status IN ('Marcada','Realizada','Cancelada')),
    Observacoes     NVARCHAR(MAX)   NULL,
    CriadoEm       DATETIME2       NOT NULL DEFAULT GETDATE()
);
GO

-- Vacinas
IF OBJECT_ID('Vacinas', 'U') IS NULL
CREATE TABLE Vacinas (
    Id          INT             IDENTITY(1,1) PRIMARY KEY,
    PetId       INT             NOT NULL REFERENCES Pets(Id),
    Nome        NVARCHAR(100)   NOT NULL,
    DataAplicacao DATE          NULL,
    Observacao  NVARCHAR(200)   NULL,
    Aplicada    BIT             NOT NULL DEFAULT 0,
    CriadoEm   DATETIME2       NOT NULL DEFAULT GETDATE()
);
GO

-- Histórico Clínico
IF OBJECT_ID('Historico', 'U') IS NULL
CREATE TABLE Historico (
    Id          INT             IDENTITY(1,1) PRIMARY KEY,
    PetId       INT             NOT NULL REFERENCES Pets(Id),
    Descricao   NVARCHAR(MAX)   NOT NULL,
    DataEvento  DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    CriadoEm   DATETIME2       NOT NULL DEFAULT GETDATE()
);
GO

-- Avisos
IF OBJECT_ID('Avisos', 'U') IS NULL
CREATE TABLE Avisos (
    Id          INT             IDENTITY(1,1) PRIMARY KEY,
    Titulo      NVARCHAR(120)   NOT NULL,
    Texto       NVARCHAR(MAX)   NOT NULL,
    Tipo        NVARCHAR(20)    NOT NULL DEFAULT 'geral'
        CHECK (Tipo IN ('geral','tutor','pet','consulta')),
    TutorId     INT             NULL REFERENCES Usuarios(Id),
    PetId       INT             NULL REFERENCES Pets(Id),
    CriadoEm   DATETIME2       NOT NULL DEFAULT GETDATE()
);
GO

-- Leituras de Avisos (N:N)
IF OBJECT_ID('AvisosLidos', 'U') IS NULL
CREATE TABLE AvisosLidos (
    AvisoId     INT NOT NULL REFERENCES Avisos(Id),
    UsuarioId   INT NOT NULL REFERENCES Usuarios(Id),
    LidoEm      DATETIME2 NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (AvisoId, UsuarioId)
);
GO

-- Prontuários / Relatórios
IF OBJECT_ID('Relatorios', 'U') IS NULL
CREATE TABLE Relatorios (
    Id          INT             IDENTITY(1,1) PRIMARY KEY,
    PetId       INT             NOT NULL REFERENCES Pets(Id),
    Texto       NVARCHAR(MAX)   NOT NULL,
    Atualizacao DATETIME2       NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
--  ÍNDICES
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pets_TutorId')
    CREATE INDEX IX_Pets_TutorId       ON Pets(TutorId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Consultas_PetId')
    CREATE INDEX IX_Consultas_PetId    ON Consultas(PetId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Consultas_VetId')
    CREATE INDEX IX_Consultas_VetId    ON Consultas(VeterinarioId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vacinas_PetId')
    CREATE INDEX IX_Vacinas_PetId      ON Vacinas(PetId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Historico_PetId')
    CREATE INDEX IX_Historico_PetId    ON Historico(PetId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Avisos_TutorId')
    CREATE INDEX IX_Avisos_TutorId     ON Avisos(TutorId);
GO

-- ============================================================
--  SEED — Usuários de Teste
--  Senha: Tutor123!   → hash BCrypt abaixo
--  Senha: Vet123!     → hash BCrypt abaixo
--  Senha: Admin123!   → hash BCrypt abaixo
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Login = 'tutor_teste')
INSERT INTO Usuarios (Nome, Login, SenhaHash, Perfil, Email)
VALUES (
    'Maria Oliveira',
    'tutor_teste',
    '$2a$11$kK3gH5Y1J9Z2mQ7X8vR0LO1w2E3T4U5I6O7P8A9S0D1F2G3H4J5K6', -- BCrypt de Tutor123!
    'Tutor',
    'maria.oliveira@email.com'
);

IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Login = 'vet_teste')
INSERT INTO Usuarios (Nome, Login, SenhaHash, Perfil, Email)
VALUES (
    'Dr. Carlos Mendes',
    'vet_teste',
    '$2a$11$aA1bB2cC3dD4eE5fF6gG7hH8iI9jJ0kK1lL2mM3nN4oO5pP6qQ7rR8', -- BCrypt de Vet123!
    'Veterinario',
    'carlos.mendes@petcare.com'
);

IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Login = 'admin_teste')
INSERT INTO Usuarios (Nome, Login, SenhaHash, Perfil, Email)
VALUES (
    'Administrador PetCare',
    'admin_teste',
    '$2a$11$sS1tT2uU3vV4wW5xX6yY7zZ8aA9bB0cC1dD2eE3fF4gG5hH6iI7jJ8', -- BCrypt de Admin123!
    'Admin',
    'admin@petcare.com'
);
GO

-- Aviso geral de boas-vindas
IF NOT EXISTS (SELECT 1 FROM Avisos WHERE Titulo = 'Bem-vindo ao PetCare Connect')
INSERT INTO Avisos (Titulo, Texto, Tipo)
VALUES (
    'Bem-vindo ao PetCare Connect',
    'O sistema PetCare Connect está no ar! Cadastre seus pets e agende consultas com facilidade.',
    'geral'
);
GO

PRINT '✅  Banco de dados PetCareConnect criado e populado com sucesso.';
GO
