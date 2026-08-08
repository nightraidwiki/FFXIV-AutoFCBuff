using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AutoFCBuff.Core.Buffs;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoFCBuff.Core.Game;

public static class FcActionActivator
{
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const int VK_NUMPAD0 = 0x60; // FFXIV UI Confirm key (Gamepad A / Numpad 0)

    public static async Task<bool> ActivateFcBuffFromStockAsync(uint buffId)
    {
        var buff = FcBuffRegistry.GetById(buffId);
        if (buff == null) return false;

        Svc.Log.Info($"[AFC Activator] Attempting auto-activation for FC buff '{buff.Name}' (Id={buffId})...");

        // 1. Ensure FreeCompanyAction window is open
        if (!IsFcActionAddonVisible())
        {
            Chat.SendMessage("/fccmd action");
            var waitStart = DateTime.UtcNow;
            while ((DateTime.UtcNow - waitStart).TotalMilliseconds < 2500)
            {
                if (IsFcActionAddonVisible()) break;
                await Task.Delay(50);
            }
        }

        if (!IsFcActionAddonVisible())
        {
            Svc.Log.Warning("[AFC Activator] FreeCompanyAction window failed to open for activation.");
            return false;
        }

        // Wait 250ms for ULD nodes to render
        await Task.Delay(250);

        // 2. Locate slot index for this buff name inside FreeCompanyAction
        int slotIndex = FindBuffSlotIndexInActionAddon(buff.Name);
        if (slotIndex < 0)
        {
            Svc.Log.Warning($"[AFC Activator] Buff '{buff.Name}' is not currently available in FC reserve stock.");
            return false;
        }

        Svc.Log.Info($"[AFC Activator] Found '{buff.Name}' at slot index {slotIndex}. Firing targeted activation callback...");

        // 3. Fire targeted activation callback (Callback 1 = Execute slot)
        FireSlotExecuteCallback(slotIndex);
        await Task.Delay(200);

        // 4. Multi-step confirmation loop (NUMPAD0 + SelectYesNo / ContextMenu)
        bool success = false;
        for (int i = 1; i <= 4; i++)
        {
            // A. Check if SelectYesno confirmation dialog is visible -> Click YES (0)
            if (TryConfirmSelectYesNo())
            {
                Svc.Log.Info($"[AFC Activator] SelectYesno confirmed successfully on step {i}!");
                success = true;
                break;
            }

            // B. Check if ContextMenu is visible -> Select Execute (0)
            TrySelectContextMenuExecute();

            // C. Fire slot execute callback again if window is still open
            FireSlotExecuteCallback(slotIndex);

            // D. Send native NUMPAD0 keypress (Gamepad A / UI Accept)
            Svc.Log.Info($"[AFC Activator] Sending NUMPAD0 keypress (step {i}/4)...");
            SendNativeNumpad0();

            await Task.Delay(250);
        }

        // Final check after loop for SelectYesNo
        if (!success && TryConfirmSelectYesNo())
        {
            success = true;
        }

        await Task.Delay(300);
        Svc.Log.Info($"[AFC Activator] Activation sequence finished for '{buff.Name}'. Success={success}");
        return success;
    }

    private static unsafe bool IsFcActionAddonVisible()
    {
        var fcActionAddon = AddonInteractions.GetAddon("FreeCompanyAction");
        return fcActionAddon != null && fcActionAddon->IsVisible;
    }

    private static unsafe void FireSlotExecuteCallback(int slotIndex)
    {
        var fcActionAddon = AddonInteractions.GetAddon("FreeCompanyAction");
        if (fcActionAddon != null && fcActionAddon->IsVisible)
        {
            // Callback 1 triggers Execute on slotIndex (DO NOT fire callback 0 as it closes FreeCompanyAction!)
            Callback.Fire(fcActionAddon, true, 1, slotIndex);
        }
    }

    private static unsafe bool TrySelectContextMenuExecute()
    {
        var contextMenuAddon = AddonInteractions.GetAddon("ContextMenu");
        if (contextMenuAddon != null && contextMenuAddon->IsVisible)
        {
            Svc.Log.Info("[AFC Activator] ContextMenu detected. Firing Execute callback (0)...");
            Callback.Fire(contextMenuAddon, true, 0);
            return true;
        }
        return false;
    }

    private static unsafe bool TryConfirmSelectYesNo()
    {
        var yesNoAddon = AddonInteractions.GetAddon("SelectYesno");
        if (yesNoAddon != null && yesNoAddon->IsVisible)
        {
            Svc.Log.Info("[AFC Activator] SelectYesno confirmation dialog detected. Firing YES callback (0)...");
            Callback.Fire(yesNoAddon, true, 0);
            return true;
        }
        return false;
    }

    private static void SendNativeNumpad0()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var hWnd = process.MainWindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                // Send NUMPAD0 exclusively (Default FFXIV UI Accept/Confirm key, mapped to Xbox A)
                PostMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_NUMPAD0, IntPtr.Zero);
                PostMessage(hWnd, WM_KEYUP, (IntPtr)VK_NUMPAD0, IntPtr.Zero);
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "[AFC Activator] Exception sending native NUMPAD0 key");
        }
    }

    private static unsafe int FindBuffSlotIndexInActionAddon(string buffName)
    {
        var fcActionAddon = AddonInteractions.GetAddon("FreeCompanyAction");
        if (fcActionAddon == null || !fcActionAddon->IsVisible) return -1;

        try
        {
            var uldManager = &fcActionAddon->UldManager;
            if (uldManager != null && uldManager->NodeListCount > 0)
            {
                for (int i = 0; i < uldManager->NodeListCount; i++)
                {
                    var node = uldManager->NodeList[i];
                    if (node == null) continue;

                    // Scan Node 14 (Reserve Stock) or Node 10/11 (Available Actions container)
                    if (node->NodeId == 14 || node->NodeId == 10 || node->NodeId == 11)
                    {
                        ushort typeVal = (ushort)node->Type;
                        if (typeVal >= 1000)
                        {
                            var compNode = (AtkComponentNode*)node;
                            if (compNode->Component != null)
                            {
                                var subUld = &compNode->Component->UldManager;
                                if (subUld != null && subUld->NodeListCount > 0)
                                {
                                    int currentSlotIndex = 0;
                                    for (int j = 0; j < subUld->NodeListCount; j++)
                                    {
                                        var slotNode = subUld->NodeList[j];
                                        if (slotNode == null || !slotNode->IsVisible()) continue;

                                        ushort slotType = (ushort)slotNode->Type;
                                        if (slotType == 1008 || slotType == 1005 || slotType == 1006)
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

                                                        if (subItemNode->Type == NodeType.Text)
                                                        {
                                                            var txtNode = (AtkTextNode*)subItemNode;
                                                            string text = txtNode->NodeText.ToString();
                                                            if (!string.IsNullOrWhiteSpace(text) && text.Contains(buffName, StringComparison.OrdinalIgnoreCase))
                                                            {
                                                                return currentSlotIndex;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            currentSlotIndex++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "[AFC Activator] Error locating slot index in FreeCompanyAction");
        }

        return -1;
    }
}
