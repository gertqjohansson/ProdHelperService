using System.Net;

namespace ProdHelperService.AdminApp;

public class AuthApiException(string code, string message, HttpStatusCode statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public HttpStatusCode StatusCode { get; } = statusCode;
}
