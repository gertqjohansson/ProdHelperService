using ProdHelperService.Controllers.Interface;

namespace ProdHelperService.Controllers.Controller;

// Routes {Controller}/{Function} names to a method on an injected controller
// instance. Add a new callable endpoint by adding a method to a controller
// interface/implementation and one entry to the routes dictionary below.
public class ControllerDispatcher : IControllerDispatcher
{
    private readonly Dictionary<(string Controller, string Function), Func<string[], object>> _routes;

    public ControllerDispatcher(IOeeController oee, IPlannerController planner)
    {
        _routes = new(RouteKeyComparer.Instance)
        {
            [("Oee", "Calculate")] = oee.Calculate,
            [("Planner", "GetInteruption")] = planner.GetInteruption
        };
    }

    public bool TryInvoke(string controller, string function, string[] parameters, out object? result)
    {
        if (_routes.TryGetValue((controller, function), out var handler))
        {
            result = handler(parameters);
            return true;
        }

        result = null;
        return false;
    }

    private sealed class RouteKeyComparer : IEqualityComparer<(string Controller, string Function)>
    {
        public static readonly RouteKeyComparer Instance = new();

        public bool Equals((string Controller, string Function) x, (string Controller, string Function) y) =>
            string.Equals(x.Controller, y.Controller, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Function, y.Function, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Controller, string Function) obj) =>
            HashCode.Combine(
                obj.Controller.ToLowerInvariant(),
                obj.Function.ToLowerInvariant());
    }
}
