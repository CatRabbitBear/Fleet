using Fleet.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fleet.Shared;
public class NotificationService : INotificationService
{
    private PermissionRequest? _currentRequest;

    public event Action<PermissionRequest>? OnPermissionRequested;

    public async Task<bool> RequestPermission(string description)
    {
        if (_currentRequest != null)
        {
            throw new InvalidOperationException("A permission request is already pending.");
        }

        var request = new PermissionRequest(description);
        _currentRequest = request;
        OnPermissionRequested?.Invoke(request);

        bool result = await request.UserDecision.Task;
        _currentRequest = null; // Cleanup
        return result;
    }

    public void ResolvePermission(bool granted)
    {
        _currentRequest?.UserDecision.TrySetResult(granted);
    }
}
