using Dima.Core.Enums;

namespace Dima.Web.Components.Orders;

public class RefundDialogResult
{
    public ERefundReason Reason { get; set; }
    public string? Details { get; set; }
}