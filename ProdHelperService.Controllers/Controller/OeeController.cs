using ProdHelperService.Controllers.Interface;

namespace ProdHelperService.Controllers.Controller;

public class OeeController : IOeeController
{
    // Handles {hcRoot}/Oee/Calculate
    public object Calculate(string[] parameters)
    {
        var p = ParameterHelper.ToDictionary(parameters);

        // TODO: replace with real OEE calculation logic.
        return new
        {
            controller = "Oee",
            function = "Calculate",
            id = p.GetValueOrDefault("id"),
            start = p.GetValueOrDefault("Start"),
            end = p.GetValueOrDefault("end"),
            result = "not implemented yet"
        };
    }
}
