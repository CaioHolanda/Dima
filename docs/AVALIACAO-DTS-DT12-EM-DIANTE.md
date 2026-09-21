# Avaliação das DTs — DT12 em diante

Roteiro elaborado em 18/09/2026 a partir do histórico Git, código e documentação de `Dima-review`. Inclui as alterações locais ainda sem commit. Execute sobre a mesma versão que pretende publicar; testar somente o último commit deixa de fora partes das DT24, DT26 e DT28.

## 1. Ordem de execução e preparação

1. Registrar versão, alterações locais, ambiente e banco utilizado.
2. Conferir o histórico das migrations e atualizar o banco de homologação.
3. Executar a suíte automatizada.
4. Executar os cenários manuais abaixo em homologação, com Stripe de teste.
5. Medir carregamento, consultas e processamento periódico.
6. Repetir os cenários de implantação no ambiente publicado, conforme a DT30.

Utilizar uma cópia representativa do banco, sem dados sensíveis, e backup antes de atualizar seu schema. Preparar: dois Admins ativos com e-mail confirmado, um usuário comum, um usuário bloqueado, produtos ativos/inativos, vouchers válidos/vencidos e pedidos em cada status. Para paginação, ter mais de 100 registros e pelo menos um resultado de pesquisa após o centésimo. Usar duas abas e, para comparar sessões independentes, outro perfil do navegador.

Todos os comandos PowerShell abaixo partem da pasta do projeto:

```powershell
Set-Location 'C:\Users\julio\OneDrive\Documentos\ChatGPT\Aplicativo Dima - DTs\Dima-review'
git status --short
git rev-parse HEAD
dotnet --version
node --version
```

Requisitos: SDK .NET 10, ferramenta `dotnet-ef` compatível com EF Core 10, Node para o teste JavaScript, SQL Server e configuração do ambiente. A API usa `ConnectionStrings:DefaultConnection`; conferir servidor e nome do banco sem registrar credenciais. As migrations não são aplicadas automaticamente no startup da API.

## 2. Migrations: o que pode faltar

| DT | Migration | Alteração / impacto se faltar | Evidência disponível |
| --- | --- | --- | --- |
| 12 | `20260914202811_MakeOrderGatewayNullable` | Permite `Order.Gateway` nulo; gravações que dependem disso podem falhar sem a alteração. | Existe no código; aplicação ao banco-alvo não confirmada neste levantamento. |
| 16 | `20260916212147_AddAdminAuditLog` | Cria auditoria; sem tabela, a operação pode concluir, mas o filtro registra falha de auditoria nos logs. | Documento de retomada registra aplicação em um computador anterior; isso não comprova outros bancos. |
| 19 | `20260917213413_AddUniqueProductSlug` | Cria índice único `UX_Product_Slug`, incluindo produtos inativos; sem ele, concorrência pode gerar slugs duplicados. | Documento DT19 registra aplicação ao banco de desenvolvimento como pendente. |
| 28 | `20260918220500_AddUserSessions` | Cria `UserSessions`; necessária para login e validação da nova sessão. | Documento DT28 informa que não foi aplicada na implementação. Migration ainda local, sem commit. |

As DT13–15, DT17–18, DT20–21 e DT24–27/29 não introduzem novas migrations próprias no estado revisado. DT26 reutiliza colunas/migrations anteriores de expiração. Não foi encontrado registro suficiente para definir o escopo das DT22 e DT23: sua cobertura permanece a confirmar. DT27 tem medições pendentes; DT30 reúne validações de implantação.

### Conferência no banco-alvo — somente leitura

Executar no SSMS/Azure Data Studio conectado ao banco correto:

```sql
SELECT @@SERVERNAME AS Servidor, DB_NAME() AS Banco;
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;

-- Cada linha retornada abaixo corresponde a uma migration ausente no histórico.
SELECT required.MigrationId AS MigrationAusente
FROM (VALUES
 ('20260914202811_MakeOrderGatewayNullable'),
 ('20260916212147_AddAdminAuditLog'),
 ('20260917213413_AddUniqueProductSlug'),
 ('20260918220500_AddUserSessions')
) AS required(MigrationId)
LEFT JOIN dbo.__EFMigrationsHistory h ON h.MigrationId = required.MigrationId
WHERE h.MigrationId IS NULL;

SELECT Slug, COUNT(*) AS Quantidade
FROM dbo.Product GROUP BY Slug HAVING COUNT(*) > 1;

SELECT c.name, c.is_nullable FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.[Order]') AND c.name = 'Gateway';
SELECT OBJECT_ID('dbo.AdminAuditLog') AS Auditoria,
       OBJECT_ID('dbo.UserSessions') AS Sessoes;
SELECT name, is_unique, is_disabled FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'UX_Product_Slug';
```

Esperado após atualização: nenhuma migration ausente; nenhum slug duplicado; `Gateway.is_nullable = 1`; ambas as tabelas existem; índice único habilitado. A comparação de slugs respeita a collation do banco. Se a tabela de histórico não existir, interromper e conferir a origem/schema do banco; não presumir que ele está vazio. Comparar também todo o histórico com a lista EF, pois pode haver migrations anteriores à DT12 ausentes.

### Atualização em homologação

Com a conexão de Development apontando para o banco de homologação escolhido:

```powershell
dotnet ef migrations list --project Dima.Api --startup-project Dima.Api -- --environment Development
dotnet ef migrations has-pending-model-changes --project Dima.Api --startup-project Dima.Api -- --environment Development

# Gerar script completo idempotente para revisão, sem aplicar ao banco.
dotnet ef migrations script --idempotent --project Dima.Api --startup-project Dima.Api --output docs/AVALIACAO-MIGRATIONS.sql -- --environment Development

# Aplicar migrations ausentes ao banco configurado, após conferir o destino.
dotnet ef database update --project Dima.Api --startup-project Dima.Api -- --environment Development
```

`has-pending-model-changes` compara modelo com migrations; não informa se o banco está atualizado. A DT19 interrompe com erro 51019 se houver slugs duplicados: analisar e corrigir os cadastros afetados antes de repetir, sem renomear/excluir automaticamente. Os scripts `DT19-MIGRATION.sql` e `DT28-MIGRATION.sql` são incrementais entre versões específicas; não substituem a atualização completa de um banco mais antigo.

Aplicar `AddUserSessions` antes de publicar a API da DT28. Cookies antigos exigirão novo login. Em produção, revisar o script e aplicar pela rotina de implantação ao banco confirmado; não reutilizar Development sem conferir o destino. Não reverter migrations como teste: o rollback da DT12 preenche gateways nulos com 1, e o da DT28 exclui os registros de sessões.

## 3. Testes automatizados e como executar

```powershell
dotnet restore Dima.slnx
dotnet test Dima.Tests/Dima.Tests.csproj --no-restore --logger 'trx;LogFileName=avaliacao-dts.trx' --results-directory TestResults/avaliacao-dts
node --test Dima.Tests/Browser/session-monitor.test.mjs
dotnet build Dima.Api/Dima.Api.csproj --no-restore
dotnet build Dima.Web/Dima.Web.csproj --no-restore
```

Esperado: nenhuma falha ou teste ignorado; builds sem erros. Registrar warnings separadamente. Para repetir um grupo, usar:

```powershell
dotnet test Dima.Tests/Dima.Tests.csproj --no-restore --filter 'FullyQualifiedName~PaymentArchitectureTests|FullyQualifiedName~CreateOrderVoucherTests'
```

Substituir a expressão pelos grupos da tabela; juntar classes com `|FullyQualifiedName~`. A suíte utiliza SQLite/InMemory, hosts HTTP locais e Stripe simulado. Aprovação não comprova SQL Server externo, pagamento real de teste, Azure/proxy nem apresentação visual.

| DT | Classes para execução focada | O que verificar |
| --- | --- | --- |
| 12 | `PaymentArchitectureTests`, `CreateOrderVoucherTests` | Contrato de pagamento independente do gateway; pedido gratuito e criação/retry de sessão. |
| 13 | `AdminUserSecurityTests` | Conta própria, último Admin, bloqueio, reativação e security stamp. |
| 14 | `AdminPaginationTests` | Pesquisa além de 100 registros, filtros, totais, limites e páginas sem sobreposição. |
| 15 | `AdminOrderDetailsTests` | Snapshots financeiros, gateway ausente/gratuito, 404 e detalhes operacionais. |
| 16 | `AdminAuditTests` | Autor, alvo, snapshots, resultado e falha da auditoria. |
| 17 | `AdminHttpAuthorizationTests`, `PaymentConfirmationIdempotencyTests` | 401/403/Admin e confirmação repetida sem reaplicar pagamento/acesso/voucher. |
| 18 | `UtcTimeTests`, `RefundTests`, `OrderAccessDurationTests` | Persistência/JSON UTC, datas civis, duração de acesso e limites de reembolso. |
| 19 | `ProductSlugTests`, `RequestObservabilityTests`, `StripeCheckoutCorrelationTests` | Unicidade, conflitos, logs, correlação e metadados estáveis no retry. |
| 20 | `HttpContractTests`, `ApiConfigurationTests` | HTTP válido/inválido, status/mensagens, paginação e isolamento da configuração. |
| 21 | `SwaggerExposureTests` | Swagger só em Development com opção habilitada. |
| 24 | `HttpContractTests`, `AdminHttpAuthorizationTests` | Contratos/cookies locais; completar com navegador publicado. |
| 26 | `OrderExpirationWorkerTests` | Cancelamento elegível, pagamento concorrente, reservas e lotes de expiração. |
| 28 | `UserSessionTests`, `AdminUserSecurityTests`, `AdminHttpAuthorizationTests`, teste Node | 15 min/8 h, atividade, revogação, aba antiga, permissões e monitor JavaScript. |
| Regressão | `ApplyVoucherTests`, `VoucherDiscountCalculatorTests`, `VoucherEligibilityTests`, `ProductAccessDurationTests`, `RefundTests` | Fluxos anteriores que continuam afetados por checkout, tempo, cancelamento e sessão. |

DT25 e DT29 dependem principalmente de aceitação visual; DT27 precisa de medição; DT30 precisa do ambiente publicado.

## 4. Testes manuais: passos, resultado e impacto

Iniciar API e Web em dois terminais, após atualizar o banco:

```powershell
# Terminal 1
dotnet run --project Dima.Api --launch-profile http
# Terminal 2
dotnet run --project Dima.Web --launch-profile http
```

Defaults: API `http://localhost:5088`, Web `http://localhost:5156`. Conferir BackendUrl/FrontendUrl e CORS efetivos. Abrir DevTools em Network, preservar o log e observar Console. Anotar IDs de pedidos e correlação; não salvar cookies, tokens ou segredos nas evidências.

### T01 — DT12: checkout e gateway

1. Criar compra paga e abrir checkout Stripe de teste; concluir e aguardar webhook.
2. Repetir a tentativa de checkout do mesmo pedido pelo fluxo disponível e conferir sessões/pagamentos no provedor.
3. Criar pedido gratuito, inclusive desconto de 100%, e conferir acesso.
4. Consultar pedido antigo com gateway ausente e comparar com pedido gratuito.

Esperado: compra paga confirmada uma vez, sem cobrança duplicada; acesso gratuito sem checkout; detalhes distinguem gateway ausente de NotApplicable. Impacto: consistência entre pedido, pagamento e acesso após o desacoplamento.

### T02 — DT13: desativação e revogação

1. Tentar desativar a própria conta Admin e o último Admin ativo: bloquear.
2. Com dois Admins ativos, desativar o outro; repetir com usuário comum conectado em outro perfil.
3. No perfil desativado, chamar uma rota protegida: 401 na próxima requisição.
4. Reativar e tentar usar a sessão antiga: continuar inválida; novo login deve funcionar.

Esperado: proteção administrativa preservada, revogação por security stamp. Medir aumento de consultas Identity por requisição; uma requisição já em execução não é interrompida pela revogação.

### T03 — DT14: listas e seletor de produtos

1. Em produtos, vouchers, usuários e pedidos, localizar registro após o centésimo.
2. Combinar pesquisa e filtros; comparar total com os registros correspondentes no banco.
3. Navegar páginas, mudar tamanho e filtros; procurar duplicações/omissões.
4. Criar/editar voucher e pesquisar produto além dos primeiros 100.

Esperado: pesquisa aplicada sobre todo o conjunto, totais corretos, ordenação estável e paginação coerente. Impacto: tamanho das respostas e tempo de consultas/listas.

### T04 — DT15: histórico do pedido

1. Comprar com voucher; registrar preço, desconto, total, código e duração de acesso.
2. Alterar preço do produto e código/valor do voucher; reabrir detalhes Admin/cliente.
3. Conferir gateway, referências, sessão, validade, acesso e reembolso; abrir um ID inexistente.
4. Abrir registro antigo sem snapshot; voltar à lista com página/pesquisa/status.

Esperado: valores históricos permanecem; campos antigos ausentes são indicados sem inventar dados; ID inexistente retorna 404; contexto da lista é preservado. O nome do produto continua vindo do cadastro atual.

### T05 — DT16: auditoria no SQL Server

1. Como Admin, criar/editar produto e voucher, desativar/reativar usuário e executar cancelamento/reembolso de teste.
2. Consultar `SELECT TOP (100) * FROM dbo.AdminAuditLog ORDER BY OccurredAtUtc DESC, Id DESC;`.
3. Conferir autor, UTC, operação/alvo, HTTP, sucesso e snapshots; confirmar ausência de senhas/stamps/tokens.
4. Fazer uma leitura, uma ação de cliente e uma tentativa anônima rejeitada: não devem produzir auditoria administrativa.

Esperado: operações cobertas possuem registro; a solicitação de reembolso é auditada, a confirmação assíncrona permanece no fluxo de webhook. Impacto: gravação adicional, armazenamento e latência. Falha de auditoria não desfaz a operação e precisa aparecer nos logs; não há atomicidade garantida.

### T06 — DT17: autorização e repetição de pagamento

1. Chamar listagens/detalhes/escritas Admin anonimamente: 401; como usuário comum: 403; como Admin: acesso conforme regra.
2. Pelo Stripe de teste, reenviar o mesmo evento assinado; reenviar confirmação equivalente do mesmo pagamento.
3. Conferir pedido, acesso e voucher antes/depois; enviar assinatura inválida.

Esperado: sem redirecionamento HTML, extensão duplicada de acesso ou segundo resgate; assinatura inválida rejeitada. Avaliar também duas requisições simultâneas em homologação para detectar diferenças do SQL Server que InMemory não cobre.

### T07 — DT18: tempo, calendário e reembolso

1. Abrir o mesmo pedido em navegadores com fusos diferentes; comparar JSON UTC com horário exibido.
2. Conferir que datas civis de voucher e transação não mudam de dia pela conversão.
3. Validar vigência inclusiva do voucher, duração em meses do snapshot e elegibilidade de reembolso no limite de 14 dias.
4. Comparar registros históricos com valores originais, sem deslocar horas no banco.

Esperado: instantes JSON com `Z`, apresentação local correta, calendário de negócio UTC e regra de reembolso inclusiva. Limites exatos/ticks devem ser verificados por `UtcTimeTests`, sem alterar o relógio da máquina compartilhada.

### T08 — DT19: slug e rastreabilidade

1. Criar produtos com o mesmo slug, incluindo produto inativo; editar mantendo o próprio slug.
2. Com duas sessões Admin, tentar criar simultaneamente o mesmo slug.
3. Conferir HTTP 409 no conflito, um único registro no banco e índice habilitado.
4. Inspecionar `X-Correlation-ID` na resposta e o mesmo ID nos logs; seguir checkout por CheckoutCorrelationId, pedido, sessão e evento Stripe.
5. Em falha controlada de homologação, conferir mensagem genérica ao cliente e exceção/operação no log.

Esperado: unicidade sob concorrência, retries sem troca indevida de chave e correlação útil sem exposição de segredos. Impacto: índice melhora suporte à consulta, mas ganho precisa ser medido; logging adiciona volume.

### T09 — DT20: contratos e regressão funcional

1. Exercitar produtos, categorias, transações, relatórios, vouchers, pedidos e Admin; conferir paginação enviada.
2. Desligar temporariamente a API de homologação e repetir uma consulta; religar e tentar novamente.
3. Conferir login inválido e erro de checkout: mensagens específicas preservadas, sem corpo bruto/stack trace.
4. Usar a suíte HttpContractTests para corpo vazio, JSON inválido e ProblemDetails.

Esperado: nenhum falso sucesso nem dados antigos retornados em resposta de erro; checkout funciona sem Stripe.js no frontend; status persistidos continuam 1–6. Rede indisponível segue o tratamento próprio do fluxo.

### T10 — DT21: Swagger por ambiente

Consultar `/swagger/index.html`, `/swagger/v1/swagger.json` e `/swagger/swagger-ui-bundle.js` na API direta e via proxy. Esperado: 404 em Production/Staging, mesmo com EnableSwagger=true; 200 somente em Development com opção true. Confirmar que não recebeu HTML da SPA com status 200. Se WAF/proxy responder 401/403, isso comprova bloqueio externo, não ausência do Swagger na aplicação.

### T11 — DT24: cookies e mesma origem no publicado

1. No navegador corporativo afetado, bloquear cookies de terceiros; abrir o site em HTTPS.
2. Fazer login, carregar dashboard, chamar rota protegida, recarregar e sair.
3. Em Network, conferir chamadas para a origem do frontend em `/api/`, sem redirecionamento ao domínio separado da API nem repetição `/api/api/`.
4. Conferir cookie HttpOnly/Secure, exclusão no logout e eventuais cookies divididos em partes; repetir em duas abas.
5. Conferir 401 anônimo e 403 sem permissão, sem fallback HTML da SPA.

Esperado: fluxo funciona sem liberar cookies de terceiros ou limpar cookies manualmente. Impacto/dependência: vínculo real frontend–API no Azure, HTTPS e proxy. A exclusão de todas as partes de cookies grandes ainda precisa ser comprovada.

### T12 — DT25: saída nos dois temas

1. Em claro e escuro, sair do dashboard; conferir tema estável no processamento, sucesso e login.
2. Simular rede lenta e falha de logout; clicar repetidamente em sair.
3. Restaurar a rede e tentar novamente; após sucesso, abrir login e usar voltar/avançar.

Esperado: processamento visível, sucesso só após resposta da API, falha com nova tentativa, ausência de chamadas simultâneas e de flashes/erros. Consultar Network para verificar que a limpeza local não provoca consultas extras a `/me` e `/roles`.

### T13 — DT26: cancelamento Admin

1. Abrir `/admin/orders/{id}` com pedido aguardando pagamento, prazo futuro e checkout seguro para encerramento; confirmar cancelamento.
2. Conferir status Cancelado, sessão encerrada quando aplicável, reserva liberada e auditoria antes/depois.
3. Repetir com pedido pago, vencido, sem prazo, voucher resgatado ou checkout em processamento/concluído: bloquear.
4. Testar pagamento concorrente ao cancelamento com serviço simulado/teste automatizado; conferir preservação de pagamento e reserva.

Esperado: servidor revalida elegibilidade; pedido pago segue reembolso, vencido segue expiração; histórico permanece. Impacto: chamadas ao gateway e disputa de RowVersion, sem cancelamento indevido.

### T14 — DT26: expiração sem interação

1. Criar pedido com voucher e aguardar o prazo sem consultar/abrir o pedido. Manter API ativa.
2. Conferir expiração e reserva pelo banco/log; defaults: pedido 30 min, varredura a cada 60 s após concluir a passagem anterior.
3. Em homologação, reduzir `OrderExpiration:PendingOrderLifetimeMinutes` e `SweepIntervalSeconds` antes de criar novos pedidos para facilitar o ensaio; registrar/restaurar configuração.
4. Reiniciar a API com pedidos vencidos; conferir processamento na passagem inicial.
5. Conferir pedido legado sem ExpiresAt, checkout incerto e mais de 100 vencidos; usar a suíte para simulação de falhas e lotes.

Esperado: Expirado, checkout encerrado quando seguro e reserva liberada; incerteza bloqueia e tenta de novo; legado sem prazo não recebe expiração inventada. O processo precisa permanecer ativo. Medir atraso real: intervalo é entre passagens e inclui tempo de processamento, sem SLA de vencimento exato.

### T15 — DT27: impacto no carregamento

1. Medir tempo desde navegação até a primeira tela útil pronta para interação, com o mesmo perfil, rede, dispositivo e conjunto de dados.
2. Repetir ao menos cinco vezes sem cache, com cache e após inatividade da infraestrutura; separar anônimo, usuário comum e Admin.
3. Registrar total, download/inicialização Blazor, autenticação, chamadas API e tempo de consultas SQL; usar Network/Performance e logs da API. Instrumentação SQL exige medição adicional no ambiente.
4. Comparar baseline anterior e versão integrada nas mesmas condições, em ambientes isolados com schemas compatíveis; registrar mediana e faixa observada.

Esperado: impacto quantificado antes de definir meta/otimização. Não há otimização nem meta numérica já comprovada na DT27. Para carga, observar também custo de validação de cookie/security stamp/sessão por chamada, polling de sessão, auditoria e varredura de pedidos.

### T16 — DT28: sessão por inatividade e duração absoluta

1. Fazer login e deixar a página visível sem ponteiro/teclado/rolagem/toque por mais de 15 min; polling não pode renovar. Conferir redirecionamento/aviso e 401 em rota protegida.
2. Interagir periodicamente por mais de 15 min: permanecer conectado; observar POST de atividade com intervalo mínimo de 30 s e GET de validade a cada 15 s.
3. Conferir GET sem mudança em LastActivityUtc; POST válido pode atualizar atividade. Para inspecionar: `SELECT Id, CreatedUtc, LastActivityUtc FROM dbo.UserSessions;`.
4. Suspender aba por mais de 15 min e retornar; repetir com retorno do Stripe após vencimento.
5. Abrir duas abas; sair em uma, entrar com outra conta e conferir sincronização sem dados da conta anterior.
6. Desconectar rede: não interpretar falha como expiração; reconectar e validar com servidor. Um 403 não encerra a sessão.
7. Manter interação até 8 h: sessão expira mesmo com atividade. Usar UserSessionTests para verificar o limite rapidamente com relógio controlado; ensaio real de 8 h é aceitação distinta.
8. Reiniciar API e, se houver várias instâncias, alternar entre elas: validade permanece consistente com banco e chaves de proteção de cookies compartilhados.

Esperado: servidor rejeita no instante do limite; indicação visual pode esperar a próxima verificação e atrasar em aba suspensa. Cookies anteriores à DT28 exigem novo login. Logout revoga também cópia do mesmo cookie; validar isso pela suíte sem exportar cookie para evidências. Impacto: novas consultas por requisição, polling e gravações de atividade.

### T17 — DT29: identidade visual do Admin

1. Entrar como Admin em claro/escuro; navegar páginas Admin e páginas comuns.
2. Conferir paleta azul e identificação textual Admin; sair e entrar como usuário comum.
3. Repetir troca de conta em duas abas, com a DT28 ativa.

Esperado: Admin azul inclusive fora de `/admin`, usuário comum/anônimo verde; tema e identidade atualizam sem manter dados/cores da conta anterior. Conferir contraste, textos, botões e ausência de flashes.

### T18 — DT30: aceitação da implantação integrada

Repetir T01, T02, T05, T06, T10, T11, T14, T16 e T17 na versão publicada. Confirmar migrations no banco publicado, ambiente Production, vínculo `/api`, HTTPS/cookies, assinatura do webhook, processo ativo para expiração e compartilhamento de chaves entre instâncias. Consultar também `DT30-CONSOLIDACAO-PRODUCAO.md`. Limites de requisições/WAF/monitoramento continuam pendências de avaliação registradas; não presumir implementação.

## 5. Registro dos resultados e critérios de liberação

Para cada caso, preencher:

| Teste | Versão/alterações locais | Ambiente/banco | Dados/IDs usados | Resultado esperado x observado | Duração/impacto | Evidência | Situação |
| --- | --- | --- | --- | --- | --- | --- | --- |
| T01–T18 | | | | | | | Não executado / Aprovado / Falhou / Bloqueado |

Registrar comparação de carregamento, latência das rotas, consultas SQL, volume de logs, atraso de expiração e custo das sessões. Sem baseline, registrar valor atual como referência, sem afirmar melhora/regressão quantitativa.

Bloqueiam liberação: migration necessária ausente, duplicidade sob concorrência, cobrança/acesso/resgate duplicado, autorização indevida, cancelamento de pagamento confirmado, sessão ultrapassando limites ou autenticação publicada falhando com bloqueio de cookies de terceiros. Validações visuais e de desempenho devem ter resultado explícito; build/testes locais não encerram DT24/DT27/DT30.

## 6. Verificação executada neste levantamento

- Teste Node do monitor de sessão: 1 aprovado, 0 falhas.
- EF `has-pending-model-changes`: modelo compatível com a última migration.
- Suíte .NET completa: 167 aprovados, 0 falhas, 0 ignorados; duração dos testes de aproximadamente 1 min 2 s. Evidência: `TestResults/avaliacao-dts/avaliacao-dts.trx`.
- API, Core e frontend compilaram durante `dotnet test`. Ocorreram avisos de nullability/MudBlazor e NU1900 por indisponibilidade da consulta de vulnerabilidades NuGet; não houve erro de compilação. Builds separados não foram executados neste levantamento.
- Nenhum banco externo foi consultado ou atualizado; a lista de migrations pendentes depende da consulta da seção 2.
- Há divergência documental: o resumo DT27/DT29 descreve DT28 como pendente no commit, enquanto o documento específico e código local já contêm a implementação. Este roteiro avalia o estado local completo.

Referências: documentos DT18–DT30 nesta pasta, `RETOMADA-DT13-DT16.md`, migrations e classes de testes listadas acima; histórico Git para DT12–DT17.
