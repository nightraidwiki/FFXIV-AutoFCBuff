using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AutoFCBuff.Core.Buffs;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoFCBuff.Core.Game;

public static class FcStockReader
{
    public static async Task<(Dictionary<uint, int> Stock, List<string> ActiveBuffs)> ScanStockAsync(int maxWaitMs = 3500)
    {
        var stock = new Dictionary<uint, int>();
        var activeBuffs = GetActiveFcBuffsFromPlayerStatus();

        if (AddonInteractions.IsFcShopOpen())
        {
            stock = AddonInteractions.ReadLiveFcStock();
            return (stock, activeBuffs);
        }

        if (IsFcActionAddonVisible())
        {
            stock = ReadStockFromFcActionAddon();
            if (activeBuffs.Count == 0)
            {
                var addonActive = ReadActiveBuffsFromFcActionAddon();
                if (addonActive.Count > 0) activeBuffs = addonActive;
            }
            return (stock, activeBuffs);
        }

        await Task.Delay(50);
        return (stock, activeBuffs);
    }

    private static unsafe bool IsFcActionAddonVisible()
    {
        var fcActionAddon = AddonInteractions.GetAddon("FreeCompanyAction");
        return fcActionAddon != null && fcActionAddon->IsVisible;
    }

    private static unsafe void CloseFcWindowsSafe()
    {
        AddonInteractions.CloseAddon("FreeCompanyAction");
        AddonInteractions.CloseAddon("FreeCompany");
        AddonInteractions.CloseAddon("FreeCompanyInfo");
    }

    public static List<string> GetActiveFcBuffsFromPlayerStatus()
    {
        var activeList = new List<string>();
        try
        {
            if (Player.Available && Player.Object != null && Player.Object.StatusList != null)
            {
                foreach (var status in Player.Object.StatusList)
                {
                    if (status.StatusId == 0) continue;
                    var gameData = status.GameData.ValueNullable;
                    if (!gameData.HasValue) continue;

                    var name = gameData.Value.Name.ExtractText();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var matchedBuff = FcBuffRegistry.Buffs.FirstOrDefault(b => 
                        name.Equals(b.Name, StringComparison.OrdinalIgnoreCase) ||
                        ("The " + b.Name).Equals(name, StringComparison.OrdinalIgnoreCase) ||
                        b.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                        name.Contains(b.Name, StringComparison.OrdinalIgnoreCase));

                    if (matchedBuff != null)
                    {
                        activeList.Add(matchedBuff.Name);
                        Svc.Log.Info($"[AFC ActiveBuffs] Detected active FC status effect on player: '{name}' -> Matched '{matchedBuff.Name}'");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "[AFC ActiveBuffs] Exception scanning player status list");
        }

        return activeList.Distinct().ToList();
    }

    public static unsafe List<string> ReadActiveBuffsFromFcActionAddon()
    {
        var activeBuffs = new List<string>();
        var fcActionAddon = AddonInteractions.GetAddon("FreeCompanyAction");
        if (fcActionAddon == null || !fcActionAddon->IsVisible)
        {
            return activeBuffs;
        }

        try
        {
            var uldManager = &fcActionAddon->UldManager;
            if (uldManager != null && uldManager->NodeListCount > 0)
            {
                for (int i = 0; i < uldManager->NodeListCount; i++)
                {
                    var node = uldManager->NodeList[i];
                    if (node == null || !node->IsVisible()) continue;

                    // Only check Node 12 or active buff headers
                    if (node->NodeId == 12)
                    {
                        ScanNodeTreeForActiveBuffNames(node, activeBuffs);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "[AFC FcActionParser] Exception scanning active FC buff nodes");
        }

        return activeBuffs.Distinct().ToList();
    }

    private static unsafe void ScanNodeTreeForActiveBuffNames(AtkResNode* node, List<string> activeBuffs)
    {
        if (node == null || !node->IsVisible()) return;

        if (node->Type == NodeType.Text)
        {
            var txtNode = (AtkTextNode*)node;
            string text = txtNode->NodeText.ToString();
            var buff = MapTextToBuff(text);
            if (buff != null)
            {
                activeBuffs.Add(buff.Name);
            }
        }

        ushort typeVal = (ushort)node->Type;
        if (typeVal >= 1000)
        {
            var compNode = (AtkComponentNode*)node;
            if (compNode->Component != null)
            {
                var subUld = &compNode->Component->UldManager;
                if (subUld != null && subUld->NodeListCount > 0)
                {
                    for (int j = 0; j < subUld->NodeListCount; j++)
                    {
                        ScanNodeTreeForActiveBuffNames(subUld->NodeList[j], activeBuffs);
                    }
                }
            }
        }
    }

    public static unsafe int GetReserveStockCountFromText()
    {
        var fcActionAddon = AddonInteractions.GetAddon("FreeCompanyAction");
        if (fcActionAddon != null && fcActionAddon->IsVisible)
        {
            var uld = &fcActionAddon->UldManager;
            if (uld != null && uld->NodeListCount > 0)
            {
                for (int i = 0; i < uld->NodeListCount; i++)
                {
                    var node = uld->NodeList[i];
                    if (node != null && node->Type == NodeType.Text && node->IsVisible())
                    {
                        var txtNode = (AtkTextNode*)node;
                        string text = txtNode->NodeText.ToString();
                        if (text.Contains("/15"))
                        {
                            var parts = text.Split('/');
                            if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out var count))
                            {
                                return count;
                            }
                        }
                    }
                }
            }
        }
        return -1;
    }

    public static Dictionary<uint, int> GetLiveFcStockFromMemory()
    {
        var stock = ReadStockFromFcActionAddon();
        if (stock.Count > 0)
        {
            return stock;
        }

        var vendorStock = AddonInteractions.ReadLiveFcStock();
        if (vendorStock.Count > 0)
        {
            return vendorStock;
        }

        return stock;
    }

    public static unsafe Dictionary<uint, int> ReadStockFromFcActionAddon()
    {
        var stock = new Dictionary<uint, int>();

        var fcActionAddon = AddonInteractions.GetAddon("FreeCompanyAction");
        if (fcActionAddon == null || !fcActionAddon->IsVisible)
        {
            return stock;
        }

        try
        {
            var uldManager = &fcActionAddon->UldManager;
            if (uldManager != null && uldManager->NodeListCount > 0)
            {
                for (int i = 0; i < uldManager->NodeListCount; i++)
                {
                    var node = uldManager->NodeList[i];
                    if (node == null) continue;

                    // Node 14 (Type 1006) is the Inactive Actions Component
                    if (node->NodeId == 14)
                    {
                        ScanInactiveActionsContainer(node, stock);
                    }
                }
            }

            var totalDetected = stock.Values.Sum();
            var textCount = GetReserveStockCountFromText();
            Svc.Log.Info($"[AFC FcActionParser] FreeCompanyAction reserve scanned cleanly! Detected = {totalDetected}/15 (TextCounter={textCount}/15)");
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "[AFC FcActionParser] Exception scanning FreeCompanyAction nodes");
        }

        return stock;
    }

    private static unsafe void ScanInactiveActionsContainer(AtkResNode* node, Dictionary<uint, int> stock)
    {
        if (node == null || !node->IsVisible()) return;

        ushort typeVal = (ushort)node->Type;
        if (typeVal >= 1000)
        {
            var compNode = (AtkComponentNode*)node;
            if (compNode->Component != null)
            {
                var subUld = &compNode->Component->UldManager;
                if (subUld != null && subUld->NodeListCount > 0)
                {
                    for (int j = 0; j < subUld->NodeListCount; j++)
                    {
                        var slotNode = subUld->NodeList[j];
                        if (slotNode == null || !slotNode->IsVisible()) continue;

                        ushort slotType = (ushort)slotNode->Type;
                        if (slotType == 1008)
                        {
                            var slotComp = (AtkComponentNode*)slotNode;
                            if (slotComp->Component != null)
                            {
                                var itemUld = &slotComp->Component->UldManager;
                                if (itemUld != null && itemUld->NodeListCount > 0)
                                {
                                    for (int k = 0; k < itemUld->NodeListCount; k++)
                                    {
                                        var subItemNode = itemUld->NodeList[k];
                                        if (subItemNode == null || !subItemNode->IsVisible()) continue;

                                        if (subItemNode->Type == NodeType.Text && subItemNode->NodeId == 3)
                                        {
                                            var txtNode = (AtkTextNode*)subItemNode;
                                            string buffName = txtNode->NodeText.ToString();
                                            if (!string.IsNullOrWhiteSpace(buffName))
                                            {
                                                var buff = MapTextToBuff(buffName);
                                                if (buff != null)
                                                {
                                                    stock[buff.Id] = stock.GetValueOrDefault(buff.Id, 0) + 1;
                                                    Svc.Log.Info($"[AFC StockScanner] Detected FC Reserve Stock Item: '{buff.Name}' (Id={buff.Id}, RawText='{buffName}')");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static FcBuffDefinition? MapTextToBuff(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return null;

        var cleaned = rawText.Trim();

        // 1. Exact match
        var match = FcBuffRegistry.Buffs.FirstOrDefault(b => b.Name.Equals(cleaned, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match;

        // 2. Remove "The " prefix
        if (cleaned.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
        {
            var withoutThe = cleaned.Substring(4).Trim();
            match = FcBuffRegistry.Buffs.FirstOrDefault(b => b.Name.Equals(withoutThe, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        // 3. Grade I missing roman numeral
        var grade1Name = cleaned + " I";
        match = FcBuffRegistry.Buffs.FirstOrDefault(b => b.Name.Equals(grade1Name, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match;

        if (cleaned.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
        {
            var grade1WithoutThe = cleaned.Substring(4).Trim() + " I";
            match = FcBuffRegistry.Buffs.FirstOrDefault(b => b.Name.Equals(grade1WithoutThe, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        return FcBuffRegistry.Buffs.FirstOrDefault(b => cleaned.Contains(b.Name, StringComparison.OrdinalIgnoreCase) || b.Name.Contains(cleaned, StringComparison.OrdinalIgnoreCase));
    }

    public static unsafe void LogMemoryInfo()
    {
        Svc.Chat.Print("=== [AFC Reserve Stock Inspector] ===");
        try
        {
            var textCounter = GetReserveStockCountFromText();
            Svc.Chat.Print($"Text Counter 'X/15' Detected = {textCounter}/15");

            var stock = ReadStockFromFcActionAddon();
            int total = stock.Values.Sum();
            Svc.Chat.Print($"FreeCompanyAction Reserve Stock Detected: {total}/15");

            foreach (var (buffId, qty) in stock)
            {
                var buff = FcBuffRegistry.GetById(buffId);
                Svc.Chat.Print($"  -> {buff?.Name} (x{qty})");
            }
        }
        catch (Exception ex)
        {
            Svc.Chat.Print($"Memory inspection error: {ex.Message}");
        }
    }
}
