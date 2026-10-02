using Dima.Core.Enums;
using Microsoft.AspNetCore.Components;

namespace Dima.Web.Components.Orders
{
    public partial class OrderStatusComponent:ComponentBase
    {
        [Inject] public TimeProvider Clock { get; set; } = null!;
        #region Parameters

        [Parameter, EditorRequired]
        public EOrderStatus Status { get; set; }

        [Parameter]
        public bool IsComplimentary { get; set; }

        [Parameter]
        public DateTimeOffset? ExpiresAt { get; set; }
        protected bool HasElapsed =>
            ExpiresAt.HasValue &&
            ExpiresAt.Value <= Clock.GetUtcNow();

        #endregion
    }
}
