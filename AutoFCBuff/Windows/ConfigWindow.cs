using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace AutoFCBuff.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin) : base("Auto FC Buffs - Settings###AutoFCBuffConfig")
    {
        this.plugin = plugin;
        Size = new Vector2(500, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;

        using var style = Styling.PushWindowStyle();

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.TextUnformatted("General Settings");
        }
        ImGui.Separator();

        var autoShow = cfg.AutoShowUi;
        if (ImGui.Checkbox("Auto-show UI on plugin load", ref autoShow))
        {
            cfg.AutoShowUi = autoShow;
            cfg.SaveDebounced();
        }

        Styling.VSpace(10);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.TextUnformatted("Post-Run Chat Commands");
        }
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
        {
            ImGui.TextUnformatted("Commands dispatched line-by-line after completing a restock run (e.g. /ays m):");
        }

        var postCmds = cfg.PostRunCommands;
        if (ImGui.InputTextMultiline("##PostRunCmds", ref postCmds, 1000, new Vector2(-1, 100)))
        {
            cfg.PostRunCommands = postCmds;
            cfg.SaveDebounced();
        }
    }
}
