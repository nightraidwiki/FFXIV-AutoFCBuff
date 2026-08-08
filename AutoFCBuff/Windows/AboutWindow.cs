using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace AutoFCBuff.Windows;

public sealed class AboutWindow : Window, IDisposable
{
    public AboutWindow() : base("About Auto FC Buffs###AutoFCBuffAbout")
    {
        Size = new Vector2(450, 300);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var style = Styling.PushWindowStyle();

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.TextUnformatted("Auto FC Buffs v1.0.0.0");
        }

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
        {
            ImGui.TextUnformatted("Created by Gemxu");
            ImGui.TextUnformatted("Automated Free Company Buff (Company Actions) purchasing for FFXIV.");
        }

        Styling.VSpace(10);
        ImGui.Separator();
        Styling.VSpace(10);

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
        {
            ImGui.TextUnformatted("Features:");
            ImGui.TextUnformatted("- Auto-teleport to Gridania, Limsa Lominsa, or Ul'dah Grand Company HQ.");
            ImGui.TextUnformatted("- Pathfinding via vnavmesh to Grand Company Quartermasters.");
            ImGui.TextUnformatted("- Automatic checking & purchasing of missing FC buffs subject to 15-stock cap.");
            ImGui.TextUnformatted("- Modern dark UI design matching DailyTribes.");
        }
    }
}
