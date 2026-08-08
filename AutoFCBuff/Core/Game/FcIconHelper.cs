using System;
using System.Numerics;
using AutoFCBuff;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Lumina.Excel.Sheets;

namespace AutoFCBuff.Core.Game;

public static class FcIconHelper
{
    public static void DrawIcon(uint iconId, Vector2 size)
    {
        try
        {
            if (TryGetIconWrap(iconId, out var wrap) && wrap != null && wrap.Handle != nint.Zero)
            {
                ImGui.Image(wrap.Handle, size);
                return;
            }
        }
        catch
        {
            // Suppress icon rendering exceptions
        }

        ImGui.Dummy(size);
    }

    public static uint LookupOfficialIconId(uint companyActionId)
    {
        try
        {
            var sheet = Plugin.DataManager.GetExcelSheet<CompanyAction>();
            if (sheet != null)
            {
                var row = sheet.GetRow(companyActionId);
                if (row.RowId > 0 && row.Icon > 0)
                {
                    return (uint)row.Icon;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, $"[AFC IconHelper] Failed to lookup CompanyAction icon for row {companyActionId}");
        }

        return 0;
    }

    private static bool TryGetIconWrap(uint iconId, out IDalamudTextureWrap? wrap)
    {
        wrap = null;

        // 1. Try HiRes
        try
        {
            var lookupHiRes = new GameIconLookup(iconId) { HiRes = true };
            if (Plugin.TextureProvider.TryGetFromGameIcon(lookupHiRes, out var texHiRes) && texHiRes != null)
            {
                wrap = texHiRes.GetWrapOrEmpty();
                if (wrap != null && wrap.Handle != nint.Zero)
                    return true;
            }
        }
        catch { }

        // 2. Try Standard Resolution
        try
        {
            var lookupStd = new GameIconLookup(iconId) { HiRes = false };
            if (Plugin.TextureProvider.TryGetFromGameIcon(lookupStd, out var texStd) && texStd != null)
            {
                wrap = texStd.GetWrapOrEmpty();
                if (wrap != null && wrap.Handle != nint.Zero)
                    return true;
            }
        }
        catch { }

        return false;
    }
}
