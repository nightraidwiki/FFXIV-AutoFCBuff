namespace AutoFCBuff.Core.Buffs;

public sealed record FcBuffDefinition(
    uint Id,
    string Name,
    string Description,
    int Grade,
    FcBuffCategory Category,
    uint IconId,
    uint CreditCost,
    uint ItemId,
    int TabIndex,
    int ItemIndex
);
