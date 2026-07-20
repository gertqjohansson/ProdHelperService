using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.Contracts.Translation;
using ProdHelperService.Translation;

namespace ProdHelperService;

[ApiController]
[Authorize]
public class TranslationController(ITranslationService translationService) : ControllerBase
{
    [HttpPost(ApiRoutes.TranslationTranslate)]
    public async Task<IActionResult> Translate(TranslateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Ok(new TranslateResponse { TranslatedText = request.Text ?? string.Empty });
        }

        if (string.IsNullOrWhiteSpace(request.FromLanguageIsoCode) || string.IsNullOrWhiteSpace(request.ToLanguageIsoCode))
        {
            return BadRequest(new AuthErrorResponse { Code = "LanguageRequired", Message = "Both language codes are required." });
        }

        try
        {
            string translated = await translationService.TranslateAsync(request.Text, request.FromLanguageIsoCode, request.ToLanguageIsoCode, cancellationToken);
            return Ok(new TranslateResponse { TranslatedText = translated });
        }
        catch (TranslationUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new AuthErrorResponse { Code = "TranslationUnavailable", Message = "Translation service is currently unavailable." });
        }
    }
}
