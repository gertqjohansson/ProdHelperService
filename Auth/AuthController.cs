using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;

namespace ProdHelperService.Auth;

[ApiController]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db,
    ITokenService tokenService,
    IMfaChallengeStore mfaChallengeStore,
    IConfiguration configuration) : ControllerBase
{
    private static AuthErrorResponse InvalidCredentials =>
        new() { Code = "InvalidCredentials", Message = "Invalid email or password." };

    [HttpPost(ApiRoutes.AuthRegister)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new AuthErrorResponse
            {
                Code = "RegistrationFailed",
                Message = string.Join(" ", result.Errors.Select(e => e.Description)),
            });
        }

        return Ok(new RegisterResponse { UserId = user.Id, Email = user.Email! });
    }

    [HttpPost(ApiRoutes.AuthHasUsers)]
    public async Task<IActionResult> HasUsers()
    {
        bool hasUsers = await db.Users.AnyAsync();
        return Ok(new HasUsersResponse { HasUsers = hasUsers });
    }

    [HttpPost(ApiRoutes.AuthLogin)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(InvalidCredentials);
        }

        Microsoft.AspNetCore.Identity.SignInResult result =
            await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return Unauthorized(new AuthErrorResponse
            {
                Code = "LockedOut",
                Message = "Account temporarily locked due to repeated failed attempts.",
            });
        }
        if (!result.Succeeded)
        {
            return Unauthorized(InvalidCredentials);
        }

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            TokenResponse tokens = await IssueTokensAsync(user, ["pwd"]);
            return Ok(new LoginResponse { MfaRequired = false, Tokens = tokens });
        }

        string challenge = mfaChallengeStore.CreateChallenge(user.Id);
        return Ok(new LoginResponse { MfaRequired = true, MfaToken = challenge });
    }

    [HttpPost(ApiRoutes.AuthVerifyMfa)]
    public async Task<IActionResult> VerifyMfa(VerifyMfaRequest request)
    {
        string? userId = mfaChallengeStore.Resolve(request.MfaToken);
        ApplicationUser? user = userId is null ? null : await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized(new AuthErrorResponse
            {
                Code = "InvalidChallenge",
                Message = "MFA challenge is invalid or has expired.",
            });
        }

        bool codeValid = await userManager.VerifyTwoFactorTokenAsync(
            user, TokenOptions.DefaultAuthenticatorProvider, request.Code);
        if (!codeValid)
        {
            // A wrong guess does not burn the challenge - the user can retry
            // with a fresh code from their authenticator app.
            await userManager.AccessFailedAsync(user);
            return Unauthorized(new AuthErrorResponse { Code = "InvalidCode", Message = "Invalid authentication code." });
        }

        mfaChallengeStore.Consume(request.MfaToken);
        await userManager.ResetAccessFailedCountAsync(user);
        TokenResponse tokens = await IssueTokensAsync(user, ["pwd", "mfa"]);
        return Ok(tokens);
    }

    [HttpPost(ApiRoutes.AuthRefresh)]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        string hash = tokenService.HashToken(request.RefreshToken);
        RefreshToken? existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (existing is null)
        {
            return Unauthorized(new AuthErrorResponse { Code = "InvalidToken", Message = "Refresh token is invalid." });
        }

        if (existing.RevokedUtc is not null)
        {
            // The presented token was already rotated out once before — this is
            // a theft/replay signal, so the whole token family is revoked, not
            // just this one request rejected.
            await RevokeAllActiveTokensAsync(existing.UserId);
            return Unauthorized(new AuthErrorResponse
            {
                Code = "TokenReuseDetected",
                Message = "Refresh token has already been used. All sessions have been signed out.",
            });
        }

        if (!existing.IsActive)
        {
            return Unauthorized(new AuthErrorResponse { Code = "TokenExpired", Message = "Refresh token has expired." });
        }

        ApplicationUser? user = await userManager.FindByIdAsync(existing.UserId);
        if (user is null)
        {
            return Unauthorized(new AuthErrorResponse { Code = "InvalidToken", Message = "Refresh token is invalid." });
        }

        string[] amr = await userManager.GetTwoFactorEnabledAsync(user) ? ["pwd", "mfa"] : ["pwd"];
        TokenResponse tokens = await IssueTokensAsync(user, amr);

        existing.RevokedUtc = DateTime.UtcNow;
        existing.ReplacedByTokenHash = tokenService.HashToken(tokens.RefreshToken);
        await db.SaveChangesAsync();

        return Ok(tokens);
    }

    [Authorize]
    [HttpPost(ApiRoutes.AuthLogout)]
    public async Task<IActionResult> Logout(LogoutRequest request)
    {
        string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string hash = tokenService.HashToken(request.RefreshToken);

        RefreshToken? token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (token is not null && token.UserId == currentUserId && token.RevokedUtc is null)
        {
            token.RevokedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        // Always 200, regardless of whether a matching token was found — don't
        // leak whether the presented refresh token existed.
        return Ok();
    }

    [Authorize]
    [HttpPost(ApiRoutes.AuthMfaAuthenticatorSetup)]
    public async Task<IActionResult> SetupAuthenticator()
    {
        ApplicationUser? user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            return Conflict(new AuthErrorResponse
            {
                Code = "AlreadyEnabled",
                Message = "Two-factor authentication is already enabled. Disable it before setting up again.",
            });
        }

        // Resetting always issues a brand-new secret, which is why this is
        // guarded above — running it on an already-scanned app would silently
        // break that existing authenticator entry.
        await userManager.ResetAuthenticatorKeyAsync(user);
        string unformattedKey = (await userManager.GetAuthenticatorKeyAsync(user))!;

        string issuer = configuration["Jwt:Issuer"] ?? "ProdHelperService";
        string uri = TotpUriHelper.BuildAuthenticatorUri(issuer, user.Email ?? user.UserName ?? user.Id, unformattedKey);
        string qr = TotpUriHelper.GenerateQrCodePngBase64(uri);

        return Ok(new AuthenticatorSetupResponse
        {
            SharedKey = unformattedKey,
            AuthenticatorUri = uri,
            QrCodePngBase64 = qr,
        });
    }

    [Authorize]
    [HttpPost(ApiRoutes.AuthMfaAuthenticatorEnable)]
    public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorRequest request)
    {
        ApplicationUser? user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        bool codeValid = await userManager.VerifyTwoFactorTokenAsync(
            user, TokenOptions.DefaultAuthenticatorProvider, request.Code);
        if (!codeValid)
        {
            return BadRequest(new AuthErrorResponse { Code = "InvalidCode", Message = "Invalid authentication code." });
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);

        // Shown exactly once — Identity only stores these hashed, there is no
        // way to retrieve them again later, only regenerate a new set.
        IEnumerable<string> recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10) ?? [];

        return Ok(new EnableAuthenticatorResponse { RecoveryCodes = recoveryCodes.ToArray() });
    }

    [Authorize]
    [HttpPost(ApiRoutes.AuthMfaAuthenticatorDisable)]
    public async Task<IActionResult> DisableAuthenticator(DisableAuthenticatorRequest request)
    {
        ApplicationUser? user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        // Step-up confirmation: a merely-open access token shouldn't be enough
        // on its own to silently turn off account security.
        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new AuthErrorResponse { Code = "InvalidPassword", Message = "Incorrect password." });
        }

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);

        // Disabling MFA changes the account's security posture enough to
        // warrant signing out every other active session too.
        await RevokeAllActiveTokensAsync(user.Id);

        return Ok();
    }

    private async Task<TokenResponse> IssueTokensAsync(ApplicationUser user, string[] amr)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        (string accessToken, DateTime expiresUtc) = tokenService.CreateAccessToken(user, roles, amr);

        string rawRefreshToken = tokenService.GenerateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashToken(rawRefreshToken),
            CreatedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.Add(tokenService.RefreshTokenLifetime),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
        });
        await db.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            AccessTokenExpiresUtc = expiresUtc,
        };
    }

    private async Task RevokeAllActiveTokensAsync(string userId)
    {
        List<RefreshToken> activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedUtc == null)
            .ToListAsync();

        DateTime now = DateTime.UtcNow;
        foreach (RefreshToken token in activeTokens)
        {
            token.RevokedUtc = now;
        }
        await db.SaveChangesAsync();
    }
}
