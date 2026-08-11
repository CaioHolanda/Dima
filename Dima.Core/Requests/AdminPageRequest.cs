namespace Dima.Core.Requests;

public class AdminPagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}