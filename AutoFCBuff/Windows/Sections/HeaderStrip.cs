using System.Numerics;
using AutoFCBuff.Core;
using AutoFCBuff.Core.Game;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoFCBuff.Windows.Sections;

internal static class HeaderStrip
{
    public static void Draw(Plugin plugin)
    {
        var cfg = plugin.Configuration;
        var gcInfo = GcLocations.ResolveTarget(cfg.SelectedGcChoice);

        var totalCurrentStock = cfg.CurrentStock.Values.Sum();
        var totalPlannedPurchases = cfg.TargetStock.Values.Sum();
        var totalFinalStock = totalCurrentStock + totalPlannedPurchases;

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.TextUnformatted("Auto FC Buffs");
        }

        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentAmber))
        {
            ImGui.TextUnformatted($"[{gcInfo.GcName}]");
        }

        ImGui.SameLine();
        var stockBadgeColor = totalFinalStock >= AfcConstants.MaxFcStockLimit
            ? Styling.AccentRose
            : (totalFinalStock > 0 ? Styling.AccentMint : Styling.TextDim);

        using (ImRaii.PushColor(ImGuiCol.Text, stockBadgeColor))
        {
            var stockText = totalPlannedPurchases > 0
                ? $" (Stock: {totalCurrentStock} +{totalPlannedPurchases} / {AfcConstants.MaxFcStockLimit})"
                : $" (Stock: {totalCurrentStock} / {AfcConstants.MaxFcStockLimit})";

            ImGui.TextUnformatted(stockText);
        }

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 90);

        if (ImGui.Button("Config"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.SameLine();
        if (ImGui.Button("About"))
        {
            plugin.ToggleAboutUi();
        }

        Styling.VSpace(6);
    }
}
