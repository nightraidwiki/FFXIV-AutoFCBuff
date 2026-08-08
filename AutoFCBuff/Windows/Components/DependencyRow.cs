using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoFCBuff.Windows.Components;

internal static class DependencyRow
{
    public static void Draw(string name, string statusText, bool isOk, string description)
    {
        var statusColor = isOk ? Styling.AccentMint : Styling.AccentRose;
        var icon = isOk ? "[OK]" : "[MISSING]";

        using (ImRaii.PushColor(ImGuiCol.Text, statusColor))
        {
            ImGui.TextUnformatted($"{icon} {name}");
        }

        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
        {
            ImGui.TextUnformatted($"- {statusText}");
        }

        if (!string.IsNullOrEmpty(description))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            {
                ImGui.TextUnformatted($"   {description}");
            }
        }
    }
}
