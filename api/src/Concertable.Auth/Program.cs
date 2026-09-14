using Concertable.Auth;
using Concertable.DataAccess.Application;
using Concertable.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddAuthHost();

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
app.UseDefaultRateLimiting();

app.MapRazorPages();

app.Run();

public sealed partial class Program;
