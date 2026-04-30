using Concertable.Auth;
using Concertable.Auth.Services;
using Concertable.Auth.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var spaClient = builder.Configuration
    .GetSection(SpaClientSettings.SectionName)
    .Get<SpaClientSettings>() ?? new SpaClientSettings();

builder.Services.AddRazorPages();

builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

builder.Services.AddIdentityServer()
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryClients(Config.Clients(spaClient))
    .AddProfileService<ProfileService>()
    .AddDeveloperSigningCredential();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
