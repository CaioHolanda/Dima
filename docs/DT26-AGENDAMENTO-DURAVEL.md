# DT26 — Expiração por mensagem durável

Implementação em C:\codes\dima. Substitui a varredura SQL permanente por tarefas
no Azure Storage Queue, consumidas pelo projeto Dima.OrderJobs (Azure Functions
isolated, .NET 10). NÃO cria nem publica recursos Azure automaticamente.

## Situação de ativação

Código implementado; conta Storage, filas, Function e configurações ainda precisam
ser provisionadas e validadas. NÃO publicar a API isoladamente.
Sem fila configurada/disponível, novos pedidos pagos não são confirmados. Login,
navegação e pedidos gratuitos não precisam do agendamento. A DT28 não mudou.

## Fluxo e consistência

1. A criação grava pedido e voucher dentro da transação SQL existente.
2. Antes do commit, envia uma mensagem durável com ID, número e instante de criação.
   A visibilidade é adiada até ExpiresAt. A mensagem não tem expiração por TTL.
3. Falha de envio: rollback SQL e resposta de erro; nenhum pedido pago confirmado
   sem agendamento. Isso exige transação relacional (produção usa SQL Server).
4. Envio confirmado seguido de rollback/commit incerto pode deixar uma mensagem
   órfã, nunca um pedido confirmado sem tarefa. Ausência do pedido provoca novas
   tentativas e, ao esgotar o limite, preservação em fila poison para diagnóstico.
   A identidade composta impede atuar sobre outro registro.
5. A Function lê somente o pedido da mensagem. Pago/cancelado/finalizado: termina.
   Prazo estendido pelo checkout: agenda no novo prazo antes de concluir a mensagem.
   Prazo vencido: reutiliza OrderExpirationService, incluindo checkout, voucher
   e RowVersion. Não há varredura do banco nem temporizador SQL.
6. Mensagens podem ser entregues mais de uma vez: estados finais são ignorados e
   concorrência continua protegida pelo RowVersion. Não há promessa de exactly-once.
7. Falhas de SQL (incluindo retomada do Serverless), Stripe, concorrência ou estado
   incerto: a Function falha. O host tenta novamente, em regra após 30 minutos,
   até cinco entregas, depois move para <fila>-poison. Reinício abrupto do host
   tem política própria de visibilidade do runtime. Alarmar e tratar poison;
   nenhuma reserva é liberada só por atingir o limite de tentativas.
8. A visibilidade de Storage Queue é limitada a sete dias. Prazos maiores geram
   uma verificação intermediária e novo agendamento, sem antecipar a expiração.

Não é uma transação distribuída: aceitamos mensagens órfãs/duplicadas em troca
de nunca confirmar a compra antes de obter confirmação durável do agendamento.
O envio ocorre com a transação SQL aberta; timeout e retries de rede são limitados.
Esse custo e bloqueio pontual só ocorrem ao criar um pedido.

## Infraestrutura e configuração

Criar uma conta Storage e uma Function em plano de execução sob demanda que
suporte .NET 10 isolated (por exemplo Flex Consumption, verificar região/preço).
Separar DEV e PROD: não compartilhar fila, conexão SQL nem credenciais Stripe.
Criar a fila dima-order-expiration e dima-order-expiration-poison antes de ativar.
Não há criação de fila no caminho da compra.

API (User Secrets no desenvolvimento; App Settings/Key Vault no Azure):
- OrderExpirationQueue:ConnectionString — conexão da conta Storage da fila.
- OrderExpirationQueue:QueueName — dima-order-expiration.
- No Azure, usar __ em vez de : nas chaves hierárquicas.

Function (App Settings):
- FUNCTIONS_WORKER_RUNTIME = dotnet-isolated.
- AzureWebJobsStorage — armazenamento do runtime.
- OrderExpirationStorage — MESMA conta/fila usada pela API.
- OrderExpirationQueueName = dima-order-expiration.
- ConnectionStrings__DefaultConnection — banco do ambiente.
- StripeApiKey — credencial Stripe do mesmo ambiente.
- Demais ApiOptions quando necessárias. Nunca copiar appsettings.Development.json.
- Acesso de rede da Function ao SQL e Storage deve ser configurado.
- Configurar alerta na fila dima-order-expiration-poison e em falhas da Function.

Esta versão usa connection strings; mantê-las nos cofres/configurações do host,
nunca no Git ou em comandos com valores reais no histórico. Avaliar identidade
gerenciada na preparação de produção.

A Function não inicia o Program da API, não executa SeedRoles e não faz consulta
SQL ao iniciar. O host verifica a FILA quando ocioso (operações Storage têm custo),
mas só instancia/consulta o contexto SQL quando há mensagem entregue.
O host e a infraestrutura continuam tendo seus próprios custos; não é custo zero.

## Desenvolvimento local

Requer Azurite (Queue + Blob para o runtime), Functions Core Tools v4 compatível
com .NET 10 e SDK .NET 10. A API sozinha não consome as tarefas.
1. Iniciar Azurite e criar as duas filas.
2. Copiar local.settings.example.json para local.settings.json (ignorado pelo Git)
   e preencher SQL/Stripe do ambiente de desenvolvimento.
3. Na API configurar OrderExpirationQueue:ConnectionString=UseDevelopmentStorage=true
   e OrderExpirationQueue:QueueName=dima-order-expiration via User Secrets.
4. Na pasta Dima.OrderJobs executar func start.
5. Executar API/frontend normalmente.

O armazenamento do Azurite deve persistir entre reinícios; apagar seus dados
remove os agendamentos. Não usar fila em memória como substituta silenciosa.

## Publicação e pedidos existentes

1. Preparar Storage, filas, Function, configuração e alertas; validar em DEV.
2. Coordenar a parada da versão antiga da API (incluindo instâncias antigas) para
   evitar manter o OrderExpirationWorker antigo rodando.
3. Publicar a Function e a API compatíveis; confirmar fila e SQL do mesmo ambiente.
4. Executar UMA VEZ, com configuração do ambiente correto:
   dotnet run --project Dima.Api -- --schedule-existing-orders
   Esse comando não inicia servidor HTTP nem faz seed: agenda os pedidos ainda
   aguardando pagamento que tenham prazo. É retomável/repetível: duplicatas são
   toleradas. Pode acordar o SQL, mas não fica rodando.
5. Pedidos legados sem ExpiresAt exigem revisão manual; não recebem prazo inventado.
6. Confirmar fila processada, mudança do pedido, liberação de reserva e ausência
   de mensagens poison. Depois verificar se SQL consegue pausar sem outros acessos.

Também usar o comando de backfill como recuperação controlada de tarefas perdidas
ou poison, após corrigir a causa. Ele não é um cron nem substitui os alertas.

Nenhuma migration SQL nova. A migration UserSessions da DT28 continua necessária
para as funcionalidades de sessão já existentes.

## Limites e validação

Uma tarefa pode chegar atrasada (fila, retomada do SQL, indisponibilidade). As
verificações existentes nas operações de compra continuam valendo; o job não
transforma um pedido vencido em válido até o processamento. Incerteza de pagamento
mantém reserva bloqueada para reconciliação, nunca provoca cancelamento cego.

Mensagens já agendadas para pedidos depois pagos/cancelados ainda geram uma leitura
no vencimento; a fila Storage não oferece remoção simples pelo ID do pedido.
Chamadas da DT28 continuam podendo manter SQL ativo com usuários autenticados.
Acordar o SQL para um vencimento ainda gera cobrança até a próxima pausa.

Testes locais cobrem expiração direcionada, duplicidade, estados finais, extensão
do checkout, falha de reagendamento, pagamento incerto, identidade e backfill.
Teste relacional SQLite verifica commit após envio e rollback do pedido/voucher
quando a fila falha; não substitui teste de concorrência em SQL Server real.
Validar a integração Azure (entrega/reinício/poison/permissões/custo) antes de produção.
## Resultado local — 21/09/2026

- 178 testes .NET aprovados, zero falhas.
- Build e publish Release de Dima.OrderJobs concluídos.
- Metadados gerados confirmam QueueTrigger de ExpireOrder.
- Pacote de publicação verificado sem appsettings*.json da API/local.settings.json.
- Nenhum recurso Azure criado, nenhuma migration/backfill executada em banco real.
- Integração real Storage/Function ainda não executada; Core Tools/Azurite não
  estão configurados neste ambiente. Sem declarar validação Azure concluída.

## Onde verificar no portal e como acompanhar custos

1. Na busca do portal, abrir "Storage accounts" / "Contas de armazenamento".
   Verificar a assinatura e todos os grupos de recursos, inclusive RG-Dima-Prod.
   Um servidor SQL não é uma conta Storage.
2. Abrir a conta do ambiente, depois "Data storage" > "Queues" (ou Storage browser).
   Confirmar dima-order-expiration e dima-order-expiration-poison.
   Uma mensagem com visibilidade futura pode não aparecer no Peek do portal;
   ausência na lista de mensagens visíveis não prova falha de envio.
3. Na busca, abrir "Function App" / "Aplicativos de funções". Após publicar, deve
   existir uma Function chamada ExpireOrder. Verificar execuções/logs e falhas.
   A existência de dima-api (App Service) não comprova que exista uma Function.
4. Teste com pedido DEV: criar, confirmar agendamento, aguardar vencimento,
   verificar execução e estado Expirado/reserva liberada. Repetir com pedido
   pago e checkout estendido. Conferir ausência de mensagem poison.
5. Em Cost Management > Cost analysis, agrupar por recurso, incluindo Storage,
   Function/plano, Application Insights/Log Analytics e banco. Configurar orçamento
   e alertas. Custos têm atraso de atualização.

Storage cobra armazenamento e operações, inclusive consultas à fila vazia.
Functions cobra execuções e memória/tempo conforme o plano; Flex on-demand pode
ter franquia compartilhada por assinatura, sujeita à elegibilidade. Não habilitar
Always Ready sem avaliar o custo fixo; monitoramento/logs também podem ser cobrados.
Um pedido pode gerar mais de uma execução por checkout estendido, retry ou duplicata.
O SQL continua cobrando durante execução e até pausar. Não afirmar gratuidade ou
economia garantida sem estimativa da região, assinatura e volume.

Referências:
- https://azure.microsoft.com/pricing/details/storage/queues/
- https://azure.microsoft.com/pricing/details/functions/
- https://learn.microsoft.com/azure/azure-functions/functions-bindings-storage-queue-trigger