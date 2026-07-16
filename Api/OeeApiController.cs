using Microsoft.AspNetCore.Mvc;
using ProdHelperService.Contracts;
using ProdHelperService.Controllers.Interface;

namespace ProdHelperService.Api;

[ApiController]
public class OeeApiController : ControllerBase
{
    private readonly IOeeController _oeeController;

    public OeeApiController(IOeeController oeeController)
    {
        _oeeController = oeeController;
    }

    [HttpPost(ApiRoutes.OeeCalculate)]
    public IActionResult Calculate(ParametersRequest request) =>
        Ok(_oeeController.Calculate(request.Parameters));
}
