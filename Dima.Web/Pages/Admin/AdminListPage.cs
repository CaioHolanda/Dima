using Dima.Core.Requests;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace Dima.Web.Pages.Admin;
public abstract class AdminListPage<T> : ComponentBase
{
    public bool IsBusy { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public int PageNumber { get; private set; } = 1;
    public int PageSize { get; private set; } = 25;
    public int TotalCount { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool? ActiveFilter { get; private set; }
    public bool? PremiumFilter { get; private set; }
    public Dima.Core.Enums.EOrderStatus? StatusFilter { get; private set; }
    private int _version;
    [Inject] public ISnackbar Snackbar { get; set; } = null!;
    protected abstract Task<PagedResponse<List<T>?>> FetchAsync();
    protected abstract void SetItems(List<T> items);
    protected void Configure(AdminPagedRequest request)
    {
        request.PageNumber = PageNumber; request.PageSize = PageSize;
        request.SearchTerm = SearchTerm; request.IsActive = ActiveFilter;
        if (request is Dima.Core.Requests.Users.GetAllAdminUsersRequest users) users.IsPremium = PremiumFilter;
        if (request is Dima.Core.Requests.Order.GetAllAdminOrdersRequest orders) orders.Status = StatusFilter;
    }
    protected void RestoreOrderList(int page, int size, string? search, Dima.Core.Enums.EOrderStatus? status)
    {
        PageNumber = Math.Max(1, page);
        PageSize = Math.Clamp(size, 1, 100);
        SearchTerm = search ?? string.Empty;
        StatusFilter = status;
    }
    protected override Task OnInitializedAsync() => ReloadAsync();
    public async Task ReloadAsync()
    {
        var version = ++_version;
        IsBusy = true;
        try
        {
            var result = await FetchAsync();
            if (version != _version) return;
            if (!result.IsSuccess) { SetItems([]); TotalCount = 0; Snackbar.Add(result.Message ?? "Não foi possível carregar a lista", Severity.Error); return; }
            TotalCount = result.TotalCount;
            if (PageNumber > TotalPages) { PageNumber = TotalPages; await ReloadAsync(); return; }
            SetItems(result.Data ?? []);
        }
        catch (Exception ex) { if (version == _version) { SetItems([]); TotalCount = 0; Snackbar.Add(ex.Message, Severity.Error); } }
        finally { if (version == _version) IsBusy = false; }
    }
    public async Task SearchAsync(string value) { SearchTerm = value; PageNumber = 1; await ReloadAsync(); }
    public async Task ChangeActiveAsync(bool? value) { ActiveFilter = value; PageNumber = 1; await ReloadAsync(); }
    public async Task ChangePremiumAsync(bool? value) { PremiumFilter = value; PageNumber = 1; await ReloadAsync(); }
    public async Task ChangeStatusAsync(Dima.Core.Enums.EOrderStatus? value) { StatusFilter = value; PageNumber = 1; await ReloadAsync(); }
    public async Task ChangePageSizeAsync(int value) { PageSize = value; PageNumber = 1; await ReloadAsync(); }
    public async Task PreviousAsync() { if (PageNumber > 1) { PageNumber--; await ReloadAsync(); } }
    public async Task NextAsync() { if (PageNumber < TotalPages) { PageNumber++; await ReloadAsync(); } }
}
