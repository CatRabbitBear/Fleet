using Fleet.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fleet.Shared;
public class NotificationService : INotificationService
{
    public event Action<PermissionRequest>? OnPermissionRequested;

    public async Task<bool> RequestPermission(string description)
    {
        var request = new PermissionRequest(description);
        OnPermissionRequested?.Invoke(request);
        return await request.UserDecision.Task;
    }
}
