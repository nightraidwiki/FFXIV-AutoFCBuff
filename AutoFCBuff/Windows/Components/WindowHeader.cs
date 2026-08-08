using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoFCBuff.Windows.Components;

internal static class WindowHeader
{
    public static void Draw(string title, string subtitle)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.TextUnformatted(title);
        }
        if (!string.IsNullOrEmpty(subtitle))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            {
                ImGui.TextUnformatted(subtitle);
            }
        }
    }
}
