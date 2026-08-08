using AutoFCBuff.Core.Ipc;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoFCBuff.Windows.Sections;

internal static class DependencyBanner
{
    public static void Draw(Plugin plugin)
    {
        if (!NavmeshIPC.Instance.IsAvailable)
        {
            using var cardStyle = Styling.PushCardStyle();
            using var color = ImRaii.PushColor(ImGuiCol.ChildBg, Styling.AccentRose * 0.25f);
            using var child = ImRaii.Child("DepBanner", new System.Numerics.Vector2(0, 36), true);
            if (child)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
                {
                    ImGui.TextUnformatted("Warning: vnavmesh plugin is not loaded! Auto-pathing requires vnavmesh.");
                }
                ImGui.SameLine();
                if (ImGui.Button("Details"))
                {
                    plugin.ToggleDependenciesUi();
                }
            }
            Styling.VSpace(6);
        }
    }
}
