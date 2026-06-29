using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Orders
{
    public class GetVoucherByNumberEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/{number}", HandleAsync)
            .WithName("Voucher: By Number")
            .WithSummary("Read one Voucher")
            .WithDescription("Read one Voucher")
            .WithOrder(1)
            .Produces<Response<Voucher?>>();

        private static async Task<IResult> HandleAsync(
            IVoucherHandler handler,
            string number)
        {
            var request = new GetVoucherByNumberRequest
            {
                Number = number
            };
            var result = await handler.GetByNumberAsync(request);
            return result.IsSuccess
                ? TypedResults.Ok(result)
                : TypedResults.BadRequest(result);
        }
    }
}