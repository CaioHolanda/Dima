namespace Dima.Core.Requests.Payment
{
    public class CreatePaymentSessionRequest : Request
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public long OrderTotal { get; set; }
    }
}