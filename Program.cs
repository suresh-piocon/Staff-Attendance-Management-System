using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Supabase;
using SalesmanAttendance.Services;
using SalesmanAttendance.Auth;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<SalesmanAttendance.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Supabase Client
//var supabaseUrl = builder.Configuration["Supabase:Url"] ?? "";
//var supabaseKey = builder.Configuration["Supabase:Key"] ?? "";
var supabaseUrl = builder.Configuration["Supabase:Url"] ?? "";
var supabaseKey = builder.Configuration["Supabase:Key"] ?? "";

var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = false
};

var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);

try
{
    await supabaseClient.InitializeAsync();
}
catch (Exception ex)
{
    // Log to browser console - open DevTools (F12) > Console to see this
    Console.Error.WriteLine($"[Startup] Supabase init warning: {ex.Message}");
    // Do NOT rethrow - let the app boot. Auth failures will surface on login.
}

builder.Services.AddSingleton(supabaseClient);

// Auth services
builder.Services.AddSingleton<AuthService>();
builder.Services.AddScoped<SupabaseAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<SupabaseAuthStateProvider>());
builder.Services.AddAuthorizationCore();

// App services
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<RoundRobinService>();
builder.Services.AddScoped<FollowUpService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
