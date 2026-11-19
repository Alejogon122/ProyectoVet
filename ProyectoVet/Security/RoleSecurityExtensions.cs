using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace ProyectoVet.Security;

public static class RoleSecurityExtensions
{
    public static IMvcBuilder AddRoleSecurity(this IServiceCollection services)
    {
        services.AddScoped<EmployeeCookieEvents>();
        services.AddSingleton<IAuthorizationHandler, RoleAccessHandler>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.LoginPath = "/Auth/Login";
            options.AccessDeniedPath = "/Auth/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(2);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.EventsType = typeof(EmployeeCookieEvents);
        });
        services.AddAuthorization(options => options.AddPolicy(RoleAccess.Policy,
            policy => policy.RequireAuthenticatedUser().AddRequirements(new RoleAccessRequirement())));
        return services.AddControllersWithViews(options =>
            options.Filters.Add(new AuthorizeFilter(RoleAccess.Policy)));
    }
}
