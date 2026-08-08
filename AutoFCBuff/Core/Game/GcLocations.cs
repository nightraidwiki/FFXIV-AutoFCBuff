using System.Numerics;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace AutoFCBuff.Core.Game;

public enum GcChoice
{
    Auto,
    Gridania,
    LimsaLominsa,
    Uldah
}

public sealed record GcTargetInfo(
    byte GcId,
    string GcName,
    string CityName,
    string NpcName,
    uint PrimaryTerritoryId,
    uint[] ValidTerritories,
    uint AetheryteId,
    Vector3 NpcPosition,
    uint NpcDataId
);

public static class GcLocations
{
    // GrandCompany Excel Sheet & Game IDs:
    // 1 = Maelstrom (Limsa Lominsa)
    // 2 = Order of the Twin Adder (Gridania)
    // 3 = Immortal Flames (Ul'dah)

    public static readonly GcTargetInfo LimsaMaelstrom = new(
        GcId: 1,
        GcName: "The Maelstrom",
        CityName: "Limsa Lominsa",
        NpcName: "Maelstrom OIC Quartermaster",
        PrimaryTerritoryId: 129, // Teleport lands in Lower Decks (129)
        ValidTerritories: new uint[] { 128, 129 },
        AetheryteId: 8,
        NpcPosition: new Vector3(93.9f, 40.2f, 75.4f),
        NpcDataId: 1002387
    );

    public static readonly GcTargetInfo GridaniaAdder = new(
        GcId: 2,
        GcName: "Order of the Twin Adder",
        CityName: "New Gridania",
        NpcName: "Twin Adder OIC Quartermaster",
        PrimaryTerritoryId: 132,
        ValidTerritories: new uint[] { 132, 133 },
        AetheryteId: 2,
        NpcPosition: new Vector3(-67.3f, -0.5f, -8.1f),
        NpcDataId: 1002388
    );

    public static readonly GcTargetInfo UldahFlames = new(
        GcId: 3,
        GcName: "The Immortal Flames",
        CityName: "Ul'dah - Steps of Nald",
        NpcName: "Flame OIC Quartermaster",
        PrimaryTerritoryId: 130,
        ValidTerritories: new uint[] { 130, 131 },
        AetheryteId: 9,
        NpcPosition: new Vector3(84.2f, -4.1f, -93.5f),
        NpcDataId: 1002389
    );

    public static GcTargetInfo ResolveTarget(GcChoice choice)
    {
        if (choice == GcChoice.Gridania) return GridaniaAdder;
        if (choice == GcChoice.LimsaLominsa) return LimsaMaelstrom;
        if (choice == GcChoice.Uldah) return UldahFlames;

        byte detectedGc = 0;

        // 1. Try Free Company Grand Company
        try
        {
            unsafe
            {
                var fcProxy = InfoProxyFreeCompany.Instance();
                if (fcProxy != null && fcProxy->GrandCompany != FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany.None)
                {
                    detectedGc = (byte)fcProxy->GrandCompany;
                    Svc.Log.Info($"[AFC] Detected Free Company GrandCompany: {detectedGc}");
                }
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Debug(ex, "[AFC] Could not read InfoProxyFreeCompany.GrandCompany");
        }

        // 2. Fall back to ECommons Player personal GrandCompany
        if (detectedGc == 0)
        {
            try
            {
                detectedGc = (byte)ECommons.GameHelpers.Player.GrandCompany;
                Svc.Log.Info($"[AFC] Detected Player Personal GrandCompany via ECommons: {detectedGc}");
            }
            catch { }
        }

        // 3. Fall back to UIState PlayerState.GrandCompany
        if (detectedGc == 0)
        {
            try
            {
                unsafe
                {
                    var uiState = UIState.Instance();
                    if (uiState != null)
                    {
                        detectedGc = uiState->PlayerState.GrandCompany;
                        Svc.Log.Info($"[AFC] Detected Player Personal GrandCompany via UIState: {detectedGc}");
                    }
                }
            }
            catch { }
        }

        // Map GC ID (1 = Maelstrom, 2 = Twin Adder, 3 = Immortal Flames)
        return detectedGc switch
        {
            1 => LimsaMaelstrom,
            2 => GridaniaAdder,
            3 => UldahFlames,
            _ => UldahFlames,
        };
    }
}
