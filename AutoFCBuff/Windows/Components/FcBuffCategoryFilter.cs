using AutoFCBuff.Core.Buffs;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoFCBuff.Windows.Components;

internal static class FcBuffCategoryFilter
{
    public static FcBuffCategory Draw(FcBuffCategory currentCategory)
    {
        var selected = currentCategory;
        var categories = new (FcBuffCategory Category, string Label)[]
        {
            (FcBuffCategory.All, "All Buffs"),
            (FcBuffCategory.Combat, "Combat"),
            (FcBuffCategory.CraftingGathering, "Crafting & Gathering"),
            (FcBuffCategory.Utility, "Utility"),
        };

        foreach (var (cat, label) in categories)
        {
            var isSelected = cat == currentCategory;
            using var color = isSelected
                ? Styling.PushAccentButtonColors()
                : ImRaii.PushColor(ImGuiCol.Button, Styling.CardBg);

            if (ImGui.Button(label))
            {
                selected = cat;
            }
            ImGui.SameLine();
        }
        ImGui.NewLine();

        return selected;
    }
}
