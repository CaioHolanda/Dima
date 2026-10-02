# DT-28 — Expiração da sessão por inatividade

Revisão local em 29/09/2026, após o usuário concluir a validação da DT26.
Alternativa aprovada: manter `UserSessions` no SQL, sem Redis, eliminar polling
periódico de validade e agrupar atividade em **1 minuto**. Mantidos **15 minutos
de inatividade** e **8 horas de duração absoluta**. **Status: implementação e validação local concluídas**, com base nos testes automatizados e nos cenários confirmados pelo usuário;
verificações complementares e implantação descritas abaixo. O encerramento local não declara aprovação de cenários manuais ainda não executados nem validação na hospedagem.

## Comportamento

Cada login cria uma sessão no banco e coloca seu identificador nas propriedades
protegidas do cookie. Toda requisição autenticada valida os prazos no servidor e
o security stamp da DT13. Cookie sem renovação deslizante; consultas automáticas
não renovam a sessão. O servidor rejeita inclusive no instante exato do limite.
Reabrir o aplicativo não cria uma sessão nova; somente novo login cria o prazo
absoluto. Logout remove o registro e invalida cópias do cookie.

`GET /api/v1/identity/session` retorna `sessionId`, `expiresUtc` e `serverUtc`,
sem registrar atividade. A leitura já feita na autenticação é reutilizada apenas
na mesma requisição HTTP; não há cache entre requisições. O horário do servidor
permite calcular a duração restante sem depender do ajuste do relógio local.

`POST /api/v1/identity/session/activity` registra atividade se a sessão ainda
estiver válida. UPDATE condicional impede reativação de sessão vencida; depois
é feita nova leitura do prazo. Exige `X-Requested-With: XMLHttpRequest` e
`X-Dima-Session` igual ao identificador do cookie; divergência retorna 409.
O servidor usa o horário de recebimento, sem aceitar timestamps do navegador.
Interação sinalizada não comprova presença humana; as 8 horas continuam obrigatórias.

## Navegador sem polling

- Consulta ao autenticar e ao retomar foco/visibilidade, sem renovar nesses eventos.
- Nenhum `setInterval` nem GET a cada 15 segundos. Um temporizador local indica
  expiração no prazo conhecido, sem consultar o banco naquele instante.
- Ponteiro, teclado, rolagem e toque confiáveis, em aba visível, marcam atividade
  pendente. Eventos são agrupados e geram no máximo um POST por minuto **por aba**.
  O último evento do intervalo é enviado mesmo sem outro clique posterior.
- Não havendo atividade nova, não há POST nem repetição automática. Os avisos
  continuam num endpoint dedicado: chamadas de negócio não renovam implicitamente
  a sessão, para evitar que polling e carregamentos sejam tratados como interação.
- Adota-se tolerância aproximada de até um minuto no registro, em condições normais
  de rede e execução. Suspensão, indisponibilidade ou atraso podem perder atividade;
  avisos recebidos após o vencimento não reativam a sessão.
- Prazos confirmados, login e logout são comunicados por BroadcastChannel e eventos
  de localStorage. Uma aba parada acompanha a renovação feita em outra, sem GET.
  O limite de um minuto é por aba, não um bloqueio distribuído entre todas as abas.
- Ao retornar de suspensão ou do Stripe, consultar o servidor antes de enviar nova
  atividade. Uma aba oculta pode adiar a indicação visual até a retomada.
- Sem ambos os mecanismos de comunicação entre abas, a sincronização ocorre ao
  retomar a aba ou pela próxima requisição autenticada. Uma aba pode apresentar
  expiração pelo prazo antigo; não se reintroduz polling como fallback.

401 limpa a autenticação e recarrega o login, descartando dados da conta anterior.
409/troca de identificador também descartam o estado antigo. 403 e falha de rede
não são tratados como 401; porém o prazo local já conhecido continua valendo.
Requisições do monitor têm timeout de 15 segundos, sem retry automático em loop.
Respostas anteriores a logout/descarte não restauram a sessão.

## Integração e limites

DT13: preservada validação do security stamp a cada requisição. Bloqueios e
alterações de segurança continuam efetivos na próxima requisição autenticada.
Sem polling, a tela ociosa não descobre uma revogação administrativa imediatamente;
ela se atualiza na próxima requisição, retomada ou expiração local.

DT26: preservado o agendamento durável por Storage Queue e Azure Function, já
validado pelo usuário. A antiga varredura SQL foi substituída na base atual.
Esta revisão reduz o tráfego da DT28; DT27 deve medir o resultado e o custo
restante. Ver DT26-AGENDAMENTO-DURAVEL.md.

Sessões antigas são limpas em logins seguintes. Não há worker varrendo sessões.
Banco compartilhado e chaves de proteção dos cookies mantêm validade entre
reinícios/instâncias; nenhum prazo depende da memória de uma instância da API.

## Encerramento local

Escopo principal aceito: agrupamento em um minuto, expiração por inatividade, sincronização entre abas/logout e recuperação após falha curta de rede. Evidência automatizada: 75 testes .NET e 13 testes JavaScript aprovados. Não há alteração de código neste encerramento. Versionamento autorizado pelo usuário exclusivamente na branch `dev`, sem merge na `main` ou deploy. Publicação e verificação na hospedagem permanecem pendentes; os cenários manuais não confirmados continuam identificados na seção de aceitação.

## Implantação

Esta revisão não cria migration nem altera o esquema. `AddUserSessions` continua
sendo pré-requisito da implementação original; o script `DT28-MIGRATION.sql`
é incremental e não precisa ser reaplicado se a migration já estiver instalada.
Publicar API e frontend compatíveis: o novo monitor utiliza `serverUtc`.
Nenhuma alteração de banco ou publicação foi executada nesta revisão.

## Validação

Resultado em `C:\Codes\Dima`, sobre a base atual da `dev`: **75 testes .NET e 13 testes Node aprovados**,
sem falhas. API e frontend compilados; permanecem avisos preexistentes de
nullability e componentes Razor/MudBlazor. `git diff --check` sem erros.

Testes Node com relógio e rede simulados cobrem ausência de polling, agrupamento,
atividade pendente, expiração local, diferença de relógio, duas abas via mensagens,
retomada, 401/403/409, falha de rede/storage e respostas atrasadas após logout.
Testes HTTP reais locais com SQLite verificam limites de 15 minutos/8 horas,
revogação, security stamp e reaproveitamento da leitura somente por requisição.

```powershell
node --test Dima.Tests/Browser/session-monitor.test.mjs
dotnet test Dima.Tests --no-restore --filter 'FullyQualifiedName~UserSessionTests|FullyQualifiedName~AdminHttpAuthorizationTests|FullyQualifiedName~AdminUserSecurityTests|FullyQualifiedName~HttpContractTests|FullyQualifiedName~OrderExpirationWorkerTests|FullyQualifiedName~ScheduledOrderExpirationTests|FullyQualifiedName~OrderSchedulingTransactionTests'
```

## Aceitação local confirmada pelo usuário

Confirmados nesta conversa, em ambiente localhost:

- Avisos de atividade aproximadamente a cada 60 segundos, com o mesmo identificador de sessão e prazo renovado em 15 minutos.
- Expiração por inatividade e retorno ao login: último aviso às 21:51:24 locais, prazo às 22:06:24 e rejeição 401 nesse horário. Trecho do log sem consultas entre 21:55:19 e 22:06:24, coerente com ausência de polling.
- Duas abas no mesmo navegador/perfil: atividade em uma mantém a sessão da outra por mais de 15 minutos.
- Sem atividade nas duas abas, ambas encerram a sessão, com atualização da aba oculta ao retomar.
- Logout em uma aba encerra a sessão compartilhada na outra.
- Falha curta de rede após novo login: usuário interagiu no frontend com a API inacessível e abriu uma segunda aba, que não concluiu a coleta de dados offline. Ao voltar online, o acesso foi restabelecido, a segunda aba carregou o perfil e os avisos de atividade foram retomados normalmente. Não foi relatada exigência de novo login. A ausência de repetição excessiva de chamadas durante a falha não foi confirmada separadamente nesse relato.

A confirmação de duas abas refere-se ao roteiro imediatamente anterior enviado ao usuário. Não implica aprovação de troca de conta, suspensão do computador, indisponibilidade prolongada de rede, bloqueio de storage/BroadcastChannel, revogação administrativa ou retorno do Stripe. Esses cenários reais e a validação na hospedagem continuam pendentes. O aviso visual de sessão expirada não foi comprovado na captura do login. O limite de 8 horas permanece coberto pelos testes automatizados, sem confirmação manual de um ensaio de 8 horas.

Roteiro completo de aceitação no navegador (repetir na hospedagem após publicação):

1. Aba visível parada: nenhum GET periódico nem POST de atividade; login exibido
   após o prazo conhecido. Desconsiderar as chamadas iniciais de autenticação.
2. Digitar/clicar por vários minutos: no máximo um POST por minuto por aba;
   parar logo após um aviso e conferir envio do último grupo pendente.
3. Duas abas: interagir em uma por mais de 15 minutos; a outra acompanha o prazo.
   Repetir logout e troca de conta, verificando descarte de dados antigos.
4. Suspender por mais de 15 minutos e voltar; repetir retorno do Stripe.
5. Rede indisponível e storage bloqueado: sem tempestade de consultas; retomar
   online e conferir validação. Bloquear também BroadcastChannel para o fallback.
6. Bloquear usuário/mudar permissões: próxima requisição rejeita a credencial antiga.
7. Limite absoluto de 8 horas e cookie copiado após logout: rejeição no servidor.

Testes automatizados não substituem validação de navegador, proxy Azure e Stripe.
