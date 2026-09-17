namespace Dima.Web;

public sealed class ThemeState
{
    private bool _isDarkMode = true;
    public event Action? Changed;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode == value) return;
            _isDarkMode = value;
            Changed?.Invoke();
        }
    }
}
