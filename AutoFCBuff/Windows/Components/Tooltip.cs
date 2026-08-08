using Dalamud.Bindings.ImGui;

namespace AutoFCBuff.Windows.Components;

internal static class Tooltip
{
    public static void Show(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(text);
        }
    }
}
