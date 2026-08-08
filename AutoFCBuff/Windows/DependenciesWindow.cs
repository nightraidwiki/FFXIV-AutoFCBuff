using System.Numerics;
using AutoFCBuff.Core.Ipc;
using AutoFCBuff.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace AutoFCBuff.Windows;

public sealed class DependenciesWindow : Window, IDisposable
{
    public DependenciesWindow() : base("Auto FC Buffs - Dependencies###AutoFCBuffDeps")
    {
        Size = new Vector2(520, 250);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var style = Styling.PushWindowStyle();

        var navAvailable = NavmeshIPC.Instance.IsAvailable;
        var navReady = NavmeshIPC.Instance.IsReady();

        DependencyRow.Draw(
            "vnavmesh",
            navAvailable ? (navReady ? "Ready" : "Building Navmesh") : "Not Installed / Missing",
            navAvailable && navReady,
            "Provides navigation and pathfinding to Grand Company Quartermasters."
        );

        Styling.VSpace(15);
        ImGui.Separator();
        Styling.VSpace(15);

        if (!navAvailable)
        {
            ImGui.TextWrapped("vnavmesh is required for automated navigation. Please install vnavmesh from your Dalamud plugin repository.");
        }
    }
}
