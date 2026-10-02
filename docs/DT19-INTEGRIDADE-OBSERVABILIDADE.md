# DT-19 — Integridade e observabilidade

Implementação na `dev`: índice único de slug, registro estruturado de exceções
e correlação frontend → API → checkout Stripe → webhook.

Status: implementada e validada no código; aplicação do índice no banco de
desenvolvimento pendente.

## Integridade e consultas

`UX_Product_Slug` é único e inclui produtos inativos. O índice dá suporte às
consultas por slug e garante a unicidade também quando duas operações passam
simultaneamente pela verificação `AnyAsync`. Criação e atualização convertem
os erros SQL Server 2601/2627 desse índice em HTTP 409, preservando os códigos
E114/E117. Outros erros continuam sendo falhas internas e são registrados.

A migration `20260917213413_AddUniqueProductSlug` verifica duplicados antes de
criar o índice. Se houver duplicados segundo a collation do banco, interrompe
a operação com erro 51019; não renomeia nem exclui produtos. A correção de
dados exige analisar os produtos afetados. O rollback remove apenas o índice.

Não foram duplicados os índices de pedidos, pagamentos, reembolsos, vouchers,
resgates, expiração e auditoria já existentes. O ganho de desempenho do novo
índice depende do volume e dos planos das consultas; não houve benchmark.

## Logs e correlação

O cliente HTTP do frontend gera `X-Correlation-ID` para cada chamada. A API
aceita somente GUID não vazio em formato N/D, normaliza para N ou gera um
novo ID para cabeçalhos ausentes/inválidos/múltiplos. Devolve o ID na resposta
e o expõe pelo CORS. Esse ID não é uma credencial nem um controle de acesso.

O middleware abre um escopo com `CorrelationId`, `TraceId`, método e caminho,
registra status e duração e cobre exceções não tratadas com um erro HTTP 500
genérico contendo os IDs. Não tenta reescrever uma resposta já iniciada e não
converte cancelamento do cliente em erro interno.

Todos os handlers da API registram as exceções antes suprimidas, com a
operação e a exceção original/stack trace. Os registros manuais de pedidos e
Stripe passam a usar `ILogger`. O console JSON inclui os escopos. Detalhes
internos de exceções de transações e Stripe deixam de ir para o cliente.
Não são acrescentados logs de corpo, query string, cookies, assinatura do
webhook ou configuração de segredos.

Checkout e PaymentIntent recebem `correlation_id` nos metadados Stripe. Esse
`CheckoutCorrelationId` deriva da chave de idempotência da tentativa e se
mantém estável nos retries: usar o ID de cada requisição HTTP nesses metadados
alteraria os parâmetros e faria o Stripe rejeitar a repetição da mesma chave.
O log de criação liga `CorrelationId`, `CheckoutCorrelationId`, pedido e sessão.
Para uma tentativa anterior à DT-19 cujo resultado esteja no cache de
idempotência do Stripe sem o metadado, a API tenta os parâmetros anteriores
com a mesma chave se receber `idempotency_error`. Nunca troca a chave para
contornar esse erro; a repetição continua vinculada à mesma tentativa.
O webhook continua validando a assinatura e abre escopos de evento e
pagamento contendo `EventId`, `EventType`, `PaymentIntentId`, `OrderNumber`
e `CheckoutCorrelationId`. Reembolsos se vinculam por `PaymentIntentId` e
`RefundId`. Webhooks e sessões anteriores sem o metadado continuam válidos;
seus IDs de negócio ainda permitem rastreamento. A requisição do webhook
recebe seu próprio `CorrelationId`, distinto da correlação do checkout.

Auditoria administrativa existente participa do escopo da requisição nos
logs; seu schema não foi alterado. Deduplicação de `EventId` permanece no
escopo da DT-02.

## Verificação e banco de desenvolvimento

Os testes da DT-19 exercitam unicidade relacional inclusive para produtos
inativos, atualização mantendo o próprio slug, conflitos descobertos na
gravação, outras falhas SQL, logs da exceção/operação, correlação HTTP, CORS,
erro genérico e webhook assinado. SQLite testa a restrição relacional; os
erros específicos SQL Server são simulados na suíte. Também é conferido o
modelo SQL Server, sem conexão com um banco externo. O transporte Stripe é
simulado para verificar os metadados efetivamente enviados pelo SDK,
parâmetros idênticos entre retries e compatibilidade com tentativas anteriores.

Verificação adicional em SQL Server LocalDB 17.0.4025.3, em banco e instância
temporários isolados: a migration bloqueou duplicados com erro 51019, criou
o índice e seu histórico, rejeitou duplicados de produtos inativos e converteu
conflitos reais de criação/atualização após o pre-check em HTTP 409. Banco e
instância temporários foram removidos ao final. Passaram os 120 testes da
suíte ampliada e, após ajuste no transporte simulado, os dois cenários novos
de retry do checkout em execução focada: 122 cenários distintos aprovados.

```powershell
dotnet test Dima.Tests/Dima.Tests.csproj
dotnet build Dima.Web/Dima.Web.csproj
dotnet ef migrations has-pending-model-changes --project Dima.Api
```

A criação da migration não aplica mudanças no banco. Com a conexão de
desenvolvimento configurada, aplicar com:

```powershell
dotnet ef database update --project Dima.Api -- --environment Development
```

`DT19-MIGRATION.sql` contém somente a mudança incremental posterior a
`AddAdminAuditLog`, para revisão. Antes de aplicar, verificar duplicados:

```sql
SELECT [Slug], COUNT(*) AS [Quantidade]
FROM [Product]
GROUP BY [Slug]
HAVING COUNT(*) > 1;
```

Esta implementação não consultou nem alterou o banco externo. A efetivação
do índice depende da aplicação bem-sucedida da migration nesse banco.
