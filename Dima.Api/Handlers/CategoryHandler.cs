using Dima.Api.Data;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Categories;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers
{
    public class CategoryHandler(AppDbContext context) : ICategoryHandler
    {
        public async Task<Response<Category?>> CreateAsync(CreateCategoryRequest request)
        {
            try
            {
                var category = new Category
                {
                    UserId = request.UserId,
                    Title = request.Title,
                    Description = request.Description
                };
                await context.Categories.AddAsync(category);
                await context.SaveChangesAsync();

                return new Response<Category?>(category,201,"Category sucessufully created");
            }
            catch
            {
                return new Response<Category?>(null,500,"[E001] Category not could be created");
            }
        }

        public async Task<Response<Category?>> DeleteAsync(DeleteCategoryRequest request)
        {
            try
            {
                var category = await context
                    .Categories
                    .FirstOrDefaultAsync(
                        x => x.Id == request.Id &&
                        x.UserId == request.UserId);
                if (category is null)
                    return new Response<Category?>(null, 404, "[E004] Category not found");

                context.Categories.Remove(category);
                await context.SaveChangesAsync();
                return new Response<Category?>(category, message: "Category removed sucessufully");
            }
            catch
            {
                return new Response<Category?>(null, 500, "[E005] Category not could be removed");
            }

        }

        public Task<PagedResponse<List<Category>>> GetAllAsync(GetAllCategoriesRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<Response<Category?>> GetByIdAsync(GetCategoryByIdRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Category?>> UpdateAsync(UpdateCategoryRequest request)
        {
            try
            {
                var category = await context
                    .Categories
                    .FirstOrDefaultAsync(
                        x => x.Id == request.Id &&
                        x.UserId == request.UserId);
                if (category is null)
                    return new Response<Category?>(null, 404, "[E002] Category not found");
                category.Title = request.Title;
                category.Description = request.Description;

                context.Categories.Update(category);
                await context.SaveChangesAsync();
                return new Response<Category?>(category, message: "Category updated sucessufully");
            }
            catch
            {
                return new Response<Category?>(null, 500, "[E003] Category not could be updated");
            }
        }
    }
}
