using Concertable.Auth.Services;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class RegisterModel : PageModel
{
    private readonly IAuthService authService;
    private readonly IIdentityServerInteractionService interaction;

    public RegisterModel(IAuthService authService, IIdentityServerInteractionService interaction)
    {
        this.authService = authService;
        this.interaction = interaction;
    }

    [BindProperty] public string Email { get; set; } = null!;
    [BindProperty] public string Password { get; set; } = null!;
    [BindProperty] public string? SelectedRole { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public bool Submitted { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<string> AvailableRoles { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var context = await interaction.GetAuthorizationContextAsync(ReturnUrl);
        AvailableRoles = GetAvailableRoles(context?.Client?.ClientId);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var context = await interaction.GetAuthorizationContextAsync(ReturnUrl);
        var clientId = context?.Client?.ClientId;
        AvailableRoles = GetAvailableRoles(clientId);

        if (clientId is null)
        {
            ErrorMessage = "Sign up must be initiated from a Concertable surface.";
            return Page();
        }

        if (clientId == "business-mobile" && string.IsNullOrEmpty(SelectedRole))
        {
            ErrorMessage = "Please select a valid role.";
            return Page();
        }

        var verifyUrl = $"{Request.Scheme}://{Request.Host}/Account/VerifyEmail";
        var result = await RegisterByClientAsync(clientId, verifyUrl, ct);

        switch (result)
        {
            case RegisterResult.Success:
                Submitted = true;
                break;
            case RegisterResult.EmailAlreadyExists:
                ErrorMessage = "An account with that email already exists.";
                break;
            default:
                ErrorMessage = "Invalid registration request.";
                break;
        }

        return Page();
    }

    private Task<RegisterResult> RegisterByClientAsync(string clientId, string verifyUrl, CancellationToken ct)
        => clientId switch
        {
            "customer-web" or "customer-mobile" => authService.RegisterCustomerAsync(Email, Password, verifyUrl, ct),
            "venue-web" => authService.RegisterVenueManagerAsync(Email, Password, verifyUrl, ct),
            "artist-web" => authService.RegisterArtistManagerAsync(Email, Password, verifyUrl, ct),
            "business-mobile" => SelectedRole switch
            {
                "VenueManager" => authService.RegisterVenueManagerAsync(Email, Password, verifyUrl, ct),
                "ArtistManager" => authService.RegisterArtistManagerAsync(Email, Password, verifyUrl, ct),
                _ => Task.FromResult(RegisterResult.InvalidRole)
            },
            _ => Task.FromResult(RegisterResult.InvalidRole)
        };

    private static IReadOnlyList<string> GetAvailableRoles(string? clientId) => clientId switch
    {
        "business-mobile" => ["VenueManager", "ArtistManager"],
        _ => []
    };
}
