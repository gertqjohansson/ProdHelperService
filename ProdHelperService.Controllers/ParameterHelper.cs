namespace ProdHelperService.Controllers;

public static class ParameterHelper
{
    // Converts the flat ["key", "value", "key", "value", ...] array used by
    // the client into a lookup dictionary.
    public static Dictionary<string, string> ToDictionary(string[] parameters)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i + 1 < parameters.Length; i += 2)
        {
            result[parameters[i]] = parameters[i + 1];
        }
        return result;
    }
}
