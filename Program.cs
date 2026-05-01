using Concertable.Application.Interfaces.Geometry;
using Concertable.Auth;
using Concertable.Auth.Services;
using Concertable.Auth.Settings;
using Concertable.Authorization.Infrastructure.Extensions;
using Concertable.Data.Infrastructure.Data;
using Concertable.Data.Infrastructure.Extensions;
using Concertable.Shared.Infrastructure.Extensions;
using Concertable.Shared.Infrastructure.Services.Geometry;
using Concertable.User.Infrastructure.Extensions;
using NetTopologySuite;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var spaClient = builder.Configuration
    .GetSection(SpaClientSettings.SectionName)
    .Get<SpaClientSettings>() ?? new SpaClientSettings();

builder.Services.AddRazorPages();

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
