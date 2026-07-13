using ProdHelperService.Controllers.Interface;

namespace ProdHelperService.Controllers.Controller;

public class PlannerController : IPlannerController
{
    // Handles {hcRoot}/Planner/GetInteruption
    public object GetInteruption(string[] parameters)
    {
        var p = ParameterHelper.ToDictionary(parameters);

        // TODO: replace with real planner logic.
        return new
        {
            controller = "Planner",
            function = "GetInteruption",
            id = p.GetValueOrDefault("id"),
            start = p.GetValueOrDefault("Start"),
            end = p.GetValueOrDefault("end"),
            includeBreaks = p.GetValueOrDefault("break"),
            result = "not implemented yet"
        };
    }
}
