using Dima.Api.Data;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers
{
    public class OrderHandler(AppDbContext context) : IOrderHandler
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
                    return new Response<Order?>(null, 404, "Pedido nao encontrado");
            }
            catch 
            {
                return new Response<Order?>(null, 404, "Falha ao obter o pedido");
            }
            switch(order.Status)
            {
                case EOrderStatus.Canceled:
                    return new Response<Order?>(order, 400, "Pedido ja cancelado");
                case EOrderStatus.WaintingPayment:
                    break;
                case EOrderStatus.Paid:
                    return new Response<Order?>(order, 400, "Pedido ja pago nao pode ser cancelado");
                case EOrderStatus.Refunded:
                    return new Response<Order?>(order, 400, "Pedido ja reembolsado nao pode ser cancelado");
                default:
                    return new Response<Order?>(order, 400, "Pedido nao pode ser cancelado");
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
                return new Response<Order?>(order, 500, "Nao foi possivel cancelar seu pedido");
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
                    return new Response<Order?>(null, 404, "Produto nao encontrado");
                context.Attach(product);
            }
            catch
            {
                return new Response<Order?>(null, 500, "Nao foi possivel buscar produto");
            }

            // Ha Voucher?
            Voucher? voucher = null;
            try
            {
                if(request.VoucherId is not null)
                {
                    voucher = await context
                        .Vouchers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.Id == request.VoucherId &&
                            x.IsActive == true);
                    if (voucher is null)
                        return new Response<Order?>(null, 400, "Voucher invalido ou nao encontrado");
                    if (voucher.IsActive == false)
                        return new Response<Order?>(null, 400, "Este voucher ja foi utilizado");
                    voucher.IsActive = false;
                    context.Vouchers.Update(voucher);
                }
            }
            catch 
            {
                return new Response<Order?>(null, 500, "Falha ao obter o Voucher informado");
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
                return new Response<Order?>(null, 500, "Nao foi possivel realizar seu pedido");
            }

            return new Response<Order?>(null, 201, $"Pedido {order.Number} cadastrado com sucesso");
        }

        public async Task<Response<List<Order>?>> GetAllAsync(GetAllOrdersRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<List<Order>?>> GetByNumberAsync(GetOrderByNumberRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Order?>> PayAsync(PayOrderRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Order?>> RefundAsync(RefundOrderRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
