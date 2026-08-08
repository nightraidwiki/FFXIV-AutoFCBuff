using System.Numerics;
using System.Threading.Tasks;
using AutoFCBuff.Core.Buffs;
using AutoFCBuff.Core.Game;
using AutoFCBuff.Core.Ipc;
using clib.TaskSystem;
using ECommons.Automation;
using ECommons.DalamudServices;

namespace AutoFCBuff.Core.Tasks;

public sealed class AutoFcBuffTask : AutoCommon
{
    private readonly Configuration config;
    private readonly FcBuffRunProgress progress;

    public FcBuffRunProgress Progress => progress;

    public AutoFcBuffTask(Configuration config)
    {
        this.config = config;
        this.progress = new FcBuffRunProgress();
    }

    protected override async Task Execute()
    {
        try
        {
            progress.CurrentStep = FcRunStep.Initializing;
            progress.StatusMessage = "Analyzing target FC buffs and stock limits...";

            // 1. Resolve GC Target
            var gcTarget = GcLocations.ResolveTarget(config.SelectedGcChoice);
            progress.TargetGcCity = gcTarget.CityName;
            Diag($"Resolved Grand Company destination: {gcTarget.GcName} ({gcTarget.CityName}) -> Target NPC: {gcTarget.NpcName}");

            // 1.5. Live Scan FC Reserve Stock & Active Buffs before run
            progress.StatusMessage = "Scanning live FC reserve stock and active buffs...";
            var (scannedStock, scannedActiveBuffs) = await FcStockReader.ScanStockAsync();
            config.CurrentStock.Clear();
            foreach (var (buffId, count) in scannedStock)
            {
                config.CurrentStock[buffId] = count;
            }
            config.ActiveFcBuffNames = scannedActiveBuffs;
            config.SaveDebounced();
            Diag($"Live FC reserve stock scanned: {scannedStock.Values.Sum()}/15 in stock. Active buffs: {string.Join(", ", scannedActiveBuffs)}");

            // 2. Calculate missing items (respecting 15 stock limit)
            var itemsToBuy = CalculateItemsToBuy(out var totalCount);
            progress.TotalToBuy = totalCount;

            if (totalCount == 0)
            {
                progress.CurrentStep = FcRunStep.Completed;
                progress.StatusMessage = "All FC buff stock targets are already satisfied!";
                Diag("No missing buffs to purchase. Task finished cleanly.");
                return;
            }

            Diag($"Total missing FC buffs to purchase: {totalCount}");

            // 3. Teleport to GC City Aetheryte
            progress.CurrentStep = FcRunStep.Teleporting;
            progress.StatusMessage = $"Teleporting to {gcTarget.CityName}...";

            var teleportSuccess = await TeleportToGcCity(gcTarget, 25_000);
            if (!teleportSuccess)
            {
                progress.CurrentStep = FcRunStep.Failed;
                progress.StatusMessage = $"Failed to teleport to {gcTarget.CityName}.";
                return;
            }

            await NextFrame(60);

            // 4. Navigate to GC OIC Quartermaster NPC
            progress.CurrentStep = FcRunStep.Navigating;
            progress.StatusMessage = $"Navigating to {gcTarget.NpcName}...";

            var targetNpc = FindOicQuartermaster(gcTarget);
            var targetPos = targetNpc?.Position ?? gcTarget.NpcPosition;
            var targetTerritory = targetNpc != null ? Svc.ClientState.TerritoryType : gcTarget.PrimaryTerritoryId;

            if (NavmeshIPC.Instance.IsAvailable)
            {
                var moveOp = new MoveOp(op => op.Move(targetTerritory, targetPos, MovementConfig.Everything.WithTolerance(2.5f), allowTeleportIfFaster: false, null, allowAethernetWithinTerritory: true));
                await RunCancellable(moveOp, 40_000, $"Navigate -> {gcTarget.NpcName}", IdleStallAbort(IdleStallTimeoutMs));
            }
            else
            {
                Diag("vnavmesh not available, skipping auto-pathing. Assuming player is near NPC.");
            }

            await NextFrame(30);

            // Re-find OIC Quartermaster NPC after navigation
            targetNpc = FindOicQuartermaster(gcTarget);

            // 5. Target and Interact with NPC
            progress.CurrentStep = FcRunStep.Purchasing;
            progress.StatusMessage = $"Interacting with {gcTarget.NpcName}...";

            if (targetNpc != null)
            {
                Diag($"Targeting & interacting with NPC: '{targetNpc.Name.TextValue}' (BaseId={targetNpc.BaseId})");
                AddonInteractions.InteractWithNpc(targetNpc);
                await NextFrame(45);
            }
            else
            {
                Diag($"Warning: Could not find NPC '{gcTarget.NpcName}' in ObjectTable near player.");
            }

            // 6. Dialogue Sequence: Pass Talk bubble, then select 'Exchange company credits' (Vendor Shop)
            var dialogueDeadline = Environment.TickCount64 + 10_000;
            bool optionClicked = false;

            while (Environment.TickCount64 < dialogueDeadline && !AddonInteractions.IsFcShopOpen())
            {
                if (CancelToken.IsCancellationRequested) break;

                // Safety: If Execute Action window opened by mistake, close it!
                if (AddonInteractions.IsFcActionWindowOpen())
                {
                    Diag("Execute Action window opened by mistake! Closing it...");
                    AddonInteractions.CloseAddon("FreeCompanyAction");
                    await NextFrame(20);
                }

                // Step 2: Pass NPC Talk speech bubble
                if (AddonInteractions.IsTalkOpen())
                {
                    Diag("NPC Talk speech bubble open. Advancing dialogue...");
                    AddonInteractions.AdvanceTalk();
                    await NextFrame(25);
                    continue;
                }

                // Step 3: Select option for 'Exchange company credits' (Vendor Shop)
                if (AddonInteractions.IsDialogWindowOpen() && !optionClicked)
                {
                    int shopIndex = AddonInteractions.GetFcCreditShopDialogIndex();
                    Diag($"Dialogue window active! Clicking entry {shopIndex} ('Exchange company credits').");
                    AddonInteractions.ClickDialogEntry(shopIndex);
                    optionClicked = true;
                    await NextFrame(45);
                    continue;
                }

                await NextFrame(15);
            }

            // Step 4: FC Shop Window (FreeCompanyCreditShop / FreeCompanyExchange)
            var shopOpened = await WaitUntilTimed(() => AddonInteractions.IsFcShopOpen(), 5000, "Wait FC Shop Window");
            if (!shopOpened)
            {
                Diag("Warning: FC Credit Shop window is not open.");
            }
            else
            {
                Diag("FC Credit Shop window is OPEN!");
                await NextFrame(30); // Settling delay

                // Sync live FC stock directly from shop window
                var liveStock = AddonInteractions.ReadLiveFcStock();
                if (liveStock.Count > 0)
                {
                    foreach (var (buffId, count) in liveStock)
                    {
                        config.CurrentStock[buffId] = count;
                    }
                    config.SaveDebounced();

                    // Recalculate missing items with updated live stock
                    itemsToBuy = CalculateItemsToBuy(out totalCount);
                    progress.TotalToBuy = totalCount;

                    if (totalCount == 0)
                    {
                        progress.CurrentStep = FcRunStep.Completed;
                        progress.StatusMessage = "All FC buff stock targets are satisfied!";
                        if (AddonInteractions.IsFcShopOpen()) AddonInteractions.CloseFcShop();
                        return;
                    }
                }
            }

            // Purchase missing target buffs in shop
            foreach (var (buff, buyQty) in itemsToBuy)
            {
                if (CancelToken.IsCancellationRequested) break;

                progress.StatusMessage = $"Buying {buff.Name} (x{buyQty})...";
                Diag($"Purchasing {buff.Name} (Tab={buff.TabIndex}, Index={buff.ItemIndex}) x{buyQty}");

                for (var i = 0; i < buyQty; i++)
                {
                    if (CancelToken.IsCancellationRequested) break;

                    if (AddonInteractions.IsFcShopOpen())
                    {
                        // Buy item directly from the NPC vendor shop list
                        var success = AddonInteractions.BuyFcAction(buff, 1);
                        if (success)
                        {
                            await NextFrame(20);

                            // 3. Check for SelectYesno confirmation window ("Yes")
                            var yesnoOpened = await WaitUntilTimed(() => AddonInteractions.IsSelectYesnoOpen(), 1500, "Wait SelectYesno");
                            if (AddonInteractions.IsSelectYesnoOpen())
                            {
                                Diag("SelectYesno confirmation popup active. Clicking 'Yes'!");
                                AddonInteractions.ClickYesnoYes();
                                await NextFrame(30);
                            }

                            progress.PurchasedCount++;
                            config.CurrentStock[buff.Id] = config.CurrentStock.GetValueOrDefault(buff.Id, 0) + 1;
                            config.SaveDebounced();

                            Diag($"Successfully bought unit {i + 1}/{buyQty} of {buff.Name}. New local stock = {config.CurrentStock[buff.Id]}");
                        }
                        else
                        {
                            Diag($"BuyFcAction returned false for {buff.Name} unit {i + 1}/{buyQty}");
                        }
                        await NextFrame(45);
                    }
                }
            }

            // Close vendor window
            if (AddonInteractions.IsFcShopOpen()) AddonInteractions.CloseFcShop();

            // Auto-Activate configured buffs AFTER purchasing if enabled
            if (config.EnableAutoActivation && (config.TargetActiveBuff1 > 0 || config.TargetActiveBuff2 > 0))
            {
                var activeBuffs = FcStockReader.GetActiveFcBuffsFromPlayerStatus();
                if (activeBuffs.Count == 0)
                {
                    Diag("Auto-activation enabled and no active FC buffs detected. Activating configured target buffs from stock...");

                    if (config.TargetActiveBuff1 > 0)
                    {
                        var b1 = FcBuffRegistry.GetById(config.TargetActiveBuff1);
                        progress.StatusMessage = $"Activating FC buff '{b1?.Name}'...";
                        await FcActionActivator.ActivateFcBuffFromStockAsync(config.TargetActiveBuff1);
                        await Task.Delay(400);
                    }

                    if (config.TargetActiveBuff2 > 0)
                    {
                        var b2 = FcBuffRegistry.GetById(config.TargetActiveBuff2);
                        progress.StatusMessage = $"Activating FC buff '{b2?.Name}'...";
                        await FcActionActivator.ActivateFcBuffFromStockAsync(config.TargetActiveBuff2);
                        await Task.Delay(400);
                    }
                }
            }

            // 9. Dispatch Post-Run Commands
            if (!string.IsNullOrWhiteSpace(config.PostRunCommands))
            {
                foreach (var cmd in config.PostRunCommands.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (cmd.StartsWith("/"))
                    {
                        Diag($"Executing post-run chat command: {cmd}");
                        Chat.SendMessage(cmd);
                        await NextFrame(20);
                    }
                }
            }

            progress.CurrentStep = FcRunStep.Completed;
            progress.StatusMessage = $"Successfully purchased {progress.PurchasedCount}/{progress.TotalToBuy} FC buffs!";
        }
        catch (Exception ex)
        {
            progress.CurrentStep = FcRunStep.Failed;
            progress.StatusMessage = $"Task encountered error: {ex.Message}";
            Diag($"Task Exception: {ex}");
        }
    }

    private static Dalamud.Game.ClientState.Objects.Types.IGameObject? FindOicQuartermaster(GcTargetInfo gcTarget)
    {
        return Svc.Objects.FirstOrDefault(o =>
            o.Name.TextValue.Contains("OIC Quartermaster", StringComparison.OrdinalIgnoreCase) ||
            (o.Name.TextValue.Contains("OIC", StringComparison.OrdinalIgnoreCase) && o.Name.TextValue.Contains("Quartermaster", StringComparison.OrdinalIgnoreCase)) ||
            o.Name.TextValue.Equals(gcTarget.NpcName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> TeleportToGcCity(GcTargetInfo gcTarget, int perAttemptTimeoutMs = 25_000)
    {
        for (var i = 1; i <= 4 && !CancelToken.IsCancellationRequested; i++)
        {
            if (gcTarget.ValidTerritories.Contains(Svc.ClientState.TerritoryType)) return true;

            var op = new MoveOp(o => o.Teleport(gcTarget.PrimaryTerritoryId, gcTarget.NpcPosition, allowSameZoneTeleport: false));
            await RunCancellable(op, perAttemptTimeoutMs, $"Teleport -> {gcTarget.CityName}#{i}", IdleStallAbort(IdleStallTimeoutMs));

            if (gcTarget.ValidTerritories.Contains(Svc.ClientState.TerritoryType)) return true;
            await NextFrame(120);
        }
        return gcTarget.ValidTerritories.Contains(Svc.ClientState.TerritoryType);
    }

    private List<(FcBuffDefinition Buff, int BuyQty)> CalculateItemsToBuy(out int totalCount)
    {
        var result = new List<(FcBuffDefinition Buff, int BuyQty)>();
        totalCount = 0;

        int currentTotalStock = 0;
        foreach (var buff in FcBuffRegistry.Buffs)
        {
            var currentStock = config.CurrentStock.GetValueOrDefault(buff.Id, 0);
            currentTotalStock += currentStock;

            var buyQty = config.TargetStock.GetValueOrDefault(buff.Id, 0);
            if (buyQty > 0)
            {
                result.Add((buff, buyQty));
                totalCount += buyQty;
            }
        }

        if (currentTotalStock + totalCount > AfcConstants.MaxFcStockLimit)
        {
            var allowedToBuy = Math.Max(0, AfcConstants.MaxFcStockLimit - currentTotalStock);
            Diag($"Target purchase count ({totalCount}) exceeds max FC stock capacity (15). Current stock={currentTotalStock}. Capping purchases to {allowedToBuy}.");

            var cappedList = new List<(FcBuffDefinition Buff, int BuyQty)>();
            var remainingAllowed = allowedToBuy;

            foreach (var item in result)
            {
                if (remainingAllowed <= 0) break;
                var qty = Math.Min(item.BuyQty, remainingAllowed);
                cappedList.Add((item.Buff, qty));
                remainingAllowed -= qty;
            }

            totalCount = allowedToBuy;
            return cappedList;
        }

        return result;
    }
}
