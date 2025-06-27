using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fleet.Shared.Interfaces;
public interface INotificationService
{
    event Action<PermissionRequest> OnPermissionRequested;

    Task<bool> RequestPermission(string description);
}
