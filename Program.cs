using Concertable.Application.Interfaces.Geometry;
using Concertable.Auth;
using Microsoft.AspNetCore.HttpOverrides;
using Concertable.Auth.Services;
using Concertable.Auth.Settings;
using Concertable.Authorization.Infrastructure.Extensions;
using Concertable.Data.Infrastructure.Data;
using Concertable.Data.Infrastructure.Extensions;
using Concertable.Shared.Infrastructure.Extensions;
using Concertable.Shared.Infrastructure.Services.Geometry;
using Concertable.User.Infrastructure.Extensions;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var spaClient = builder.Configuration
    .GetSection(SpaClientSettings.SectionName)
    .Get<SpaClientSettings>() ?? new SpaClientSettings();

builder.Services.AddRazorPages();

if (builder.Environment.IsDevelopment())
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

builder.Services.AddKeyedSingleton<IGeometryProvider, GeographicGeometryProvider>(GeometryProviderType.Geographic, (_, _) =>
    new GeographicGeometryProvider(NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326)));
builder.Services.AddKeyedSingleton<IGeometryProvider, MetricGeometryProvider>(GeometryProviderType.Metric, (_, _) =>
    new MetricGeometryProvider(NtsGeometryServices.Instance.CreateGeometryFactory(srid: 3857)));

builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddAuthorizationModule();
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddScoped<DomainEventDispatchInterceptor>();
builder.Services.AddSharedDbContext(builder.Configuration);
builder.Services.AddUserModule(builder.Configuration);

builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClientRoleResolver, ClientRoleResolver>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

var clients = new List<Client>(Config.WebClients(spaClient))
{
    Config.CustomerMobileClient(builder.Configuration["Auth:ExpoGoRedirectUri:Customer"]),
    Config.BusinessMobileClient(builder.Configuration["Auth:ExpoGoRedirectUri:Business"]),
};
if (builder.Environment.IsEnvironment("E2E"))
    clients.Add(Config.TestClient);

var publicUrl = builder.Configuration["Auth:PublicUrl"];

var isBuilder = builder.Services.AddIdentityServer(options =>
{
    if (!string.IsNullOrEmpty(publicUrl))
        options.IssuerUri = publicUrl;
})
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryClients(clients)
    .AddProfileService<ProfileService>()
    .AddAuthorizeInteractionResponseGenerator<RoleEnforcingInteractionResponseGenerator>()
    .AddOperationalStore(options =>
    {
        options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
            sql => sql.MigrationsAssembly(migrationsAssembly));
        options.DefaultSchema = "idsrv";
    })
    .AddDeveloperSigningCredential();

if (builder.Environment.IsEnvironment("E2E"))
    isBuilder.AddResourceOwnerValidator<ResourceOwnerPasswordValidator>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var grants = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
    await grants.Database.MigrateAsync();
}

app.MapDefaultEndpoints();

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
