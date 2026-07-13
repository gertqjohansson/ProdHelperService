namespace ProdHelperService.Controllers.Interface;

public interface IControllerDispatcher
{
    bool TryInvoke(string controller, string function, string[] parameters, out object? result);
}
