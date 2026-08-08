using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace AutoFCBuff.Core.Debug;

internal static unsafe class TargetDumper
{
    public static void Dump()
    {
        var territoryId = Svc.ClientState.TerritoryType;
        var playerPos = Svc.Objects.LocalPlayer?.Position;
        var posStr = playerPos.HasValue ? $"({playerPos.Value.X:F2}, {playerPos.Value.Y:F2}, {playerPos.Value.Z:F2})" : "?";

        Svc.Log.Info($"[AFC TargetDumper] Territory: {territoryId} | Player Pos: {posStr}");
        Svc.Chat.Print($"[AFC] Territory: {territoryId} | Player Pos: {posStr}");

        var target = TargetSystem.Instance()->Target;
        if (target == null)
        {
            Svc.Chat.Print("[AFC] No target selected. Target the Quartermaster NPC, then type /afc target.");
            return;
        }

        var baseId = target->BaseId;
        var name = target->NameString;
        var tPos = target->Position;

        Svc.Log.Info($"[AFC TargetDumper] Target BaseId={baseId} Name='{name}' Pos=({tPos.X:F2}, {tPos.Y:F2}, {tPos.Z:F2})");
        Svc.Chat.Print($"[AFC] Target NPC: '{name}' | BaseId={baseId} | Pos=({tPos.X:F2}, {tPos.Y:F2}, {tPos.Z:F2})");
    }
}
