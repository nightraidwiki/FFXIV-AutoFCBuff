using System.Collections.Generic;
using System.Linq;

namespace AutoFCBuff.Core.Buffs;

public static class FcBuffRegistry
{
    public static readonly IReadOnlyList<FcBuffDefinition> Buffs = new List<FcBuffDefinition>
    {
        // Combat Buffs
        new(1, "Heat of Battle I", "Increases EXP earned through combat by 5%.", 1, FcBuffCategory.Combat, 216513, 3000, 20054, 0, 0),
        new(2, "Heat of Battle II", "Increases EXP earned through combat by 10%.", 2, FcBuffCategory.Combat, 216513, 6000, 20055, 0, 17),
        new(13, "Back on Your Feet I", "Reduces weakness duration by 10%.", 1, FcBuffCategory.Combat, 216511, 3000, 20066, 0, 12),
        new(14, "Back on Your Feet II", "Reduces weakness duration by 15%.", 2, FcBuffCategory.Combat, 216511, 6000, 20067, 0, 29),
        new(19, "Brave New World I", "Increases primary attributes for characters level 49 and under by 10%.", 1, FcBuffCategory.Combat, 216501, 3000, 20072, 0, 7),
        new(20, "Brave New World II", "Increases primary attributes for characters level 49 and under by 15%.", 2, FcBuffCategory.Combat, 216501, 6000, 20073, 0, 24),

        // Crafting & Gathering Buffs
        new(3, "Earth and Water I", "Increases EXP earned through gathering by 5%.", 1, FcBuffCategory.CraftingGathering, 216515, 3000, 20056, 1, 1),
        new(4, "Earth and Water II", "Increases EXP earned through gathering by 10%.", 2, FcBuffCategory.CraftingGathering, 216515, 6000, 20057, 1, 18),
        new(5, "Helping Hand I", "Increases EXP earned through crafting by 5%.", 1, FcBuffCategory.CraftingGathering, 216516, 3000, 20058, 1, 2),
        new(6, "Helping Hand II", "Increases EXP earned through crafting by 10%.", 2, FcBuffCategory.CraftingGathering, 216516, 6000, 20059, 1, 19),
        new(15, "Live Off the Land I", "Increases Gathering by 5.", 1, FcBuffCategory.CraftingGathering, 216502, 3000, 20068, 1, 8),
        new(16, "Live Off the Land II", "Increases Gathering by 10.", 2, FcBuffCategory.CraftingGathering, 216502, 6000, 20069, 1, 25),
        new(17, "Touch of Ingenuity I", "Increases Control by 5.", 1, FcBuffCategory.CraftingGathering, 216505, 3000, 20070, 1, 11),
        new(18, "Touch of Ingenuity II", "Increases Control by 10.", 2, FcBuffCategory.CraftingGathering, 216505, 6000, 20071, 1, 28),

        // Utility Buffs
        new(7, "Meat and Mead I", "Extends meal benefit duration by 10 minutes.", 1, FcBuffCategory.Utility, 216508, 3000, 20060, 2, 13),
        new(8, "Meat and Mead II", "Extends meal benefit duration by 15 minutes.", 2, FcBuffCategory.Utility, 216508, 6000, 20061, 2, 30),
        new(9, "Reduced Rates I", "Reduces teleportation fees by 15%.", 1, FcBuffCategory.Utility, 216512, 3000, 20062, 2, 16),
        new(10, "Reduced Rates II", "Reduces teleportation fees by 20%.", 2, FcBuffCategory.Utility, 216512, 6000, 20063, 2, 33),
        new(11, "Proper Maintenance I", "Reduces gear wear by 10%.", 1, FcBuffCategory.Utility, 216510, 3000, 20064, 2, 14),
        new(12, "Proper Maintenance II", "Reduces gear wear by 20%.", 2, FcBuffCategory.Utility, 216510, 6000, 20065, 2, 31),
        new(21, "Jackpot I", "Increases MGP earned in the Gold Saucer by 5%.", 1, FcBuffCategory.Utility, 216519, 3000, 20074, 2, 6),
        new(22, "Jackpot II", "Increases MGP earned in the Gold Saucer by 10%.", 2, FcBuffCategory.Utility, 216519, 6000, 20075, 2, 23),
        new(23, "Seal Sweetener I", "Increases company seals earned by 5%.", 1, FcBuffCategory.Utility, 216518, 3000, 20076, 2, 5),
        new(24, "Seal Sweetener II", "Increases company seals earned by 10%.", 2, FcBuffCategory.Utility, 216518, 6000, 20077, 2, 22),
    };

    public static FcBuffDefinition? GetById(uint id) => Buffs.FirstOrDefault(b => b.Id == id);
}
