namespace Dima.Web.Security;

public sealed class SessionSignals
{
    public event Action? Unauthorized;
    public void Reject() => Unauthorized?.Invoke();
}
