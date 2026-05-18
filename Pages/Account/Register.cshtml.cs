using Concertable.Auth.Services;
using Concertable.User.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Concertable.Auth.Pages.Account;

public sealed class RegisterModel : PageModel
{
    private readonly IAuthService authService;
    private readonly IClientRoleResolver clientRoleResolver;

    public RegisterModel(IAuthService authService, IClientRoleResolver clientRoleResolver)
    {
        this.authService = authService;
        this.clientRoleResolver = clientRoleResolver;
    }

    [BindProperty] public string Email { get; set; } = null!;
    [BindProperty] public string Password { get; set; } = null!;
    [BindProperty] public string? SelectedRole { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public bool Submitted { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<Role> AvailableRoles { get; private set; } = [];

    public async Task OnGetAsync() =>
        AvailableRoles = await clientRoleResolver.GetAllowedRolesAsync(ReturnUrl);

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        AvailableRoles = await clientRoleResolver.GetAllowedRolesAsync(ReturnUrl);
        var resolution = await clientRoleResolver.ResolveRoleAsync(ReturnUrl, SelectedRole);

        ErrorMessage = resolution switch
        {
            RoleResolution.UnknownClient  => "Sign up must be initiated from a Concertable surface.",
            RoleResolution.InvalidSelection => "Please select a valid role.",
            _ => null
        };

        if (ErrorMessage is not null) return Page();

        var role = ((RoleResolution.Resolved)resolution).Role;
        var verifyUrl = $"{Request.Scheme}://{Request.Host}/Account/VerifyEmail";
        var result = await authService.RegisterAsync(Email, Password, role, verifyUrl, ct);

        switch (result)
        {
            case RegisterResult.Success:
                Submitted = true;
                break;
            case RegisterResult.EmailAlreadyExists:
                ErrorMessage = "An account with that email already exists.";
                break;
            case RegisterResult.RoleNotAllowed:
                ErrorMessage = "Cannot self-assign this role.";
                break;
            case RegisterResult.InvalidRole:
                ErrorMessage = "Invalid role.";
                break;
        }

        return Page();
    }
}
