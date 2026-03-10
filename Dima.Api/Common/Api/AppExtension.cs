using Dima.Api.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Dima.Api.Common.Api
{
    public static class AppExtension
    {
        public static void ConfigureDevEnviroment(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapSwagger().RequireAuthorization();
        }
        public static void UserSecurity(this WebApplication app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }
    }
}
