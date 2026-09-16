namespace Dima.Core.Requests;

public class AdminPagedRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 25;
    public int PageNumber { get => _pageNumber; set => _pageNumber = Math.Clamp(value, 1, 1000000); }
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public int PageSize { get => _pageSize; set => _pageSize = Math.Clamp(value, 1, 100); }
}