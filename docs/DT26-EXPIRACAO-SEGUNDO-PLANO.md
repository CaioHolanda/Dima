# DT-26 — Cancelamento Admin e expiração de pedidos

O cancelamento pelo Admin é elegível apenas para pedidos aguardando pagamento e deve verificar a situação do checkout antes de concluir. Pedidos pagos seguem o fluxo de reembolso. Pedidos vencidos devem seguir o fluxo de expiração.

## Implementado nesta etapa

A API executa uma verificação ao iniciar e repete após cada passagem, com intervalo padrão de 60 segundos. `OrderExpiration:SweepIntervalSeconds` configura esse intervalo, que deve ser positivo. O prazo padrão do pedido continua sendo 30 minutos (`PendingOrderLifetimeMinutes`).

O serviço seleciona pedidos aguardando pagamento com prazo vencido, em lotes de 100, e reutiliza `OrderExpirationService`. Cada pedido usa um contexto separado. A expiração encerra o checkout quando possível, preserva o histórico como Expirado e libera reservas de voucher. Situações de pagamento incertas permanecem bloqueadas e são tentadas na próxima passagem. Falhas são registradas nos logs. As verificações nas ações do usuário permanecem como proteção adicional.

A execução exige que o processo da API esteja ativo. Se o ambiente suspende a aplicação por inatividade, configurar execução contínua na hospedagem ou utilizar um agendador externo. Após reiniciar, a primeira passagem processa os pedidos vencidos durante a indisponibilidade. Não há garantia de atualização no instante exato do vencimento.

Pedidos legados sem `ExpiresAt` exigem verificação própria e não recebem prazo inventado pelo serviço.

## Cancelamento Admin implementado

A página de detalhes do pedido (`/admin/orders/{id}`), acessível pelo número na listagem, oferece Cancelar pedido quando o registro está aguardando pagamento, possui prazo futuro e não contém registros de pagamento ou acesso. A confirmação explica o encerramento do checkout, a liberação da reserva e a preservação do histórico. A página consulta novamente o pedido após a tentativa e informa o resultado.

`POST /api/v1/admin/orders/{id}/cancel` exige a política AdminOnly. O servidor verifica novamente a elegibilidade e reutiliza o encerramento seguro do fluxo de expiração, gravando Cancelado em vez de Expirado. Checkout em processamento ou concluído, tentativa de sessão sem ID recuperado, voucher resgatado, prazo ausente ou vencido e estados diferentes de Aguardando pagamento bloqueiam a ação. RowVersion protege a gravação contra atualização concorrente; pedido e reserva são salvos juntos. A auditoria administrativa existente registra responsável, horário, resultado e snapshots antes/depois. Falhas de persistência da auditoria são registradas nos logs pelo filtro existente.
