using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AutoFCBuff.Core;
using AutoFCBuff.Core.Buffs;
using AutoFCBuff.Core.Game;
using AutoFCBuff.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoFCBuff.Windows.Sections;

internal static class SetupPanel
{
    private static FcBuffCategory selectedCategory = FcBuffCategory.All;

    public static void Draw(Configuration config, bool isRunning, Action onStartRun)
    {
        var totalCurrentStock = config.CurrentStock.Values.Sum();
        var totalPlannedPurchases = config.TargetStock.Values.Sum();
        var totalFinalStock = totalCurrentStock + totalPlannedPurchases;

        var targetStockRatio = Math.Clamp((float)totalFinalStock / AfcConstants.MaxFcStockLimit, 0f, 1f);
        var targetGaugeColor = totalFinalStock >= AfcConstants.MaxFcStockLimit ? Styling.AccentRose : Styling.AccentTeal;

        // Existing Stock in FC
        var stockItems = config.CurrentStock.Where(kv => kv.Value > 0)
            .Select(kv => (Buff: FcBuffRegistry.GetById(kv.Key), Count: kv.Value))
            .Where(x => x.Buff != null)
            .ToList();

        // Calculate dynamic hero card height based on 3 items per row + active buffs row
        int stockRows = Math.Max(1, (int)Math.Ceiling(stockItems.Count / 3.0));
        float activeBuffsExtraHeight = config.ActiveFcBuffNames.Count > 0 ? 22f : 0f;
        float heroHeight = 85 + (stockRows * 26) + activeBuffsExtraHeight + 6;

        // Hero Card: FC Stock & Active Buffs Overview
        using (Styling.PushCardStyle())
        using (var heroChild = ImRaii.Child("HeroSummary", new Vector2(0, heroHeight), true, ImGuiWindowFlags.NoScrollbar))
        {
            if (heroChild)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
                {
                    ImGui.TextUnformatted("Allocation des Buffs FC en Stock");
                }

                ImGui.SameLine();
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
                {
                    ImGui.TextUnformatted($"(Capacité Max: {AfcConstants.MaxFcStockLimit})");
                }

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - 130);
                using (ImRaii.PushColor(ImGuiCol.Text, totalFinalStock >= AfcConstants.MaxFcStockLimit ? Styling.AccentRose : Styling.AccentAmber))
                {
                    ImGui.TextUnformatted($"Total Final: {totalFinalStock} / {AfcConstants.MaxFcStockLimit}");
                }

                Styling.VSpace(4);

                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
                {
                    ImGui.TextUnformatted("En Stock FC: ");
                }
                ImGui.SameLine();
                using (ImRaii.PushColor(ImGuiCol.Text, totalCurrentStock > 0 ? Styling.AccentMint : Styling.TextDim))
                {
                    ImGui.TextUnformatted($"{totalCurrentStock} / {AfcConstants.MaxFcStockLimit}");
                }

                if (totalPlannedPurchases > 0)
                {
                    ImGui.SameLine();
                    using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentTeal))
                    {
                        ImGui.TextUnformatted($"(Achats Prévus: +{totalPlannedPurchases})");
                    }
                }

                ImGui.SameLine();
                if (ImGui.SmallButton("Scan Stock"))
                {
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        var (liveMemoryStock, activeBuffs) = await FcStockReader.ScanStockAsync();
                        config.CurrentStock.Clear();
                        foreach (var (buffId, qty) in liveMemoryStock)
                        {
                            config.CurrentStock[buffId] = qty;
                        }
                        config.ActiveFcBuffNames = activeBuffs;
                        config.SaveDebounced();
                    });
                }

                ImGui.SameLine();
                if (ImGui.SmallButton("Réinitialiser"))
                {
                    config.CurrentStock.Clear();
                    config.ActiveFcBuffNames.Clear();
                    config.SaveDebounced();
                }

                // Display Active FC Buffs if detected
                if (config.ActiveFcBuffNames.Count > 0)
                {
                    Styling.VSpace(2);
                    using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentAmber))
                    {
                        ImGui.TextUnformatted("Buffs Actifs: ");
                    }
                    ImGui.SameLine();
                    using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
                    {
                        ImGui.TextUnformatted(string.Join(" | ", config.ActiveFcBuffNames));
                    }
                }

                Styling.VSpace(4);

                if (stockItems.Count == 0)
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
                    {
                        ImGui.TextUnformatted("Aucun buff FC détecté en stock.");
                    }
                }
                else
                {
                    if (ImGui.BeginTable("StockItemsGrid3Col", 3, ImGuiTableFlags.SizingStretchSame))
                    {
                        for (int i = 0; i < stockItems.Count; i++)
                        {
                            if (i % 3 == 0) ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(i % 3);

                            var (buff, count) = stockItems[i];
                            if (buff == null) continue;
                            using var buffIdScope = ImRaii.PushId((int)buff.Id + 5000);

                            FcIconHelper.DrawIcon(buff.IconId, new Vector2(15, 20));
                            ImGui.SameLine(0, 4);

                            var gradeColor = buff.Grade == 2 ? Styling.AccentAmber : Styling.AccentTeal;
                            using (ImRaii.PushColor(ImGuiCol.Text, gradeColor))
                            {
                                ImGui.TextUnformatted($"{buff.Name}");
                            }

                            ImGui.SameLine(0, 4);
                            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
                            {
                                ImGui.TextUnformatted($"x{count}");
                            }
                        }

                        ImGui.EndTable();
                    }
                }

                Styling.VSpace(4);

                // Progress Bar for Target Stock
                using (ImRaii.PushColor(ImGuiCol.PlotHistogram, targetGaugeColor))
                {
                    var barText = totalFinalStock >= AfcConstants.MaxFcStockLimit ? $"Stock Plein: {totalFinalStock} / {AfcConstants.MaxFcStockLimit} (Stock: {totalCurrentStock} + Achats: +{totalPlannedPurchases})" : $"Achats: +{totalPlannedPurchases} | Total Final: {totalFinalStock} / {AfcConstants.MaxFcStockLimit}";
                    ImGui.ProgressBar(targetStockRatio, new Vector2(-1, 14), barText);
                }
            }
        }

        Styling.VSpace(6);

        // Controls Row: GC Selection + Auto-Activation Toggle + Target Active Buff 1 + Target Active Buff 2
        ImGui.TextUnformatted("QG :");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(90);
        var currentGc = config.SelectedGcChoice;
        if (ImGui.BeginCombo("##GcChoiceCombo", currentGc.ToString()))
        {
            foreach (GcChoice choice in Enum.GetValues(typeof(GcChoice)))
            {
                if (ImGui.Selectable(choice.ToString(), choice == currentGc))
                {
                    config.SelectedGcChoice = choice;
                    config.SaveDebounced();
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine(0, 8);
        var enableAuto = config.EnableAutoActivation;
        if (ImGui.Checkbox("Auto-Activer Buffs##EnableAutoAct", ref enableAuto))
        {
            config.EnableAutoActivation = enableAuto;
            config.SaveDebounced();
        }

        if (config.EnableAutoActivation)
        {
            ImGui.SameLine(0, 6);
            ImGui.TextUnformatted("Actif 1 :");
            ImGui.SameLine(0, 3);
            ImGui.SetNextItemWidth(110);
            DrawActiveBuffCombo("##TargetActiveBuff1", config.TargetActiveBuff1, selectedId =>
            {
                config.TargetActiveBuff1 = selectedId;
                config.SaveDebounced();
            });

            ImGui.SameLine(0, 6);
            ImGui.TextUnformatted("Actif 2 :");
            ImGui.SameLine(0, 3);
            ImGui.SetNextItemWidth(110);
            DrawActiveBuffCombo("##TargetActiveBuff2", config.TargetActiveBuff2, selectedId =>
            {
                config.TargetActiveBuff2 = selectedId;
                config.SaveDebounced();
            });
        }

        // Grade Toggle Button
        ImGui.SameLine(0, 8);
        var gradeToggleText = config.ShowGrade1Buffs ? "G1 & G2" : "G2";
        var gradeToggleColor = config.ShowGrade1Buffs ? Styling.AccentTeal : Styling.AccentAmber;
        using (ImRaii.PushColor(ImGuiCol.Button, gradeToggleColor * 0.25f))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, gradeToggleColor * 0.45f))
        using (ImRaii.PushColor(ImGuiCol.Text, gradeToggleColor))
        {
            if (ImGui.Button(gradeToggleText))
            {
                config.ShowGrade1Buffs = !config.ShowGrade1Buffs;
                config.SaveDebounced();
            }
        }

        // Category Filter Buttons
        Styling.VSpace(4);
        selectedCategory = FcBuffCategoryFilter.Draw(selectedCategory);

        Styling.VSpace(6);

        // Filter Buff List based on Category and Grade Filter (Grade 2 default)
        var filteredBuffs = FcBuffRegistry.Buffs
            .Where(b => config.ShowGrade1Buffs || b.Grade == 2)
            .Where(b => selectedCategory == FcBuffCategory.All || b.Category == selectedCategory)
            .ToList();

        // 2-Column Grid Layout for Buff Cards
        using (var scrollChild = ImRaii.Child("BuffListScroll", new Vector2(0, -50), true))
        {
            if (scrollChild)
            {
                if (ImGui.BeginTable("FcBuffGrid", 2, ImGuiTableFlags.SizingStretchSame))
                {
                    ImGui.TableSetupColumn("Col1", ImGuiTableColumnFlags.WidthStretch, 0.5f);
                    ImGui.TableSetupColumn("Col2", ImGuiTableColumnFlags.WidthStretch, 0.5f);

                    for (int i = 0; i < filteredBuffs.Count; i++)
                    {
                        if (i % 2 == 0)
                        {
                            ImGui.TableNextRow();
                        }
                        ImGui.TableSetColumnIndex(i % 2);

                        var buff = filteredBuffs[i];
                        FcBuffCard.Draw(buff, config, totalCurrentStock, totalPlannedPurchases);
                    }

                    ImGui.EndTable();
                }
            }
        }

        Styling.VSpace(6);

        // Bottom Action Bar: Run Button
        var canStart = totalPlannedPurchases > 0 && !isRunning;
        using var btnDisable = ImRaii.Disabled(!canStart);
        using var btnColor = ImRaii.PushColor(ImGuiCol.Button, canStart ? Styling.AccentTeal : Styling.CardBg);
        using var btnHover = ImRaii.PushColor(ImGuiCol.ButtonHovered, Styling.AccentTeal);

        if (ImGui.Button("LANCER L'ACHAT DES BUFFS FC", new Vector2(-1, 38)))
        {
            onStartRun();
        }
    }

    private static void DrawActiveBuffCombo(string comboId, uint currentSelectedId, Action<uint> onSelect)
    {
        var currentBuff = FcBuffRegistry.GetById(currentSelectedId);
        var previewText = currentBuff != null ? currentBuff.Name : "Aucun";

        if (ImGui.BeginCombo(comboId, previewText))
        {
            if (ImGui.Selectable("Aucun", currentSelectedId == 0))
            {
                onSelect(0);
            }

            foreach (var buff in FcBuffRegistry.Buffs)
            {
                if (ImGui.Selectable($"{buff.Name} (G{buff.Grade})", buff.Id == currentSelectedId))
                {
                    onSelect(buff.Id);
                }
            }

            ImGui.EndCombo();
        }
    }
}
