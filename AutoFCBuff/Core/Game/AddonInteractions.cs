using System;
using System.Collections.Generic;
using System.Linq;
using AutoFCBuff.Core.Buffs;
using ECommons.Automation;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoFCBuff.Core.Game;

public static unsafe class AddonInteractions
{
    public static AtkUnitBase* GetAddon(string name)
    {
        try
        {
            var manager = RaptureAtkUnitManager.Instance();
            return manager != null ? manager->GetAddonByName(name) : null;
        }
        catch
        {
            return null;
        }
    }

    public static bool InteractWithNpc(Dalamud.Game.ClientState.Objects.Types.IGameObject npc)
    {
        if (npc == null) return false;
        try
        {
            ECommons.DalamudServices.Svc.Targets.Target = npc;
            var targetSystem = TargetSystem.Instance();
            if (targetSystem == null) return false;

            var result = targetSystem->InteractWithObject((GameObject*)npc.Address, false);
            return result != 7 && result > 0;
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, "[AFC] Exception during InteractWithNpc");
            return false;
        }
    }

    public static bool IsTalkOpen()
    {
        var talk = GetAddon("Talk");
        return talk != null && talk->IsVisible;
    }

    public static void AdvanceTalk()
    {
        try
        {
            var talk = GetAddon("Talk");
            if (talk != null && talk->IsVisible)
            {
                Callback.Fire(talk, true, 0);
            }
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, "[AFC Dialog] Exception advancing Talk speech bubble");
        }
    }

    public static bool IsDialogWindowOpen()
    {
        var selectString = GetAddon("SelectString");
        if (selectString != null && selectString->IsVisible) return true;

        var selectIconString = GetAddon("SelectIconString");
        if (selectIconString != null && selectIconString->IsVisible) return true;

        return IsTalkOpen();
    }

    public static bool IsFcActionWindowOpen()
    {
        var addon = GetAddon("FreeCompanyAction");
        return addon != null && addon->IsVisible;
    }

    public static bool IsSelectYesnoOpen()
    {
        var selectYesno = GetAddon("SelectYesno");
        return selectYesno != null && selectYesno->IsVisible;
    }

    public static bool IsFcShopOpen()
    {
        var fcExchange = GetAddon("FreeCompanyExchange");
        if (fcExchange != null && fcExchange->IsVisible) return true;

        var fcShop = GetAddon("FreeCompanyCreditShop");
        if (fcShop != null && fcShop->IsVisible) return true;

        var gcExchange = GetAddon("GrandCompanyExchange");
        return gcExchange != null && gcExchange->IsVisible;
    }

    public static int GetFcCreditShopDialogIndex()
    {
        try
        {
            var selectString = GetAddon("SelectString");
            if (selectString != null && selectString->IsVisible)
            {
                var master = new AddonMaster.SelectString((nint)selectString);
                if (master.Entries != null)
                {
                    foreach (var entry in master.Entries)
                    {
                        var text = entry.Text;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            if (text.Contains("Exchange", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("Échanger", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("Credit", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("Crédit", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("Shop", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("Boutique", StringComparison.OrdinalIgnoreCase))
                            {
                                ECommons.DalamudServices.Svc.Log.Info($"[AFC Dialog] Detected credit shop entry '{text}' at index {entry.Index}");
                                return entry.Index;
                            }
                        }
                    }
                }
                return 1; // Default to 1 (Exchange credits) because 0 is Execute Action
            }

            var selectIconString = GetAddon("SelectIconString");
            if (selectIconString != null && selectIconString->IsVisible)
            {
                var master = new AddonMaster.SelectIconString((nint)selectIconString);
                if (master.Entries != null)
                {
                    foreach (var entry in master.Entries)
                    {
                        var text = entry.Text;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            if (text.Contains("Exchange", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("Échanger", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("Credit", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("Crédit", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("Shop", StringComparison.OrdinalIgnoreCase) ||
                                text.Contains("Boutique", StringComparison.OrdinalIgnoreCase))
                            {
                                ECommons.DalamudServices.Svc.Log.Info($"[AFC Dialog] Detected credit shop icon entry '{text}' at index {entry.Index}");
                                return entry.Index;
                            }
                        }
                    }
                }
                return 1;
            }
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, "[AFC Dialog] Exception finding credit shop dialog index. Defaulting to 1.");
        }

        return 1; // Default to index 1
    }

    public static Dictionary<uint, int> ReadLiveFcStock()
    {
        var result = new Dictionary<uint, int>();

        try
        {
            var fcShop = GetAddon("FreeCompanyCreditShop");
            if (fcShop == null || !fcShop->IsVisible)
            {
                fcShop = GetAddon("FreeCompanyExchange");
            }

            if (fcShop != null && fcShop->IsVisible && fcShop->UldManager.NodeListCount > 0)
            {
                var shopMaster = new AddonMaster.FreeCompanyCreditShop((nint)fcShop);
                if (shopMaster.Items != null)
                {
                    foreach (var item in shopMaster.Items)
                    {
                        if (item.ItemId > 0)
                        {
                            var buff = FcBuffRegistry.Buffs.FirstOrDefault(b => b.ItemId == item.ItemId);
                            if (buff != null)
                            {
                                result[buff.Id] = Math.Max(0, item.QuantityInInventory);
                                ECommons.DalamudServices.Svc.Log.Info($"[AFC LiveStock] {buff.Name} (ItemId={buff.ItemId}) Owned in FC = {item.QuantityInInventory}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, "[AFC LiveStock] Exception safely caught while reading vendor stock.");
        }

        return result;
    }

    public static void ClickDialogEntry(int index)
    {
        try
        {
            var selectString = GetAddon("SelectString");
            if (selectString != null && selectString->IsVisible)
            {
                var master = new AddonMaster.SelectString((nint)selectString);
                if (master.Entries != null && index >= 0 && index < master.Entries.Length)
                {
                    ECommons.DalamudServices.Svc.Log.Info($"[AFC Dialog] Selecting SelectString entry index {index} ('{master.Entries[index].Text}')...");
                    master.Entries[index].Select();
                }
                else
                {
                    Callback.Fire(selectString, true, index);
                }
                return;
            }

            var selectIconString = GetAddon("SelectIconString");
            if (selectIconString != null && selectIconString->IsVisible)
            {
                var master = new AddonMaster.SelectIconString((nint)selectIconString);
                if (master.Entries != null && index >= 0 && index < master.Entries.Length)
                {
                    ECommons.DalamudServices.Svc.Log.Info($"[AFC Dialog] Selecting SelectIconString entry index {index} ('{master.Entries[index].Text}')...");
                    master.Entries[index].Select();
                }
                else
                {
                    Callback.Fire(selectIconString, true, index);
                }
                return;
            }

            var talk = GetAddon("Talk");
            if (talk != null && talk->IsVisible)
            {
                Callback.Fire(talk, true, 0);
            }
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, "[AFC Dialog] Exception clicking dialog entry");
        }
    }

    public static void ClickYesnoYes()
    {
        try
        {
            var selectYesno = GetAddon("SelectYesno");
            if (selectYesno != null && selectYesno->IsVisible)
            {
                try
                {
                    new AddonMaster.SelectYesno((nint)selectYesno).Yes();
                }
                catch
                {
                    Callback.Fire(selectYesno, true, 0);
                }
            }
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, "[AFC Dialog] Exception confirming SelectYesno");
        }
    }

    public static void CloseDialog()
    {
        try
        {
            var selectString = GetAddon("SelectString");
            if (selectString != null && selectString->IsVisible)
            {
                Callback.Fire(selectString, true, -1);
                return;
            }

            var selectIconString = GetAddon("SelectIconString");
            if (selectIconString != null && selectIconString->IsVisible)
            {
                Callback.Fire(selectIconString, true, -1);
            }
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, "[AFC Dialog] Exception closing dialog");
        }
    }

    public static void CloseFcShop()
    {
        try
        {
            var fcExchange = GetAddon("FreeCompanyExchange");
            if (fcExchange != null && fcExchange->IsVisible)
            {
                Callback.Fire(fcExchange, true, -1);
                return;
            }

            var fcShop = GetAddon("FreeCompanyCreditShop");
            if (fcShop != null && fcShop->IsVisible)
            {
                Callback.Fire(fcShop, true, -1);
                return;
            }

            var gcExchange = GetAddon("GrandCompanyExchange");
            if (gcExchange != null && gcExchange->IsVisible)
            {
                Callback.Fire(gcExchange, true, -1);
            }
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, "[AFC Shop] Exception closing FC shop");
        }
    }

    public static void CloseAddon(string name)
    {
        try
        {
            var addon = GetAddon(name);
            if (addon != null && addon->IsVisible)
            {
                addon->Close(true);
            }
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, $"[AFC Addon] Exception closing addon '{name}'");
        }
    }

    public static void SelectShopTab(int tabIndex)
    {
        // Deprecated: shop items in FreeCompanyCreditShop / FreeCompanyExchange are indexed directly.
    }

    public static unsafe bool BuyFcAction(FcBuffDefinition buff, int count = 1)
    {
        try
        {
            var fcExchange = GetAddon("FreeCompanyExchange");
            if (fcExchange == null || !fcExchange->IsVisible)
            {
                fcExchange = GetAddon("FreeCompanyCreditShop");
            }
            if (fcExchange == null || !fcExchange->IsVisible)
            {
                fcExchange = GetAddon("GrandCompanyExchange");
            }

            if (fcExchange != null && fcExchange->IsVisible)
            {
                // 1. Primary Method: Scan NodeId 26 (central column vendor list) to locate exact row index of buff
                int node26RowIdx = FindShopItemIndexInNode26(fcExchange, buff.Name);
                int globalIndex;

                if (node26RowIdx >= 0)
                {
                    // Grade 2 actions in FreeCompanyExchange vendor shop require offset +17 (Grade 1: 0-16, Grade 2: 17-33)
                    globalIndex = (buff.Grade == 2) ? 17 + node26RowIdx : node26RowIdx;
                    ECommons.DalamudServices.Svc.Log.Info($"[AFC Vendor] Found '{buff.Name}' (Grade {buff.Grade}) in NodeId 26 at list row {node26RowIdx}. Global shop index = {globalIndex}. Firing purchase Callback 2...");
                }
                else
                {
                    globalIndex = buff.ItemIndex;
                    ECommons.DalamudServices.Svc.Log.Info($"[AFC Vendor] NodeId 26 lookup miss for '{buff.Name}'. Using registry fallback global index = {globalIndex}. Firing purchase Callback 2...");
                }

                Callback.Fire(fcExchange, true, 2, globalIndex, count);
                return true;
            }
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, $"[AFC Vendor] Exception during BuyFcAction for '{buff.Name}'");
        }

        return false;
    }

    public static unsafe int FindShopItemIndexInNode26(AtkUnitBase* fcExchange, string buffName)
    {
        if (fcExchange == null || !fcExchange->IsVisible) return -1;

        try
        {
            var uldManager = &fcExchange->UldManager;
            if (uldManager == null || uldManager->NodeListCount <= 0) return -1;

            AtkComponentNode* listCompNode = null;
            for (int i = 0; i < uldManager->NodeListCount; i++)
            {
                var n = uldManager->NodeList[i];
                if (n != null && n->NodeId == 26 && ((ushort)n->Type >= 1000))
                {
                    listCompNode = (AtkComponentNode*)n;
                    break;
                }
            }

            if (listCompNode == null || listCompNode->Component == null) return -1;

            var subUld = &listCompNode->Component->UldManager;
            if (subUld == null || subUld->NodeListCount <= 0) return -1;

            int currentListItemIndex = 0;
            for (int j = 0; j < subUld->NodeListCount; j++)
            {
                var rowNode = subUld->NodeList[j];
                if (rowNode == null) continue;

                ushort rowType = (ushort)rowNode->Type;
                if (rowType == 1013 || rowType >= 1000)
                {
                    var rowComp = (AtkComponentNode*)rowNode;
                    if (rowComp->Component != null)
                    {
                        var itemUld = &rowComp->Component->UldManager;
                        if (itemUld != null && itemUld->NodeListCount > 0)
                        {
                            for (int k = 0; k < itemUld->NodeListCount; k++)
                            {
                                var subItemNode = itemUld->NodeList[k];
                                // NodeId 3 inside the ListItemRenderer component is EXCLUSIVELY the Item Name text node!
                                if (subItemNode != null && subItemNode->Type == NodeType.Text && subItemNode->NodeId == 3)
                                {
                                    var txtNode = (AtkTextNode*)subItemNode;
                                    string text = txtNode->NodeText.ToString().Trim();
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        if (IsMatchingBuffName(text, buffName))
                                        {
                                            ECommons.DalamudServices.Svc.Log.Info($"[AFC Vendor] Matched '{buffName}' with NodeId 26 item '#3 Text={text}' at row index {currentListItemIndex}");
                                            return currentListItemIndex;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    currentListItemIndex++;
                }
            }
        }
        catch (Exception ex)
        {
            ECommons.DalamudServices.Svc.Log.Warning(ex, $"[AFC Vendor] Exception scanning NodeId 26 for '{buffName}'");
        }

        return -1;
    }

    private static bool IsMatchingBuffName(string textNodeText, string buffName)
    {
        if (string.IsNullOrWhiteSpace(textNodeText) || string.IsNullOrWhiteSpace(buffName)) return false;

        string cleanText = textNodeText.Trim();
        string cleanBuff = buffName.Trim();

        // Exact match
        if (cleanText.Equals(cleanBuff, StringComparison.OrdinalIgnoreCase)) return true;

        // Strip "The " prefix
        string textNoThe = cleanText.StartsWith("The ", StringComparison.OrdinalIgnoreCase) ? cleanText.Substring(4).Trim() : cleanText;
        string buffNoThe = cleanBuff.StartsWith("The ", StringComparison.OrdinalIgnoreCase) ? cleanBuff.Substring(4).Trim() : cleanBuff;

        if (textNoThe.Equals(buffNoThe, StringComparison.OrdinalIgnoreCase)) return true;

        // Grade matching (I vs II vs III)
        bool textIsGrade3 = cleanText.EndsWith(" III") || cleanText.EndsWith(" 3");
        bool buffIsGrade3 = cleanBuff.EndsWith(" III") || cleanBuff.EndsWith(" 3");

        bool textIsGrade2 = (cleanText.EndsWith(" II") || cleanText.EndsWith(" 2")) && !textIsGrade3;
        bool buffIsGrade2 = (cleanBuff.EndsWith(" II") || cleanBuff.EndsWith(" 2")) && !buffIsGrade3;

        bool textIsGrade1 = (cleanText.EndsWith(" I") || cleanText.EndsWith(" 1")) && !textIsGrade2 && !textIsGrade3;
        bool buffIsGrade1 = (cleanBuff.EndsWith(" I") || cleanBuff.EndsWith(" 1")) && !buffIsGrade2 && !buffIsGrade3;

        if (textIsGrade3 != buffIsGrade3 || textIsGrade2 != buffIsGrade2 || textIsGrade1 != buffIsGrade1) return false;

        // Multi-language mappings (French <-> English)
        if ((cleanBuff.Contains("Touch of Ingenuity", StringComparison.OrdinalIgnoreCase) || cleanBuff.Contains("Ingenuity", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("Control", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("Ingenuity", StringComparison.OrdinalIgnoreCase))) return true;

        if ((cleanBuff.Contains("Helping Hand", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("travail", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("Helping", StringComparison.OrdinalIgnoreCase))) return true;

        if ((cleanBuff.Contains("Heat of Battle", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("ardeur", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("Battle", StringComparison.OrdinalIgnoreCase))) return true;

        if ((cleanBuff.Contains("Meat and Mead", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("Consommation", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("Mead", StringComparison.OrdinalIgnoreCase))) return true;

        if ((cleanBuff.Contains("Reduced Rates", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("réduits", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("Rates", StringComparison.OrdinalIgnoreCase))) return true;

        if ((cleanBuff.Contains("Live Off the Land", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("Richesse", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("Land", StringComparison.OrdinalIgnoreCase))) return true;

        if ((cleanBuff.Contains("Seal Sweetener", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("Sceaux", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("Sweetener", StringComparison.OrdinalIgnoreCase))) return true;

        if ((cleanBuff.Contains("Proper Maintenance", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("Entretien", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("Maintenance", StringComparison.OrdinalIgnoreCase))) return true;

        if ((cleanBuff.Contains("Jackpot", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("Jackpot", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("MGP", StringComparison.OrdinalIgnoreCase))) return true;

        if ((cleanBuff.Contains("Back on Your Feet", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("Feet", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("Pieds", StringComparison.OrdinalIgnoreCase))) return true;

        if ((cleanBuff.Contains("Brave New World", StringComparison.OrdinalIgnoreCase)) &&
            (cleanText.Contains("World", StringComparison.OrdinalIgnoreCase) || cleanText.Contains("Monde", StringComparison.OrdinalIgnoreCase))) return true;

        return textNoThe.Contains(buffNoThe, StringComparison.OrdinalIgnoreCase) || buffNoThe.Contains(textNoThe, StringComparison.OrdinalIgnoreCase);
    }
}
