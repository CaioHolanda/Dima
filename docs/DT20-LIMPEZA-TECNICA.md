# DT-20 — Limpeza técnica

Status: implementada no código local e validada por testes e compilação.
Publicação e validação do ambiente publicado não foram executadas nesta tarefa.

## Resíduos removidos

- `WaitingPayment` substitui `WaintingPayment` em toda a aplicação e nos testes.
  Os valores persistidos/JSON permanecem 1 a 6, com atribuições explícitas.
  Nomes históricos de índices e migrations são preservados; não há alteração de schema.
- O cliente público de produtos implementa somente o contrato público. Foram
  removidos os seis métodos administrativos com `NotImplementedException`;
  as operações administrativas continuam no `AdminProductHandler`.
- Stripe.js e `StripePublicKey` foram removidos do frontend. O checkout continua
  recebendo a URL da sessão criada na API e redirecionando por `window.checkout`.
- O cabeçalho AJAX passa a usar `X-Requested-With`. Credenciais de navegador,
  tratamento especial do login e comportamento de sessão expirada são preservados.

## Configuração

`ApiOptions` recebe as chaves raiz já usadas pelas implantações (`BackendUrl`,
`FrontendUrl`, `StripeApiKey`, `StripeWebhookSecret`). Não é necessário renomear
variáveis/configurações do ambiente. Checkout, reembolso, expiração de sessão,
webhook e recuperação de senha usam dependências/opções injetadas.

Cada provedor da API possui seu próprio `IStripeClient`; os serviços de sessão
e reembolso recebem esse cliente explicitamente. A aplicação deixa de atribuir
`StripeConfiguration.ApiKey` ou depender do cliente global do SDK. A ausência
da chave mantém os erros de configuração existentes antes de chamar Stripe.

Conexão SQL e origens CORS são lidas da configuração do builder. O Core retém
somente constantes de resposta/paginação. O frontend resolve a URL da API
localmente no startup, preservando a origem de produção e `/api/`. O tema visual
continua compartilhado, com propriedade sem setter; não é configuração de ambiente.

## Contratos HTTP

Clientes de produtos, pedidos, categorias, transações, relatórios, vouchers e
administração compartilham `HttpResponseReader`. Respostas e streams são
liberados após a leitura. O status HTTP define `Code` e, em erro, os dados são
removidos. Mensagens de domínio em envelopes válidos são preservadas.

Corpo vazio, JSON inválido, ProblemDetails ou JSON sem o campo numérico `code`
produzem uma mensagem local sem expor o corpo bruto. O status original de erro
é preservado; uma resposta HTTP de sucesso com envelope inválido ou código de falha no corpo vira 502.
Falhas de rede/cancelamento continuam propagando normalmente.

As operações Identity conservam seu contrato de status HTTP e mensagens
específicas, incluindo respostas bem-sucedidas sem corpo. Seus objetos HTTP
agora também são liberados. Clientes de listagem de produtos, pedidos,
categorias e transações enviam a paginação recebida.

## Validação

A suíte completa contém 137 cenários, incluindo regressões para status HTTP,
corpos vazios/inválidos, ProblemDetails, mensagens de checkout, liberação de
respostas, paginação e metadados, cabeçalho AJAX, login/401, valores numéricos
do enum, isolamento de configuração entre hosts e ausência de chave Stripe.
Os testes de checkout/retry e webhook assinado usam transporte/serviços simulados;
não realizam pagamentos nem acessam Stripe ou banco externo.

```powershell
dotnet test Dima.Tests/Dima.Tests.csproj --no-restore
dotnet build Dima.Web/Dima.Web.csproj --no-restore
```

O teste `SqlServer_model_matches_migrations_and_gateway_is_optional` verifica
que o modelo continua compatível com as migrations, sem conexão ao banco.
Há avisos de compilação preexistentes em componentes MudBlazor, layout e
`StaticWebAppsCookieManager`, fora das mudanças desta DT.
