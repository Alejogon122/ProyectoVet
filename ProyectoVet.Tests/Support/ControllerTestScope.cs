using System.Security.Claims;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ProyectoVet.Security;

namespace ProyectoVet.Tests.Support;

internal sealed class ControllerTestScope<T> : IDisposable where T : Controller
{
    private readonly ServiceProvider _services;
    public T Controller { get; }
    public AuthenticationSpy Authentication { get; } = new();

    public ControllerTestScope(T controller, string? role = null, int employeeId = 1)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllersWithViews();
        services.AddSingleton<IAuthenticationService>(Authentication);
        _services = services.BuildServiceProvider();
        var http = new DefaultHttpContext
        {
            RequestServices = _services,
            Session = new MemorySession(),
            User = role == null ? new ClaimsPrincipal(new ClaimsIdentity()) :
                EmployeeCookieEvents.CreatePrincipal(employeeId, "Empleado de prueba", role)
        };
        Controller = controller;
        Controller.ControllerContext = new ControllerContext { HttpContext = http, RouteData = new RouteData() };
        Controller.TempData = new TempDataDictionary(http, new MemoryTempDataProvider());
    }

    public void Dispose() => _services.Dispose();

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private sealed class MemorySession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new();
        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _data.Keys;
        public void Clear() => _data.Clear();
        public void Remove(string key) => _data.Remove(key);
        public void Set(string key, byte[] value) => _data[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _data.TryGetValue(key, out value);
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

internal sealed class AuthenticationSpy : IAuthenticationService
{
    public ClaimsPrincipal? SignedInPrincipal { get; private set; }
    public string? SignInScheme { get; private set; }
    public string? SignOutScheme { get; private set; }
    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.NoResult());
    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
    {
        SignedInPrincipal = principal;
        SignInScheme = scheme;
        return Task.CompletedTask;
    }
    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        SignOutScheme = scheme;
        return Task.CompletedTask;
    }
}
