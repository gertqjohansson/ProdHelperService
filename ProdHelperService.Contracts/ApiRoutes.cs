namespace ProdHelperService.Contracts;

// Route paths shared between ProdHelperService's API controllers and any
// HTTP caller (e.g. ProdHelperService.AdminApp), so both sides route off the
// same constants instead of duplicating literal strings.
public static class ApiRoutes
{
    public const string OeeCalculate = "Oee/Calculate";
    public const string PlannerGetInteruption = "Planner/GetInteruption";
}
