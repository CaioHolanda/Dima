# DT-24 — Autenticação sem depender de cookie de terceiros

Status: implementação principal já presente no código; validação de implantação
pendente na **DT-30 — Consolidação de produção**. Prioridade original: P0.
Revisão local: 17/09/2026. Não encerrar a DT24 como validada em produção.

## Evidências no código

- `Dima.Web/Program.cs`: fora de Development, a base da API é derivada de
  `HostEnvironment.BaseAddress`, com `/api/`. O BackendUrl separado só é usado
  em Development. A normalização evita a repetição do sufixo `/api`.
- `Dima.Api/Endpoints/Endpoint.cs`: os endpoints estão no grupo `/api`, incluindo
  `/api/v1/identity/login-user` e `/api/v1/identity/logout`.
- `Dima.Web/Security/CookieHandler.cs`: envia credenciais nas requisições do
  navegador e o cabeçalho `X-Requested-With: XMLHttpRequest`.
- `Dima.Api/Common/Api/BuilderExtension.cs`: cookie HttpOnly, Secure obrigatório
  fora de Development e `StaticWebAppsCookieManager` nesse ambiente. A API
  responde 401/403 em vez de redirecionar para páginas de login/acesso negado.
- `Dima.Api/Common/Api/StaticWebAppsCookieManager.cs`: logout emite cookie vazio
  com `Max-Age=0`, preservando domínio, caminho e atributos de segurança.
- `LogoutEndpoint` chama `SignOutAsync`; `AccountHandler.LogoutAsync` verifica
  o resultado HTTP antes de considerar a solicitação bem-sucedida.

O cookie ainda usa SameSite=None fora de Development. Isso não comprova
dependência de terceiros: a requisição precisa efetivamente permanecer na mesma
origem no navegador. Não alterar essa configuração sem avaliar o fluxo publicado.

## Limites da evidência local

Validação local: 32 testes existentes aprovados, sem falhas, selecionando
`HttpContractTests`, `AdminHttpAuthorizationTests` e
`Application_cookie_validates_stamp_on_every_request`. Cobrem contratos HTTP,
login/autorização com cookie em Development e configuração de revogação por
security stamp. Não cobrem o proxy Azure, políticas corporativas nem a exclusão
de cookies divididos em partes. A compilação apresentou avisos preexistentes.

`staticwebapp.config.json` não declara o vínculo com a API. O workflow do frontend
tem `api_location` vazio; o workflow da API é separado. Esses arquivos não
comprovam que `/api` está vinculado ao backend no Azure. Sem o vínculo efetivo,
usar uma URL de mesma origem no cliente não resolve a autenticação.

A emissão delega ao `ChunkingCookieManager`, mas a exclusão atual apaga somente
o cookie principal. Verificar cookies divididos em partes e resíduos no logout
na DT30; a limpeza de todas as partes não está comprovada por esta revisão.

## Critério de encerramento

Executar e registrar os cenários da seção DT24 da DT30 no ambiente publicado,
incluindo o navegador corporativo afetado com cookies de terceiros bloqueados.
Build e testes locais não substituem essa comprovação.

O acabamento visual do logout continua na DT25; revogação de credenciais na
DT13 e expiração por inatividade na DT28.
