using System;
using System.Numerics;
using AutoFCBuff.Core.Buffs;
using AutoFCBuff.Core.Game;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace AutoFCBuff.Windows;

public sealed class IconPickerWindow : Window
{
    private int testIconId = 60801;

    public IconPickerWindow() : base("Tester d'Icônes FC##AutoFCBuffIconPicker")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 500),
            MaximumSize = new Vector2(800, 800)
        };
    }

    public override void Draw()
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.TextUnformatted("Testeur d'Icônes FFXIV en Direct");
        }

        ImGui.TextWrapped("Entrez un numéro d'icône ci-dessous pour prévisualiser l'image en direct :");
        Styling.VSpace(6);

        ImGui.SetNextItemWidth(120);
        ImGui.InputInt("ID d'Icône Test", ref testIconId, 1, 10);
        ImGui.SameLine();

        if (testIconId > 0)
        {
            FcIconHelper.DrawIcon((uint)testIconId, new Vector2(32, 32));
        }

        Styling.VSpace(10);
        ImGui.Separator();
        Styling.VSpace(10);

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentAmber))
        {
            ImGui.TextUnformatted("Icônes Actuellement Assignées aux Buffs :");
        }

        Styling.VSpace(6);

        using var scroll = ImRaii.Child("IconListScroll", new Vector2(0, -10), true);
        if (scroll)
        {
            foreach (var buff in FcBuffRegistry.Buffs)
            {
                using var id = ImRaii.PushId((int)buff.Id + 9000);

                FcIconHelper.DrawIcon(buff.IconId, new Vector2(24, 24));
                ImGui.SameLine();

                var gradeColor = buff.Grade == 2 ? Styling.AccentAmber : Styling.AccentTeal;
                using (ImRaii.PushColor(ImGuiCol.Text, gradeColor))
                {
                    ImGui.TextUnformatted($"[G{buff.Grade}] {buff.Name}");
                }

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - 120);
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
                {
                    ImGui.TextUnformatted($"ID: {buff.IconId}");
                }
            }
        }
    }
}
