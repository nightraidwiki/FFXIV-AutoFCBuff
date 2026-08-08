using System;
using System.Linq;
using System.Numerics;
using AutoFCBuff.Core;
using AutoFCBuff.Core.Buffs;
using AutoFCBuff.Core.Game;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoFCBuff.Windows.Components;

internal static class FcBuffCard
{
    public static void Draw(FcBuffDefinition buff, Configuration config, int totalCurrentStock, int totalPlannedPurchases)
    {
        var buyQty = config.TargetStock.GetValueOrDefault(buff.Id, 0);
        var currentStock = config.CurrentStock.GetValueOrDefault(buff.Id, 0);

        // Maximum additional purchases allowed globally across all buffs combined (Limit = 15 total in FC)
        var maxGlobalAllowedPurchases = Math.Max(0, AfcConstants.MaxFcStockLimit - totalCurrentStock);
        var remainingPurchaseBudget = Math.Max(0, maxGlobalAllowedPurchases - (totalPlannedPurchases - buyQty));
        var canAddMore = (totalCurrentStock + totalPlannedPurchases) < AfcConstants.MaxFcStockLimit;

        using var id = ImRaii.PushId((int)buff.Id);
        using var cardStyle = Styling.PushCardStyle();
        using var pad = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(8, 6));
        using var space = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(4, 2));

        using var child = ImRaii.Child($"BuffCard_{buff.Id}", new Vector2(0, 108), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!child) return;

        // Line 1: Icon (18x24, 30:40 aspect ratio) + Grade + Name + Category
        FcIconHelper.DrawIcon(buff.IconId, new Vector2(18, 24));
        ImGui.SameLine(0, 6);

        var gradeColor = buff.Grade == 2 ? Styling.AccentAmber : Styling.AccentTeal;
        using (ImRaii.PushColor(ImGuiCol.Text, gradeColor))
        {
            ImGui.TextUnformatted($"[G{buff.Grade}]");
        }

        ImGui.SameLine(0, 4);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.TextUnformatted(buff.Name);
        }

        ImGui.SameLine(0, 6);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.CategoryColor(buff.Category)))
        {
            ImGui.TextUnformatted($"({buff.Category})");
        }

        // Line 2: Cost & Stock Info
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
        {
            var stockStr = currentStock > 0 ? $"En Stock: {currentStock}/15" : "En Stock: 0";
            var buyStr = buyQty > 0 ? $" | Achat: +{buyQty}" : "";
            ImGui.TextUnformatted($"{buff.CreditCost:N0} Cr | {stockStr}{buyStr}");
        }

        Styling.VSpace(2);

        // Line 3: Stepper Controls (Achat Quantity)
        ImGui.TextUnformatted("Achat:");
        ImGui.SameLine(0, 6);

        if (ImGui.Button("-##Dec", new Vector2(22, 20)) && buyQty > 0)
        {
            config.TargetStock[buff.Id] = buyQty - 1;
            config.SaveDebounced();
        }

        ImGui.SameLine(0, 3);
        ImGui.SetNextItemWidth(34);
        var inputVal = buyQty;
        if (ImGui.InputInt("##Input", ref inputVal, 0, 0))
        {
            config.TargetStock[buff.Id] = Math.Clamp(inputVal, 0, remainingPurchaseBudget);
            config.SaveDebounced();
        }

        ImGui.SameLine(0, 3);
        using (var dis = ImRaii.Disabled(!canAddMore))
        {
            if (ImGui.Button("+##Inc", new Vector2(22, 20)) && canAddMore)
            {
                config.TargetStock[buff.Id] = buyQty + 1;
                config.SaveDebounced();
            }
        }

        ImGui.SameLine(0, 4);
        if (ImGui.Button("0##Zero", new Vector2(22, 20)))
        {
            config.TargetStock[buff.Id] = 0;
            config.SaveDebounced();
        }

        ImGui.SameLine(0, 4);
        using (var dis = ImRaii.Disabled(remainingPurchaseBudget <= 0))
        {
            if (ImGui.Button("Max##MaxBtn", new Vector2(34, 20)))
            {
                config.TargetStock[buff.Id] = remainingPurchaseBudget;
                config.SaveDebounced();
            }
        }
    }
}
