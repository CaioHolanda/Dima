using Dima.Api.Data;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Stripe;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;

namespace Dima.Api.Handlers
{
    public class OrderHandler(AppDbContext context, IStripeHandler stripeHandler) : IOrderHandler
    {
        public async Task<Response<Order?>> CancelAsync(CancelOrderRequest request)
        {
            Order? order;
            // Primeira analise: Pedido pode ser cancelado?
            try
            {
                order = await context
                    .Orders
                    .Include(x => x.Product)
                    .Include(x => x.Voucher)
                    .FirstOrDefaultAsync(x =>
                            x.Id == request.Id &&
                            x.UserId == request.UserId);
                if (order is null)
                    return new Response<Order?>(null, 404, "[E035] Pedido nao encontrado");
            }
            catch 
            {
                return new Response<Order?>(null, 404, "[E036] Falha ao obter o pedido");
            }
            switch(order.Status)
            {
                case EOrderStatus.Canceled:
                    return new Response<Order?>(order, 400, "[E037] Pedido ja cancelado");
                case EOrderStatus.WaintingPayment:
                    break;
                case EOrderStatus.Paid:
                    return new Response<Order?>(order, 400, "[E038] Pedido ja pago nao pode ser cancelado");
                case EOrderStatus.Refunded:
                    return new Response<Order?>(order, 400, "[E039] Pedido ja reembolsado nao pode ser cancelado");
                default:
                    return new Response<Order?>(order, 400, "[E040] Pedido nao pode ser cancelado");
            }
            order.Status = EOrderStatus.Canceled;
            order.UpdatedAt = DateTime.Now;

            // Segunda analise: Podendo ser cancelado atualiza o banco
            try
            {
                context.Orders.Update(order);
                await context.SaveChangesAsync();
            }
            catch 
            {
                return new Response<Order?>(order, 500, "[E041] Nao foi possivel cancelar seu pedido");
            }
            return new Response<Order?>(order, 200, $"Pedido {order.Number} cancelado com sucesso");
        }

        public async Task<Response<Order?>> CreateAsync(CreateOrderRequest request)
        {
            // Produto existe?
            Product? product;
            try
            {
                product = await context
                    .Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.ProductId &&
                        x.IsActive == true);
                if (product is null)
                    return new Response<Order?>(null, 404, "[E041] Produto nao encontrado");
                context.Attach(product);
            }
            catch
            {
                return new Response<Order?>(null, 500, "[E042] Nao foi possivel buscar produto");
            }

            // Ha Voucher?
            Voucher? voucher=null;
            try
            {
                if (request.VoucherId is not null)
                {
                    var voucherId = request.VoucherId.Value;

                    // 1. Existe algum voucher com este ID?
                    voucher = await context.Vouchers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == voucherId);

                    if (voucher is null)
                        return new Response<Order?>(null, 400, $"[E043] Voucher {voucherId} nao encontrado");

                    // 2. O voucher está ativo?
                    if (!voucher.IsActive)
                        return new Response<Order?>(null, 400, $"[E043] Voucher {voucherId} existe, mas esta inativo");

                    // 3. Agora sim, atualiza
                    voucher.IsActive = false;
                    context.Vouchers.Update(voucher);
                }
            }
            catch 
            {
                return new Response<Order?>(null, 500, "[E045] Falha ao obter o Voucher informado");
            }

            // Se existe produto e ha ou nao voucher cria-se o pedido
            var order = new Order
            {
                UserId = request.UserId,
                Product = product,
                ProductId = request.ProductId,
                Voucher = voucher,
                VoucherId = request.VoucherId
            };
            try
            {
                await context.Orders.AddAsync(order);
                await context.SaveChangesAsync();
            }
            catch 
            {
                return new Response<Order?>(null, 500, "[E046] Nao foi possivel realizar seu pedido");
            }

            return new Response<Order?>(order, 201, $"Pedido {order.Number} cadastrado com sucesso");
        }

        public async Task<PagedResponse<List<Order>?>> GetAllAsync(GetAllOrdersRequest request)
        {
            try
            {
                var query = context
                    .Orders
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .Include(x => x.Voucher)
                    .Where(x => x.UserId == request.UserId)
                    .OrderByDescending(x => x.CreatedAt);
                var orders = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();
                var count = await query.CountAsync();
                return new PagedResponse<List<Order>?>(
                    orders,
                    count,
                    request.PageNumber,
                    request.PageSize);
            }
            catch 
            {
                return new PagedResponse<List<Order>?>(null, 500, "[E062] Nao foi possivel listar os pedidos");
            }

        }

        public async Task<Response<Order?>> GetByNumberAsync(GetOrderByNumberRequest request)
        {
            try
            {
                var order = await context
                    .Orders
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .Include(x => x.Voucher)
                    .FirstOrDefaultAsync(x => x.Number == request.Number &&
                                              x.UserId==request.UserId);
                return order is null
                    ? new Response<Order?>(null, 404, "[E063] Pedido nao encontrado")
                    : new Response<Order?>(order);

            }
            catch 
            {

                return new Response<Order?> (null, 500, "[E061] Nao foi possivel consultar pedido");
            }
        }

        public async Task<Response<Order?>> PayAsync(PayOrderRequest request)
        {
            Order? order;
            try
            {
                order = await context
                    .Orders
                    .Include(x => x.Product)
                    .Include(x => x.Voucher)
                    .FirstOrDefaultAsync(x=>x.Number == request.Number && x.UserId==request.UserId);
                if (order is null)
                    return new Response<Order?>(null, 404, "[E047] Pedido nao encontrado");
            }
            catch 
            {
                return new Response<Order?>(null, 500, "[E048] Falha ao buscar pedido");
            }
            switch (order.Status)
            {
                case EOrderStatus.Canceled:
                    return new Response<Order?>(order, 400, "[E049] Pedido cancelado, repagamento nao possivel");
                case EOrderStatus.Refunded:
                    return new Response<Order?>(order, 400, "[E050] Pedido reembolsado, repagamento nao possivel");
                case EOrderStatus.Paid:
                    return new Response<Order?>(order, 400, "[E051] Pedido ja pago, repagamento nao possivel");
                case EOrderStatus.WaintingPayment:
                    break;
                default:
                    return new Response<Order?>(order, 400, "[E052] Falha ao processar pagamento");
            }
            try
            {
                Console.WriteLine($"[PAY] Order Number: {order.Number}");
                var getTransactionsRequest = new GetTransactionsByOrderNumberRequest
                {
                    Number = order.Number
                };
                var result = await stripeHandler.GetTransactionsByOrderNumberAsync(getTransactionsRequest);

                Console.WriteLine($"[STRIPE] Success: {result.IsSuccess}");
                Console.WriteLine($"[STRIPE] Message: {result.Message}");
                Console.WriteLine($"[STRIPE] Data null: {result.Data is null}");
                Console.WriteLine($"[STRIPE] Count: {result.Data?.Count}");

                if (result.IsSuccess == false)
                    return new Response<Order?>(null, 500, "[E084] Nao foi possivel localizar o pagamento");
                if(result.Data is null)
                    return new Response<Order?>(null, 500, "[E085] Nao foi possivel localizar o pagamento");
                if(result.Data.Any(x=>x.Refunded))
                    return new Response<Order?>(null, 400, "[E086] Este pedido ja teve o pagamento informado");
                if(!result.Data.Any(x=>x.Paid))
                    return new Response<Order?>(null, 400, "[E087] Este pedido nao foi pago");
                request.ExternalReference = result.Data[0].Id;
            }
            catch 
            {
                return new Response<Order?>(null, 400, "[E088] Nao foi possivel dar baixa no seu pedido");
            }
            order.Status = EOrderStatus.Paid;
            order.ExternalReference=request.ExternalReference;
            order.UpdatedAt=DateTime.Now;

            // Persistencia em banco
            try
            {
                context.Orders.Update(order);
                await context.SaveChangesAsync();
            }
            catch 
            {
                return new Response<Order?>(order, 500, "[E053] Falha ao processar pagamento");
            }
            return new Response<Order?>(order, 200, $"Pedido {order.Number} pago com sucesso");

        }

        public async Task<Response<Order?>> RefundAsync(RefundOrderRequest request)
        {
            Order? order;
            try
            {
                order = await context
                    .Orders
                    .Include(x => x.Product)
                    .Include(x => x.Voucher)
                    .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == request.UserId);
                if (order is null)
                    return new Response<Order?>(null, 404, "[E060] Pedido nao encontrado");

            }
            catch
            {

                return new Response<Order?>(null, 500, "[E054] Falha ao buscar pedido");
            }
            switch (order.Status)
            {
                case EOrderStatus.Canceled:
                    return new Response<Order?>(order, 400, "[E055] Pedido cancelado, reembolso nao possivel");
                case EOrderStatus.Refunded:
                    return new Response<Order?>(order, 400, "[E056] Pedido reembolsado, reembolso nao possivel");
                case EOrderStatus.Paid:
                    break;
                case EOrderStatus.WaintingPayment:
                    return new Response<Order?>(order, 400, "[E057] Pedido ainda nao foi pago, reembolso nao possivel");
                default:
                    return new Response<Order?>(order, 400, "[E058] Falha ao processar pagamento");
            }
            //Neste ponto se insere o codigo do Stripe (PCI)
            order.Status = EOrderStatus.Refunded;
            order.UpdatedAt = DateTime.Now;

            // Persistencia em banco
            try
            {
                context.Orders.Update(order);
                await context.SaveChangesAsync();
            }
            catch
            {
                return new Response<Order?>(order, 500, "[E059] Falha ao processar reembolso");
            }
            return new Response<Order?>(order, 200, $"Pedido {order.Number} reembolsado com sucesso");


        }
    }
}
