using AutoFCBuff.Core.Ipc;
using clibServices = clib.Services;
using DalamudServices = ECommons.DalamudServices;

namespace AutoFCBuff.Core.Tasks;

public sealed class AutoFcBuffController : IDisposable
{
    private AutoFcBuffTask? activeTask;

    public bool Running => clibServices.Svc.Automation.Running;
    public FcBuffRunProgress? ActiveProgress => activeTask?.Progress;

    public void Start(Configuration config)
    {
        if (Running)
        {
            DalamudServices.Svc.Log.Warning("[AFC] Controller.Start called while task is already running.");
            return;
        }

        activeTask = new AutoFcBuffTask(config);
        clibServices.Svc.Automation.Start(activeTask);
        DalamudServices.Svc.Log.Info("[AFC] FC Buff automation task started.");
    }

    public void Stop()
    {
        if (Running)
        {
            clibServices.Svc.Automation.Stop();
            DalamudServices.Svc.Log.Info("[AFC] FC Buff automation task stopped by user.");
        }
    }

    public bool CheckDependencies(out string missingReason)
    {
        if (!NavmeshIPC.Instance.IsAvailable)
        {
            missingReason = "vnavmesh plugin is not installed or available.";
            return false;
        }

        if (!NavmeshIPC.Instance.IsReady())
        {
            missingReason = "vnavmesh is currently building navigation mesh.";
            return false;
        }

        missingReason = string.Empty;
        return true;
    }

    public void Dispose()
    {
        Stop();
    }
}
