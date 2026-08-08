using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoFCBuff.Core.Debug;

internal static unsafe class AddonInspector
{
    public static void Dump()
    {
        var manager = RaptureAtkUnitManager.Instance();
        if (manager == null)
        {
            Svc.Chat.Print("[AFC Debug] RaptureAtkUnitManager is null.");
            return;
        }

        Svc.Chat.Print("=== [AFC UI Debugger] Currently Visible Addons ===");
        int count = 0;

        for (int i = 0; i < manager->AllLoadedUnitsList.Count; i++)
        {
            var unit = manager->AllLoadedUnitsList.Entries[i].Value;
            if (unit == null || !unit->IsVisible) continue;

            var name = unit->NameString;
            count++;
            Svc.Log.Info($"[AFC Debug] Visible Addon #{count}: '{name}' (Addr=0x{(nint)unit:X})");
            Svc.Chat.Print($"[AFC] Addon: '{name}'");

            if (name == "SelectString" || name == "SelectIconString")
            {
                DumpSelectString(unit, name);
            }
            else if (name == "FreeCompanyAction" || name == "FreeCompanyExchange" || name == "FreeCompanyCreditShop" || name == "GrandCompanyExchange")
            {
                DumpShopAddon(unit, name);
            }
        }

        if (count == 0)
        {
            Svc.Chat.Print("[AFC Debug] No visible AtkUnitBase windows found.");
        }
    }

    private static void DumpSelectString(AtkUnitBase* unit, string name)
    {
        Svc.Chat.Print($"--- {name} Entries ---");
        try
        {
            var atkValues = unit->AtkValues;
            int valCount = unit->AtkValuesCount;
            Svc.Chat.Print($"AtkValuesCount = {valCount}");

            int limit = Math.Min(valCount, 30);
            for (int i = 0; i < limit; i++)
            {
                var val = atkValues[i];
                if (val.Type == FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.String && val.String.Value != null)
                {
                    var text = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((nint)val.String.Value);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Svc.Chat.Print($"  [{i}] Text: '{text}'");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, $"[AFC Debug] Failed to dump {name}");
        }
    }

    private static void DumpShopAddon(AtkUnitBase* unit, string name)
    {
        Svc.Chat.Print($"--- {name} AtkValues (Count={unit->AtkValuesCount}) ---");
        try
        {
            int valCount = unit->AtkValuesCount;
            if (valCount > 0 && unit->AtkValues != null)
            {
                int limit = Math.Min(valCount, 80);
                for (int i = 0; i < limit; i++)
                {
                    var val = unit->AtkValues[i];
                    if (val.Type == FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType.String && val.String.Value != null)
                    {
                        var text = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((nint)val.String.Value);
                        if (!string.IsNullOrWhiteSpace(text))
                            Svc.Chat.Print($"  [{i}] String: '{text}'");
                    }
                    else if (val.UInt > 0 || val.Int > 0)
                    {
                        Svc.Chat.Print($"  [{i}] UInt={val.UInt} Int={val.Int} Type={val.Type}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, $"[AFC Debug] Failed to dump {name}");
        }
    }
}
