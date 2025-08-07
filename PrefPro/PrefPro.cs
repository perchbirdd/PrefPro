using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects.Enums;
using PrefPro.Settings;

namespace PrefPro;

public class PrefPro : IDalamudPlugin
{
    private const string CommandName = "/prefpro";

    private readonly PluginUI _ui;
    private readonly LoginState _loginState;

    private readonly LuaHandler _luaHandler;
    private readonly GenderHandler _genderHandler;
    private readonly StringHandler _stringHandler;

    public string? PlayerName => _loginState.LoggedIn ? _loginState.PlayerName : null;
    public int PlayerGender => DalamudApi.ClientState.LocalPlayer?.Customize[(int)CustomizeIndex.Gender] ?? 0;
    public RaceSetting PlayerRace => (RaceSetting)(DalamudApi.ClientState.LocalPlayer?.Customize[(int)CustomizeIndex.Race] ?? 0);
    public TribeSetting PlayerTribe => (TribeSetting)(DalamudApi.ClientState.LocalPlayer?.Customize[(int)CustomizeIndex.Tribe] ?? 0);

    public ulong CurrentPlayerContentId => DalamudApi.ClientState.LocalContentId;

    public PrefPro(IDalamudPluginInterface pi)
    {
        DalamudApi.Initialize(pi);

        _loginState = new LoginState();

        var configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        configuration.Initialize(this);

        _luaHandler = new LuaHandler(_loginState, configuration);
        _stringHandler = new StringHandler(_loginState, configuration);
        _genderHandler = new GenderHandler(_loginState, configuration);

        _ui = new PluginUI(configuration, this);

        DalamudApi.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Display the PrefPro configuration interface.",
        });

        // var frameworkLangCallOffsetStr = "48 8B 88 ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 63 7E 24";
        // var frameworkLangCallOffsetPtr = DalamudApi.SigScanner.ScanText(frameworkLangCallOffsetStr);
        // _frameworkLangCallOffset = *(uint*)(frameworkLangCallOffsetPtr + 3);
        // DalamudApi.PluginLog.Verbose($"framework lang call offset {_frameworkLangCallOffset} {_frameworkLangCallOffset:X}");

        // TODO: Include? no idea
        // if (frameworkLangCallOffset is < 10000 or > 14000)
        // {
        //     PluginLog.Error("Framework language call offset is invalid. The plugin will be disabled.");
        //     throw new InvalidOperationException();
        // }

        DalamudApi.PluginInterface.UiBuilder.Draw += DrawUI;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        DalamudApi.Framework.RunOnFrameworkThread(() => _loginState.Start());
    }

    public void Dispose()
    {
        _ui.Dispose();
        _luaHandler.Dispose();
        _genderHandler.Dispose();
        _stringHandler.Dispose();

        DalamudApi.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        _ui.SettingsVisible = true;
    }

    private void DrawUI()
    {
        _ui.Draw();
    }

    private void DrawConfigUI()
    {
        _ui.SettingsVisible = true;
    }

    public void OnConfigSave()
    {
        _stringHandler.RefreshConfig();
    }
}