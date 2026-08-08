using AutoFCBuff.Windows.Sections;
using Dalamud.Interface.Windowing;

namespace AutoFCBuff.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin) : base("Auto FC Buffs##AutoFCBuffMainWindow")
    {
        this.plugin = plugin;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new System.Numerics.Vector2(650, 600),
            MaximumSize = new System.Numerics.Vector2(1200, 1000)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        var ctrl = plugin.Controller;

        using var style = Styling.PushWindowStyle();

        HeaderStrip.Draw(plugin);
        DependencyBanner.Draw(plugin);

        if (ctrl.Running)
        {
            RunningPanel.Draw(ctrl);
        }
        else
        {
            SetupPanel.Draw(cfg, ctrl.Running, () => ctrl.Start(cfg));
        }
    }
}
