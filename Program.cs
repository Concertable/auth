using Concertable.Auth;
using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.Auth.Data;
using Concertable.Auth.Data.Events;
using Concertable.Auth.Data.Seeders;
using Concertable.Auth.Services;
using Concertable.Auth.Settings;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel;
using Concertable.Kernel.Extensions;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Messaging.Application.Extensions;
using Concertable.Messaging.AzureServiceBus.Extensions;
using Concertable.Messaging.Infrastructure.Extensions;
using Concertable.ServiceDefaults;
using Concertable.Shared.Blob.Infrastructure.Extensions;
using Concertable.Shared.Email.Infrastructure.Extensions;
using Concertable.Shared.Geocoding.Infrastructure.Extensions;
using Concertable.Shared.Imaging.Infrastructure.Extensions;
using Concertable.Shared.Pdf.Infrastructure.Extensions;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.HttpOverrides;
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
builder.Services.AddSharedBlob(builder.Configuration);
builder.Services.AddSharedEmail(builder.Configuration);
builder.Services.AddSharedGeocoding();
builder.Services.AddSharedImaging();
builder.Services.AddSharedPdf();
builder.Services.AddCurrentUser();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddScoped<IDomainEventDispatchInterceptor, DomainEventDispatchInterceptor>();

var authConnectionString = builder.Configuration.GetConnectionString("AuthDb");
builder.Services.AddSeedingInfrastructure();
builder.Services.AddSingleton<AuthConfigurationProvider>();
builder.Services.AddDbContext<AuthDbContext>((sp, opt) =>
    opt.UseSqlServer(authConnectionString)
        .AddInterceptors(
            sp.GetRequiredService<AuditInterceptor>(),
            sp.GetRequiredService<IDomainEventDispatchInterceptor>())
        .UseSeedingSupport(sp));

builder.Services.AddScoped<IDomainEventHandler<CredentialCreatedDomainEvent>, CredentialCreatedDomainEventHandler>();
builder.Services.AddScoped<IProfileClaimsProvider, AuthLocalClaimsProvider>();
builder.Services.AddScoped<IProfileClaimsProvider, B2BProfileClaimsProvider>();
builder.Services.AddScoped<IProfileClaimsProvider, CustomerProfileClaimsProvider>();
builder.Services.AddMemoryCache();
builder.Services.AddClientCredentials(opts =>
{
    opts.Authority = builder.Configuration["Auth:Authority"] ?? builder.Configuration["services__auth__https__0"] ?? "";
    opts.ClientId = builder.Configuration["ServiceAuth:AuthClientId"] ?? "";
    opts.ClientSecret = builder.Configuration["ServiceAuth:AuthClientSecret"] ?? "";
});

builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IDbInitializer, AuthDbInitializer>();
if (!builder.Environment.IsProduction())
    builder.Services.AddScoped<IDevSeeder, AuthDevSeeder>();

builder.Services.AddOutbox(opt => opt.UseSqlServer(authConnectionString), runDispatcher: true);
builder.Services.AddInProcessEventDispatch();
builder.Services.AddAzureServiceBusTransport(
    opts =>
    {
        opts.ConnectionString = builder.Configuration.GetConnectionString("asb") ?? "";
        opts.ServiceName = "concertable-auth";
    },
    reg =>
    {
        reg.Publishes<CredentialRegisteredEvent>();
    });

var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

var clients = new List<Client>(Config.WebClients(spaClient))
{
    Config.CustomerMobileClient(builder.Configuration["Auth:ExpoGoRedirectUri:Customer"]),
    Config.VenueMobileClient(builder.Configuration["Auth:ExpoGoRedirectUri:Business"]),
    Config.ArtistMobileClient(builder.Configuration["Auth:ExpoGoRedirectUri:Business"]),
    Config.ServiceClient("concertable-b2b",
        builder.Configuration["ServiceAuth:B2BClientSecret"]!,
        "payment:write"),
    Config.ServiceClient("concertable-customer",
        builder.Configuration["ServiceAuth:CustomerClientSecret"]!,
        "payment:write"),
    Config.ServiceClient("concertable-auth",
        builder.Configuration["ServiceAuth:AuthClientSecret"]!,
        "user:claims"),
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
    .AddOperationalStore(options =>
    {
        options.ConfigureDbContext = b => b.UseSqlServer(
            builder.Configuration.GetConnectionString("B2BDb"),
            sql => sql.MigrationsAssembly(migrationsAssembly));
        options.DefaultSchema = "idsrv";
    })
    .AddDeveloperSigningCredential();

if (builder.Environment.IsEnvironment("E2E"))
    isBuilder.AddResourceOwnerValidator<ResourceOwnerPasswordValidator>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await initializer.InitializeAsync();
}

app.MapDefaultEndpoints();

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
