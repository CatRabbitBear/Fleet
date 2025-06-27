using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fleet.Shared;
public class PermissionRequest
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Description { get; }
    public TaskCompletionSource<bool> UserDecision { get; } = new();

    public PermissionRequest(string description)
    {
        Description = description;
    }
}
