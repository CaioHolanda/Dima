# Dima
Saas Financial Aplication for didatic propose

## DT16 — Auditoria administrativa

A tabela `AdminAuditLog` registra as chamadas de escrita realizadas pelo administrador
em produtos, vouchers e usuários, além de cancelamentos e solicitações de reembolso.
Registra o identificador e nome do autor, data UTC, operação, alvo, código HTTP,
sucesso/falha e snapshots JSON anteriores e posteriores com campos selecionados.
Senhas, security stamps, cookies e tokens não são registrados. Não existe interface
ou endpoint de consulta; o histórico é consultado diretamente no banco.

Aplicar a migration antes de executar a API:

```sh
dotnet ef database update --project Dima.Api --startup-project Dima.Api
```

Exemplo de consulta:

```sql
SELECT TOP (100) * FROM AdminAuditLog ORDER BY OccurredAtUtc DESC, Id DESC;
```

A auditoria ocorre após a execução do endpoint e usa um contexto separado.
Ela registra o resultado da solicitação de reembolso; a confirmação assíncrona
pelo gateway permanece nos registros do pedido/webhook. Falhas ao gravar a auditoria
são registradas no log da API sem desfazer a operação. Assim, este mecanismo simples
não garante atomicidade entre a operação e seu registro nem impede alterações diretas
no banco. Leituras, ações de clientes e requisições rejeitadas antes do endpoint
(autorização ou binding inválido) não são incluídas. Futuras operações de exclusão ou
arquivamento na rota administrativa de pedidos herdam o filtro de auditoria.
