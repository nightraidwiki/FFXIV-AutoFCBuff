using System;
using System.Collections.Generic;
using AutoFCBuff.Core;
using AutoFCBuff.Core.Game;
using Dalamud.Configuration;
using ECommons.Throttlers;

namespace AutoFCBuff;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public GcChoice SelectedGcChoice { get; set; } = GcChoice.Auto;

    // By default, show Grade II buffs only (false = Grade 2 only, true = Grade 1 & 2)
    public bool ShowGrade1Buffs { get; set; } = false;

    // Target stock desired by the user per FC Buff Id (e.g. Buff ID 2 -> 5)
    public Dictionary<uint, int> TargetStock { get; set; } = [];

    // Current stock stored/detected in FC chest (e.g. Buff ID 2 -> 2)
    public Dictionary<uint, int> CurrentStock { get; set; } = [];

    // Auto-activation settings (Default FALSE = Never activate automatically unless checked)
    public bool EnableAutoActivation { get; set; } = false;
    public uint TargetActiveBuff1 { get; set; } = 0;
    public uint TargetActiveBuff2 { get; set; } = 0;

    // Currently active FC buff names detected on player/FC
    public List<string> ActiveFcBuffNames { get; set; } = [];

    public string PostRunCommands { get; set; } = string.Empty;

    public bool AutoShowUi { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    public void SaveDebounced()
    {
        if (EzThrottler.Throttle(AfcConstants.ThrottleKeys.Save, AfcConstants.SaveThrottleMs))
            Save();
    }
}
