using AutoFCBuff.Core;
using AutoFCBuff.Core.Debug;
using AutoFCBuff.Core.Game;
using AutoFCBuff.Core.Tasks;
using AutoFCBuff.Windows;
using clib;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;

namespace AutoFCBuff;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;

    internal Configuration Configuration { get; }
    internal static Configuration Cfg { get; private set; } = null!;
    internal WindowSystem WindowSystem { get; } = new("AutoFCBuff");
    internal AutoFcBuffController Controller { get; }

    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private readonly AboutWindow aboutWindow;
    private readonly DependenciesWindow dependenciesWindow;
    private readonly IconPickerWindow iconPickerWindow;

    public Plugin()
    {
        ECommonsMain.Init(PluginInterface, this);
        CLibMain.Init(PluginInterface, this, CLibModule.Automation);

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Cfg = Configuration;
        Controller = new AutoFcBuffController();

        mainWindow = new MainWindow(this);
        configWindow = new ConfigWindow(this);
        aboutWindow = new AboutWindow();
        dependenciesWindow = new DependenciesWindow();
        iconPickerWindow = new IconPickerWindow();

        WindowSystem.AddWindow(mainWindow);
        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(aboutWindow);
        WindowSystem.AddWindow(dependenciesWindow);
        WindowSystem.AddWindow(iconPickerWindow);

        CommandManager.AddHandler(AfcConstants.PrimaryCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the Auto FC Buffs window. /afc config | deps | about | target | debug | icons"
        });
        CommandManager.AddHandler(AfcConstants.AliasCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = "Alias for /afc."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();
        mainWindow.Dispose();
        configWindow.Dispose();
        aboutWindow.Dispose();
        dependenciesWindow.Dispose();

        CommandManager.RemoveHandler(AfcConstants.PrimaryCommand);
        CommandManager.RemoveHandler(AfcConstants.AliasCommand);

        Controller.Dispose();
        CLibMain.Dispose();
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim();
        if (trimmed.Equals("config", StringComparison.OrdinalIgnoreCase))
            ToggleConfigUi();
        else if (trimmed.Equals("about", StringComparison.OrdinalIgnoreCase))
            ToggleAboutUi();
        else if (trimmed.Equals("deps", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("dependencies", StringComparison.OrdinalIgnoreCase))
            ToggleDependenciesUi();
        else if (trimmed.Equals("target", StringComparison.OrdinalIgnoreCase))
            TargetDumper.Dump();
        else if (trimmed.Equals("icons", StringComparison.OrdinalIgnoreCase))
            ToggleIconPickerUi();
        else if (trimmed.Equals("debug", StringComparison.OrdinalIgnoreCase))
        {
            AddonInspector.Dump();
            FcStockReader.LogMemoryInfo();
        }
        else
            ToggleMainUi();
    }

    public void ToggleMainUi() => mainWindow.Toggle();
    public void ToggleConfigUi() => configWindow.Toggle();
    public void ToggleAboutUi() => aboutWindow.Toggle();
    public void ToggleDependenciesUi() => dependenciesWindow.Toggle();
    public void ToggleIconPickerUi() => iconPickerWindow.Toggle();
}
