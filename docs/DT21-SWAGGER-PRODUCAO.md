# DT-21 — Swagger em produção

Status: corrigida no código local; cinco cenários HTTP de regressão aprovados.
Publicação e validação do ambiente publicado pendentes para a DT consolidada de produção.

## Motivação e correção

O Swagger era registrado em todos os ambientes, antes da autenticação e autorização.
O endpoint de `MapSwagger().RequireAuthorization()` não protegia os caminhos
servidos diretamente por `UseSwagger()` e `UseSwaggerUI()`.

`ConfigureDevEnvironment()` agora registra esses middlewares somente quando
o ambiente é `Development` e `EnableSwagger=true`. A configuração passa a ser
consultada; mesmo uma configuração incorreta com valor true em Production ou
Staging não habilita a documentação. O mapeamento duplicado de Swagger foi removido.

Isso reduz as informações disponíveis para scanners automatizados. A medida
complementa autenticação e autorização; não impede a descoberta de rotas por outros meios.

## Validação local

`SwaggerExposureTests` inicia hosts HTTP locais sem banco ou serviços externos.
Verifica `/swagger/index.html`, `/swagger/v1/swagger.json` e
`/swagger/swagger-ui-bundle.js` nos seguintes cenários:

- Production com EnableSwagger true e false: 404.
- Staging com EnableSwagger true: 404.
- Development com EnableSwagger false: 404.
- Development com EnableSwagger true: 200.

## Pendências para a DT consolidada de produção

- Confirmar que o ambiente publicado está configurado como Production.
- Verificar anonimamente, na URL pública da API e em seus caminhos de proxy,
  que a interface, JSON e arquivos Swagger não são servidos, inclusive com
  variações de rota e redirecionamentos. Respostas 401/403 por proteção externa
  devem ser diferenciadas da ausência da documentação na aplicação.
- Acumular a avaliação de limites de requisições, monitoramento de varreduras
  e regras de firewall/WAF; essas camadas não foram implementadas nesta DT.

Esta lista está referenciada na [DT30 — Consolidação de produção](DT30-CONSOLIDACAO-PRODUCAO.md),
cujo número foi definido pelo usuário em 17/09/2026 para a execução final.
