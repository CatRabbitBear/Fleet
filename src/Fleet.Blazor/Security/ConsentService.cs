using Fleet.Shared;
using Fleet.Shared.Interfaces;

namespace Fleet.Blazor.Security;

public interface IConsentService
{
    Task<bool> RequestConsentAsync(ActionDescriptor action, RequestIdentity identity, CancellationToken cancellationToken);
}

public sealed class ConsentService : IConsentService
{
    private readonly INotificationService _notificationService;

    public ConsentService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<bool> RequestConsentAsync(ActionDescriptor action, RequestIdentity identity, CancellationToken cancellationToken)
    {
        var description = $"Approve {action.ActionType} on {action.Resource} from {identity.RequestedBy}?";
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            return await _notificationService.RequestPermission(description).WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
