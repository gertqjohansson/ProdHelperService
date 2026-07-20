using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProdHelperService.Contracts;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.Contracts.Service;
using ProdHelperService.ServiceManagement;

namespace ProdHelperService;

[ApiController]
[Authorize(Roles = "Administrator")]
public class ServiceController(IServiceLifecycleManager lifecycleManager, IWindowsServiceInstaller serviceInstaller) : ControllerBase
{
    [HttpPost(ApiRoutes.ServiceGetInfo)]
    public IActionResult GetInfo() => Ok(new GetServiceInfoResponse { Port = lifecycleManager.CurrentPort });

    [HttpPost(ApiRoutes.ServiceUpdatePort)]
    public async Task<IActionResult> UpdatePort(UpdatePortRequest request, CancellationToken cancellationToken)
    {
        PortUpdateResult result = await lifecycleManager.UpdatePortAsync(request.NewPort, cancellationToken);
        if (!result.Success)
        {
            return Conflict(new AuthErrorResponse { Code = result.ErrorCode!, Message = result.ErrorMessage! });
        }

        return Ok(new UpdatePortResponse { NewPort = result.NewPort, SettingsFilePersisted = result.SettingsFilePersisted });
    }

    [HttpPost(ApiRoutes.ServiceGetRegistrationStatus)]
    public IActionResult GetRegistrationStatus()
    {
        ServiceRegistrationState state = serviceInstaller.GetStatus();
        return Ok(new ServiceRegistrationStatusResponse
        {
            IsRegistered = state != ServiceRegistrationState.NotRegistered,
            State = state.ToString(),
        });
    }

    [HttpPost(ApiRoutes.ServiceRegister)]
    public async Task<IActionResult> Register(CancellationToken cancellationToken) =>
        ToActionResult(await serviceInstaller.RegisterAsync(WindowsServiceInstaller.ResolveBinPath(), cancellationToken));

    [HttpPost(ApiRoutes.ServiceUnregister)]
    public async Task<IActionResult> Unregister(CancellationToken cancellationToken) =>
        ToActionResult(await serviceInstaller.UnregisterAsync(cancellationToken));

    [HttpPost(ApiRoutes.ServiceStart)]
    public async Task<IActionResult> Start(CancellationToken cancellationToken) =>
        ToActionResult(await serviceInstaller.StartAsync(cancellationToken));

    [HttpPost(ApiRoutes.ServiceStop)]
    public async Task<IActionResult> Stop(CancellationToken cancellationToken) =>
        ToActionResult(await serviceInstaller.StopAsync(cancellationToken));

    // Anonymous: this is the one action on this controller that must be callable before
    // login, since its entire purpose is answering "is the API reachable at all" - see
    // ProdHelperService.AdminApp's Program.cs (EnsureServiceReachableAsync), which opens
    // ServiceConfigForm when this call fails, before any session exists.
    [AllowAnonymous]
    [HttpPost(ApiRoutes.ServiceIamAlive)]
    public IActionResult IamAlive() => Ok(new ServiceAliveResponse { ServerUtcTime = DateTime.UtcNow });

    private IActionResult ToActionResult(ServiceOperationResult result)
    {
        if (!result.Success)
        {
            return Conflict(new AuthErrorResponse { Code = result.ErrorCode!, Message = result.ErrorMessage! });
        }

        return Ok(new ServiceActionResponse { State = serviceInstaller.GetStatus().ToString() });
    }
}
