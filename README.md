# DocMigrate API

Backend da plataforma de documentacao modular DocMigrate.

## Stack

- .NET 10
- Entity Framework Core + PostgreSQL
- FluentValidation
- MinIO (armazenamento de arquivos)

## Pre-requisitos

- .NET 10 SDK
- PostgreSQL 16 (via Docker)
- MinIO (via Docker)

## Setup

1. Subir servicos de infraestrutura:
   ```bash
   # Na pasta raiz (documentacao_migrate/)
   docker compose up -d postgres minio
   ```

2. Configurar variaveis de ambiente:
   ```bash
   cp .env.example .env
   # Editar .env conforme necessario
   ```

3. Aplicar migrations:
   ```bash
   cd src/DocMigrate.API
   dotnet ef database update
   ```

4. Rodar a API:
   ```bash
   dotnet run --project src/DocMigrate.API
   ```
   API disponivel em: `http://localhost:5029`

## Comandos

| Comando | Descricao |
|---------|-----------|
| `dotnet build` | Compilar o projeto |
| `dotnet test` | Rodar testes |
| `dotnet run --project src/DocMigrate.API` | Iniciar API |
| `dotnet ef migrations add <Nome>` | Criar migration |
| `dotnet ef database update` | Aplicar migrations |

## Arquitetura

Clean Architecture com 4 camadas:

```
Domain/          → Entidades puras (Space, Page, User, UserPreference)
Application/     → DTOs, Interfaces, Validators
Infrastructure/  → EF Core, Services, Configurations
API/             → Controllers, Middleware, Program.cs
```

## Endpoints

| Metodo | Rota | Descricao |
|--------|------|-----------|
| GET | /api/spaces | Listar espacos |
| GET | /api/spaces/:id | Detalhe do espaco |
| POST | /api/spaces | Criar espaco |
| PUT | /api/spaces/:id | Atualizar espaco |
| DELETE | /api/spaces/:id | Excluir espaco |
| GET | /api/pages?spaceId=X | Paginas do espaco |
| GET | /api/pages/:id | Detalhe da pagina |
| POST | /api/pages | Criar pagina |
| PUT | /api/pages/:id | Atualizar pagina |
| DELETE | /api/pages/:id | Excluir pagina |
| POST | /api/files/icons | Upload de icone |
| GET | /api/user-preferences | Preferencias do usuario |
| PUT | /api/user-preferences | Atualizar preferencias |
| DELETE | /api/user-preferences | Resetar preferencias |
| GET | /api/health | Health check |

## Testes

```bash
dotnet test
```

27 testes cobrindo CRUD de Spaces e Pages, soft delete, e validacoes.
