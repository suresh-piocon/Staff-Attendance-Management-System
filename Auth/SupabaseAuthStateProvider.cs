using Microsoft.AspNetCore.Components.Authorization;
using SalesmanAttendance.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SalesmanAttendance.Auth
{
    public class SupabaseAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthService _authService;
        private static readonly AuthenticationState _anonymous =
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        public SupabaseAuthStateProvider(AuthService authService)
        {
            _authService = authService;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var session = _authService.GetCurrentSession();
            if (session == null)
                return Task.FromResult(_anonymous);

            var user = _authService.GetCurrentUser();
            if (user == null)
                return Task.FromResult(_anonymous);

            var role = _authService.GetUserRole();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.Email ?? ""),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "supabase");
            var principal = new ClaimsPrincipal(identity);

            return Task.FromResult(new AuthenticationState(principal));
        }

        public void NotifyAuthStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
