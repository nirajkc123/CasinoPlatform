using System.Security.Claims;
using CasinoPlatform.Api.DTOs;
using CasinoPlatform.Api.Models;
using CasinoPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CasinoPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IWalletService _walletService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ITokenService tokenService,
        IWalletService walletService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _walletService = walletService;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Conflict(new { message = "An account with this email already exists." });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        // UserManager.CreateAsync hashes the password (PBKDF2 by default) - we
        // never see or store the plaintext password anywhere (Stage 2, item 9).
        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });

        await EnsureRoleExistsAsync("User");
        await _userManager.AddToRoleAsync(user, "User");

        await _walletService.CreateWalletWithInitialGrantAsync(user.Id, ct);

        _logger.LogInformation("New user registered: {UserId}", user.Id);

        return await BuildAuthResponseAsync(user, ct);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        // Always run CheckPasswordAsync even if user is null (against a dummy
        // hash) so login timing doesn't reveal whether the email exists.
        var passwordOk = user is not null && await _userManager.CheckPasswordAsync(user, request.Password);

        if (user is null || !passwordOk || !user.IsActive)
            return Unauthorized(new { message = "Invalid email or password." });

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await BuildAuthResponseAsync(user, ct);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> GetProfile(CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound();

        var wallet = await _walletService.GetWalletAsync(userId, ct);

        return new UserProfileResponse(
            user.Id, user.Email!, user.DisplayName, user.CreatedAtUtc, user.LastLoginAtUtc, wallet.Balance);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = _tokenService.CreateAccessToken(user, roles);
        var wallet = await _walletService.GetWalletAsync(user.Id, ct);

        return new AuthResponse(token, expiresAtUtc, user.Id, user.Email!, user.DisplayName, roles, wallet.Balance);
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
            await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Missing user id claim."));
}
