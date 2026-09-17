# DT-18 — Tempo e fuso horário

Implementação: instantes em UTC; relógio injetável nos fluxos da API.

## Convenção e histórico

O responsável pelo projeto confirmou em 17/09/2026 que o fuso de negócio e
o fuso dos registros históricos são UTC. Assim, os valores existentes de
`DATETIME2` representam UTC: seus números não devem receber um deslocamento.
Os conversores EF recuperam `DateTimeKind.Utc` ao ler esses instantes; o JSON
inclui `Z` e o frontend converte para o fuso local somente para apresentação.

Não há alteração de schema nem necessidade de uma migration de dados sob essa
premissa confirmada. Uma migration que somasse/subtraísse horas corromperia
esses instantes. A verificação `dotnet ef migrations has-pending-model-changes`
confirma que não há mudanças de schema pendentes.

Instantes: criação/atualização/pagamento/acesso/reembolso de pedidos, criação
de transações e reserva/resgate/liberação de vouchers. Prazos de expiração e
auditoria continuam usando `DateTimeOffset` em UTC.

## Datas civis

`Voucher.StartsAt`/`EndsAt` e `Transaction.PaidOrReceivedAt` são datas civis.
Não recebem conversão de fuso ou marcação UTC. A vigência diária de vouchers
permanece inclusiva; prévia e criação de pedido usam o mesmo serviço de tempo.
`BusinessTime:TimeZoneId` é explicitamente `UTC`, validado na inicialização.
Os períodos e relatórios financeiros usam o calendário desse fuso; os seletores
do frontend adotam o calendário UTC. Alterar o fuso de negócio no futuro exige
alinhar esses seletores, além da configuração da API.

O acesso continua usando a duração em meses do snapshot do pedido; o prazo de
reembolso continua inclusivo até o instante de pagamento acrescido de 14 dias.
As comparações de reembolso da API e da UI compartilham `RefundTimeRules`.

## Validação

Testes antigos de pagamento, pedido gratuito, expiração, acesso, reembolso,
auditoria e paginação foram adequados à convenção UTC. Os testes temporais de
criação, acesso, reembolso e auditoria usam relógio fixo.

`Dima.Tests/Time/UtcTimeTests.cs` cobre persistência e JSON, histórico sem
deslocamento, datas civis preservadas, fusos diferentes, virada de dia dos
vouchers, cancelamento, pagamento e limite de reembolso com precisão de tick.
O roundtrip relacional usa SQLite; não substitui validação de implantação no
SQL Server. O modelo e os conversores de todos os campos de instantes também
são verificados.

Comandos:

```powershell
dotnet test Dima.Tests/Dima.Tests.csproj
dotnet build Dima.Web/Dima.Web.csproj
dotnet ef migrations has-pending-model-changes --project Dima.Api
```
