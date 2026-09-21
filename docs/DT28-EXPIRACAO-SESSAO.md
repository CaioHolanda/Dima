# DT-28 — Expiração da sessão por inatividade

Prioridade: P1. Prazos aprovados pelo usuário: **15 minutos sem interação** e
**8 horas de duração absoluta**. Implementação local em 18/09/2026; validação
no navegador e implantação ainda pendentes.

## Comportamento

Cada login cria uma sessão no banco (`UserSessions`) e inclui seu identificador
nas propriedades protegidas do cookie. Toda requisição com cookie valida os
dois prazos no servidor, além do security stamp da DT13. O cookie não usa
renovação deslizante; consultas, carregamentos e polling não prolongam a sessão.
O vencimento ocorre inclusive no instante exato do limite. Cookies anteriores
à implementação não contêm o identificador e exigem novo login.

`GET /api/v1/identity/session` consulta a validade sem alterar a atividade.
`POST /api/v1/identity/session/activity` registra atividade somente enquanto
a sessão permanece válida. A atualização condicional no banco impede que uma
requisição concorrente reative uma sessão vencida. Exige os cabeçalhos
`X-Requested-With: XMLHttpRequest` e `X-Dima-Session` correspondente ao cookie;
uma aba com outro identificador recebe 409 e deve descartar seu estado antigo.
Interação sinalizada pelo navegador não comprova criptograficamente presença
humana; o limite absoluto permanece obrigatório.

O frontend reporta eventos confiáveis de ponteiro, teclado, rolagem e toque,
com intervalo mínimo de 30 segundos entre os registros. Verifica a validade
a cada 15 segundos e ao retornar à aba, sem renovar por esses eventos de retorno.
Com isso, a indicação visual pode ocorrer até uma verificação após o vencimento;
o servidor já rejeita qualquer requisição autenticada fora do prazo.
Abas suspensas e o retorno do Stripe seguem a mesma validação.

401 em sessão autenticada limpa a autenticação e recarrega a página de login,
descartando os serviços e dados da conta anterior. O login apresenta aviso de
expiração. 403 permanece um erro de permissão e não encerra a sessão. Falhas de
rede não são interpretadas como expiração; ao reconectar, o servidor determina
a validade. Respostas de autenticação que começaram antes de uma limpeza não
podem restaurar o estado antigo.

Login/logout são sincronizados por eventos de armazenamento entre abas; o polling
serve de alternativa quando o armazenamento está indisponível. Uma troca de
identificador exige recarregamento para descartar dados da conta anterior.
Logout remove o registro da sessão, invalidando também cópias do mesmo cookie.
Sessões com mais de 8 horas são removidas nos logins seguintes. O banco compartilhado
preserva a política entre reinícios e instâncias; as instâncias também precisam
compartilhar corretamente as chaves de proteção de dados dos cookies.

## Implantação

Aplicar a migration `AddUserSessions` **antes** de publicar a API modificada.
O script [DT28-MIGRATION.sql](DT28-MIGRATION.sql) corresponde à atualização
de `AddUniqueProductSlug` para `AddUserSessions`. Não foi aplicado a bancos
locais ou de produção nesta implementação. Ao publicar, usuários com cookies
antigos terão de entrar novamente.

## Validação

Resultado local: **47 testes .NET aprovados**, sem falhas, na seleção abaixo;
**1 teste Node aprovado**. A compilação de API/frontend foi concluída com
avisos preexistentes e indisponibilidade da consulta de vulnerabilidades NuGet.
`dotnet ef migrations has-pending-model-changes` confirmou que não há alterações
do modelo pendentes de migration.

Testes HTTP com cookie real e SQLite usam relógio controlado para conferir os
limites, consultas sem renovação, atividade, revogação no logout, security stamp,
403 e bloqueio de atividade de outra sessão. Os testes administrativos existentes
verificam que a política de autorização foi preservada.

O teste Node do módulo JavaScript verifica consultas automáticas, distinção
de eventos confiáveis, intervalo de atividade, retomada, troca de conta e tratamento
de falhas de rede/403. Executar com:

```powershell
node --test Dima.Tests/Browser/session-monitor.test.mjs
dotnet test Dima.Tests --no-restore --filter 'FullyQualifiedName~UserSessionTests|FullyQualifiedName~AdminHttpAuthorizationTests|FullyQualifiedName~AdminUserSecurityTests|FullyQualifiedName~HttpContractTests'
```

Antes de encerrar em produção, validar duas abas, aba suspensa por mais de
15 minutos, retorno do Stripe após vencimento, logout, troca de conta,
interação durante 8 horas e sincronização via proxy da hospedagem. Esses
cenários reais de navegador não são comprovados pelos testes locais.
