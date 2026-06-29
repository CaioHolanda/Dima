# Contexto do Projeto Dima

## Descrição
Aplicação SaaS financeira didática desenvolvida seguindo o curso de programação do **Balta.io**.
Repositório público: https://github.com/CaioHolanda/Dima  
Branch de desenvolvimento ativa: `dev` (9 commits à frente da `main`)

---

## Stack Tecnológica
- **.NET 10** (versão mais recente)
- **ASP.NET Core** com Minimal APIs
- **Blazor WebAssembly** (frontend roda no browser)
- **Entity Framework Core** + **SQL Server** (Docker, localhost:1433, database: `dima-dev`)
- **ASP.NET Identity** com autenticação por **Cookie** (chave primária `long`)
- **MudBlazor 9** (componentes UI)
- **Swagger/OpenAPI** (apenas em ambiente de desenvolvimento)

---

## Estrutura da Solução (3 projetos)

### Dima.Core
Camada de domínio puro — sem dependências de infraestrutura.
Referenciada tanto pelo `Dima.Api` quanto pelo `Dima.Web`.
- **Models:** `Category`, `Transaction`, `User`, enum `ETransactionType`
- **Handler Interfaces:** `ICategoryHandler`, `ITransactionHandler`, `IAccountHandler`
- **Requests:** `Request` (base), `PagedRequest`, e requests específicos por operação
- **Responses:** `Response<T>`, `PagedResponse<T>` (com TotalCount e TotalPages)
- **Utilitários:** `Configuration` (DefaultPageSize, StatusCode), `DateTimeExtension` (GetFirstDay/GetLastDay)

### Dima.Api
Camada de backend — expõe a API HTTP consumida pelo Dima.Web.
- **Program.cs** limpo com Extension Methods: `AddConfiguration`, `AddSecurity`, `AddDataContexts`, `AddCrossOrigin`, `AddDocumentation`, `AddServices`
- **Endpoints** (implementam `IEndpoint` com `static abstract Map()`):
  - Categories: Create, Delete, GetAll, GetById, Update
  - Transactions: Create, Delete, GetAll, GetById, GetByPeriod
  - Identity: Login, Logout, GetRoles
- **Handlers concretos:** `CategoryHandler`, `TransactionHandler` (implementam interfaces do Core via EF Core)
- **AppDbContext:** herda de `IdentityDbContext<User, long>`
- **Mappings:** Fluent API em arquivos separados (`CategoryMapping`, `TransactionMapping`, tabelas Identity)
- **Migrations:** v1 (Fevereiro 2026), v2 (Março 2026)

### Dima.Web
Camada de frontend — Blazor WebAssembly, roda inteiramente no browser.
- **Program.cs:** configura MudBlazor, HttpClient nomeado com `CookieHandler`, `AuthenticationStateProvider`
- **Security:**
  - `CookieHandler` (DelegatingHandler): adiciona `BrowserRequestCredentials.Include` em todas as requisições
  - `CookieAuthenticationStateProvider`: verifica autenticação via `v1/identity/manage/info` e roles via `v1/identity/roles`
- **Handlers (Web):** `AccountHandler`, `CategoryHandler`, `TransactionHandler` — consomem a API via HttpClient
- **Layouts:** `MainLayout` (páginas autenticadas), `HeadlessLayout` (login/registo sem menu)
- **Pages:** Login, Register, Logout, Categories/List, Categories/Create
- **Tema MudBlazor:** cor primária `#1EFA2D` (verde), fonte Raleway, suporte a PaletteLight e PaletteDark

---

## Padrão Arquitetural (Request/Response + Handlers)
Simplificação do CQRS ensinada pelo Balta.io:
1. `Dima.Core` define o **Model**, o **Request** tipado e a **interface IHandler**
2. `Dima.Api` implementa o **Handler concreto** (usa EF Core) e o **Endpoint** (verbo HTTP + rota + metadados)
3. `Dima.Web` implementa o **Handler Web** (chama a API via HttpClient) e a **Page Razor** (.razor + .razor.cs)

### Sequência de criação de um novo endpoint (confirmada na análise):
```
[Dima.Core]
1. Criar o Model
2. Criar os Requests e Responses tipados
3. Criar a interface IHandler com os contratos

[Dima.Api]
4. Criar a classe Handler que implementa a interface (usa EF Core + AppDbContext)
5. Registrar interface + implementação no BuilderExtension (DI)
6. Criar a classe Endpoint (verbo HTTP + rota + metadados + chamada ao handler)
7. Registrar o endpoint no Endpoint.cs central (MapEndpoints)
```

---

## Bugs Identificados

### 1. String não interpolada — `TransactionHandler.cs`
```csharp
// ❌ Errado — {ex.Message} nunca será substituído
catch { return new Response<string>(null, 500, "[E010] Transaction update not possible. {ex.Message}"); }

// ✅ Correto
catch (Exception ex) { return new Response<string>(null, 500, $"[E010] Transaction update not possible. {ex.Message}"); }
```

### 2. `[Inject]` ausente — `Pages/Categories/List.razor.cs`
```csharp
// ❌ Errado — sem [Inject], Handler será sempre null → NullReferenceException em runtime
public ICategoryHandler Handler { get; set; } = null;

// ✅ Correto
[Inject]
public ICategoryHandler Handler { get; set; } = null!;
```

---

## Git / GitHub
- Branch `main`: estado estável inicial (sem Dima.Web)
- Branch `dev`: desenvolvimento ativo — contém todo o Dima.Web e 9 commits à frente da main
- Fluxo correto: desenvolver na `dev` → Pull Request para `main` quando estável
- O utilizador é iniciante em Git/GitHub

---

## Pontos em Aberto / Próximos Passos do Curso
- Implementação das páginas de Transactions (pasta existe mas está vazia)
- Futuramente: integração com Stripe para assinaturas e planos (Order, Plan — ainda não implementados)
- Merge da branch `dev` → `main` quando o frontend estiver estável

---

## Notas sobre o Utilizador
- Iniciante em Git/GitHub — conceitos de branch e merge ainda em aprendizagem
- Primeira interação prática com o Claude
- Preferência por uso do Claude via browser (plano gratuito por ora)
- Usa Visual Studio + GitHub Desktop no Windows
- SQL Server rodando em Docker localmente
