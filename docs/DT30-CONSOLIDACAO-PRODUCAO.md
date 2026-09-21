# DT-30 — Consolidação de produção

Status: pendente. Número definido pelo usuário em 17/09/2026 para acumular
as ações e validações de produção ao final das DTs. Este registro não autoriza
nem registra uma implantação já realizada.

## DT24 — Autenticação pela mesma origem (P0 original)

- [ ] Confirmar o backend vinculado/proxy no Azure Static Web Apps e o tratamento
  do prefixo `/api`: a chamada pública `/api/` deve chegar ao endpoint da API,
  sem responder com o HTML do frontend ou duplicar/remover indevidamente o prefixo.
- [ ] Conferir no navegador as URLs de login, consulta de usuário, chamadas
  protegidas e logout: mesma origem do frontend, sem redirecionamento para a
  origem direta da API. Registrar ambiente e versão publicada do frontend/API.
- [ ] No navegador corporativo afetado, com cookies de terceiros bloqueados,
  testar login, navegação autenticada, recarga da página, logout e novo login.
  Repetir em um navegador de referência e registrar versões e política aplicada.
- [ ] Verificar Set-Cookie e armazenamento efetivo: HttpOnly, Secure, Path,
  Domain e SameSite; confirmar envio nas chamadas subsequentes. Não registrar
  valores de cookies, tokens, credenciais ou cabeçalhos sensíveis nas evidências.
- [ ] Confirmar que logout com Max-Age=0 atravessa o proxy, remove o cookie e
  faz a próxima requisição protegida retornar 401. Incluir cookie dividido em
  partes e conferir resíduos; se necessário, corrigir e validar a limpeza antes
  do encerramento. Revogação de uma cópia antiga é assunto distinto, da DT13.
- [ ] Confirmar 401 para acesso anônimo e 403 para usuário sem permissão,
  sem redirecionamento para HTML nem falso sucesso do fallback da SPA.

Critério de aprovação: fluxo funcional no navegador afetado, sem liberar cookies
de terceiros nem exigir limpeza manual. Registrar data, versão/commit,
cenários e resultados sem dados sensíveis. Falhas mantêm a DT24 aberta.

Referência: [revisão da DT24](DT24-AUTENTICACAO-MESMA-ORIGEM.md).

## Pendências de produção já registradas em outras DTs

- [ ] Incorporar as pendências de produção dos documentos das DTs anteriores
  antes da execução final, preservando seus critérios e dependências.
- [ ] DT21: confirmar ambiente Production e ausência de interface, JSON e assets
  Swagger na URL direta e nos caminhos de proxy. Diferenciar bloqueio externo
  401/403 da ausência da documentação na aplicação.
- [ ] DT21: avaliar limites de requisições, monitoramento de varreduras e
  firewall/WAF, conforme o registro existente; essas camadas não foram implementadas.

Referência: [DT21](DT21-SWAGGER-PRODUCAO.md). As demais listas permanecem nos
documentos de origem até a consolidação final; esta lista ainda não é exaustiva.
