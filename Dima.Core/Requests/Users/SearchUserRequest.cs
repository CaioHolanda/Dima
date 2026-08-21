namespace Dima.Core.Requests.Users;

public class SearchUsersRequest
{
    public string SearchTerm { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
}