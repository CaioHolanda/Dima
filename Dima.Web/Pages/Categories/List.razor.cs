using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Categories;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Runtime.InteropServices;

namespace Dima.Web.Pages.Categories
{
    public partial class ListCategoriesPage:ComponentBase
    {
        #region Properties
        public bool IsBusy { get; set; } = false;
        public List<Category> Categories { get; set; } = [];
        public string SearchTerm { get; set; } = string.Empty;
        #endregion

        #region Services
        [Inject]
        public ISnackbar Snackbar { get; set; } = null!;
        [Inject]
        public ICategoryHandler Handler { get; set; } = null;
        #endregion

        #region Methods

        public Func<Category, bool> Filter => category =>
        {
            if (string.IsNullOrEmpty(SearchTerm))
                return true;
            if (category.Id.ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                return true;
            if (category.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                return true;
            if (category.Description is not null && category.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;

        };

        #endregion



        #region Overrides
        protected override async Task OnInitializedAsync()
        {
            IsBusy = true;
            try
            {
                var request = new GetAllCategoriesRequest();
                var result = await Handler.GetAllAsync(request);
                if (result.IsSuccess)
                    Categories = result.Data ?? [];
            }
            catch(Exception ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion
    }
}
