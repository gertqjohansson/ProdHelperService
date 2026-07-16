using Microsoft.AspNetCore.Mvc;
using ProdHelperService.Contracts;
using ProdHelperService.Controllers.Interface;

namespace ProdHelperService.Api;

[ApiController]
public class PlannerApiController : ControllerBase
{
    private readonly IPlannerController _plannerController;

    public PlannerApiController(IPlannerController plannerController)
    {
        _plannerController = plannerController;
    }

    [HttpPost(ApiRoutes.PlannerGetInteruption)]
    public IActionResult GetInteruption(ParametersRequest request) =>
        Ok(_plannerController.GetInteruption(request.Parameters));
}
