namespace Dima.Core.Requests.Payment
{
    public class GetTransactionsByOrderNumberRequest : Request
    {
        public string Number { get; set; } = string.Empty;
    }
}