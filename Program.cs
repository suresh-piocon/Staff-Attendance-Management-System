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
var supabaseUrl = builder.Configuration["https://sawhokfpoyhzgvvxjnej.supabase.co"] ?? "";
var supabaseKey = builder.Configuration["sb_publishable_jse-ZnMwsUTy-SRXlkvO6A_5xW3MKhB"] ?? "";

var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = false
};

var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);
await supabaseClient.InitializeAsync();

builder.Services.AddSingleton(supabaseClient);

// Auth services
builder.Services.AddSingleton<AuthService>();
builder.Services.AddScoped<SupabaseAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<SupabaseAuthStateProvider>());
builder.Services.AddAuthorizationCore();

// App services
builder.Services.AddScoped<SalesmanService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<FollowUpService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
