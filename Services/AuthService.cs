using Supabase;
using Supabase.Gotrue;
using System.Threading.Tasks;

namespace SalesmanAttendance.Services
{
    public class AuthService
    {
        private readonly Supabase.Client _supabase;

        public AuthService(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<Session?> SignInAsync(string email, string password)
        {
            var session = await _supabase.Auth.SignIn(email, password);
            return session;
        }

        public async Task SignOutAsync()
        {
            await _supabase.Auth.SignOut();
        }

        public Session? GetCurrentSession()
        {
            return _supabase.Auth.CurrentSession;
        }

        public User? GetCurrentUser()
        {
            return _supabase.Auth.CurrentUser;
        }

        public bool IsAuthenticated => _supabase.Auth.CurrentSession != null;

        public string GetUserRole()
        {
            var user = _supabase.Auth.CurrentUser;
            if (user?.UserMetadata != null &&
                user.UserMetadata.TryGetValue("role", out var role))
            {
                return role?.ToString() ?? "Salesman";
            }
            return "Salesman";
        }
    }
}
