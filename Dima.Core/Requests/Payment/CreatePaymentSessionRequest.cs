namespace Dima.Core.Requests.Payment
{
    public class CreatePaymentSessionRequest : Request
    {
        public string OrderNumber { get; set; } = string.Empty;

    }
}