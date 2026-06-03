🐾 PetCare Connect

Sistema de gestão veterinária que conecta **tutores**, **veterinários** e **administradores** em uma única plataforma, permitindo o gerenciamento de pets, consultas, vacinas, histórico clínico, avisos e relatórios.

---

📋 Sobre o Projeto

O **PetCare Connect** é uma aplicação web full-stack desenvolvida em **ASP.NET Core 8** (back-end) com front-end em **HTML/CSS/JavaScript** servido diretamente pela API. O sistema implementa autenticação via **JWT** e controle de acesso por perfil de usuário.

Perfis de usuário
- **Tutor** — gerencia seus pets, consultas e visualiza vacinas/histórico
- **Veterinário** — atende consultas, registra vacinas, histórico e relatórios
- **Admin** — administra usuários e acessa estatísticas do sistema

---

🛠️ Tecnologias

Back-end
- ASP.NET Core 8 (Web API)
- Dapper (acesso a dados)
- Microsoft.Data.SqlClient — **SQL Server 2019+**
- BCrypt.Net — hash de senhas
- JWT Bearer Authentication
- Swagger / Swashbuckle (documentação da API)

**Front-end**
- HTML, CSS e JavaScript (vanilla)
- Páginas: login, cadastro, recuperação de senha, painéis de tutor, veterinário e administração

---
📂 Estrutura do Projeto

```
PetCare-Connect-main/
└── Backend/
    ├── Controllers/        # Endpoints da API (Auth, Pets, Usuarios, etc.)
    ├── Models/             # Entidades (Usuario, Pet, Consulta, Vacina...)
    ├── DTOs/               # Objetos de transferência de dados
    ├── Services/           # AuthService (lógica de autenticação)
    ├── Data/               # PetCareDb, scripts SQL (criação + seed)
    ├── wwwroot/            # Front-end (HTML, JS, imagens)
    ├── Program.cs          # Configuração da aplicação
    └── appsettings.json    # Conexão com o banco e config JWT
```

---

🗄️ Entidades Principais

| Entidade   | Descrição                                            |
|------------|------------------------------------------------------|
| Usuario    | Tutores, veterinários e administradores              |
| Pet        | Animais cadastrados, vinculados a um tutor           |
| Consulta   | Agendamentos entre pets e veterinários               |
| Vacina     | Registro e controle de vacinas por pet               |
| Historico  | Eventos do histórico clínico do pet                  |
| Aviso      | Notificações gerais ou direcionadas                  |
| Relatorio  | Relatórios clínicos por pet                          |

---

🚀 Como Executar

Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2019 ou superior

1. Configurar o banco de dados
Execute os scripts na pasta `Backend/Data/`:
```sql
-- 1. Criar o banco e as tabelas
CreateDatabase.sql

-- 2. (Opcional) Popular com dados de teste
Seed.sql
```

2. Ajustar a connection string
Edite o arquivo `Backend/appsettings.json` com o nome do seu servidor SQL:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=SEU_SERVIDOR;Database=PetCareConnect;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

3. Rodar a aplicação
```bash
cd Backend
dotnet restore
dotnet run
```

A API ficará disponível em `http://localhost:5000`.

---

📖 Documentação da API (Swagger)

Com a aplicação rodando, acesse:
```
http://localhost:5000/swagger
```

Principais Endpoints

**Autenticação** (`/api/auth`)
- `POST /login` — autenticação e geração do token JWT
- `POST /cadastro` — cadastro de novo usuário
- `POST /recuperar-senha` / `POST /nova-senha` — fluxo de recuperação

**Pets** (`/api/pets`) — listar, buscar, criar e remover
**Consultas** (`/api/consultas`) — listar e agendar
**Vacinas** (`/api/vacinas`) — consultar por pet, criar e remover
**Histórico** (`/api/historico`) — consultar por pet, criar e remover
**Avisos** (`/api/avisos`) — listar, criar e marcar como lido
**Relatórios** (`/api/relatorios`) — listar e gerar
**Usuários** (`/api/usuarios`) — gestão de usuários e veterinários
**Admin** (`/api/admin/estatisticas`) — estatísticas do sistema

---

🔐 Usuários de Teste

Após rodar o `Seed.sql`:

| Perfil       | Login          | Senha       |
|--------------|----------------|-------------|
| Tutor        | `tutor_teste`  | `Tutor123!` |
| Veterinário  | `vet_teste`    | `Vet123!`   |
| Admin        | `admin_teste`  | `Admin123!` |

---

🔒 Segurança

- Senhas armazenadas com hash **BCrypt** (workFactor 11)
- Autenticação **JWT** com expiração configurável (padrão: 8h)
- Autorização baseada em perfis (roles)

> ⚠️ **Atenção:** a chave JWT e a connection string presentes no `appsettings.json` são apenas para desenvolvimento. Em produção, utilize variáveis de ambiente ou um cofre de segredos (User Secrets / Azure Key Vault).

---

## 📝 Licença

Projeto acadêmico desenvolvido para fins de estudo.
