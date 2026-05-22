using System.Threading;

namespace IdentityHub.ExampleProject.Services;

public class PermissionCheckProbe
{
    private int _permissionCheckCalls;

    public int IncrementPermissionCheckCalls() => Interlocked.Increment(ref _permissionCheckCalls);

    public int PermissionCheckCalls => _permissionCheckCalls;

    public void Reset() => Interlocked.Exchange(ref _permissionCheckCalls, 0);
}
