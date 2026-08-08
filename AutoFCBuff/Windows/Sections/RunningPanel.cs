using System.Numerics;
using AutoFCBuff.Core.Tasks;
using AutoFCBuff.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoFCBuff.Windows.Sections;

internal static class RunningPanel
{
    public static void Draw(AutoFcBuffController ctrl)
    {
        var progress = ctrl.ActiveProgress;
        if (progress == null) return;

        using var cardStyle = Styling.PushCardStyle();
        using var child = ImRaii.Child("RunningPanelChild", new Vector2(0, 0), true);
        if (!child) return;

        Styling.VSpace(10);
        ProgressRing.Draw(progress.StepPercentage, radius: 40f);

        Styling.VSpace(10);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            Styling.TextCentered(progress.StatusMessage, Styling.TextStrong, fontScale: 1.1f);
        }

        Styling.VSpace(8);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentAmber))
        {
            Styling.TextCentered($"Destination: {progress.TargetGcCity}", Styling.AccentAmber);
        }

        Styling.VSpace(8);
        if (progress.TotalToBuy > 0)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            {
                Styling.TextCentered($"Purchased: {progress.PurchasedCount} / {progress.TotalToBuy} Buffs", Styling.TextSecondary);
            }
        }

        Styling.VSpace(20);
        using (ImRaii.PushColor(ImGuiCol.Button, Styling.AccentRose * 0.7f))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, Styling.AccentRose))
        {
            if (ImGui.Button("CANCEL AUTOMATION", new Vector2(240, 38)))
            {
                ctrl.Stop();
            }
        }
    }
}
