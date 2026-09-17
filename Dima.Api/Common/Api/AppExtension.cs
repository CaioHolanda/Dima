using Dima.Api.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Dima.Api.Common.Api
{
    public static class AppExtension
    {
        public static void ConfigureDevEnvironment(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment()
                || !app.Configuration.GetValue<bool>("EnableSwagger"))
                return;

            app.UseSwagger();
            app.UseSwaggerUI();
        }
        public static void UserSecurity(this WebApplication app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }
    }
}
