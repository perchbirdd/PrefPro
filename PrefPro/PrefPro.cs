using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace PrefPro;

public class PrefPro : IDalamudPlugin
{
    private const string CommandName = "/prefpro";

    private readonly PluginUI _ui;
    
    private readonly LoginState _loginState;
    private readonly LuaHandler _luaHandler;
    private readonly GenderHandler _genderHandler;
    private readonly StringHandler _stringHandler;
    
    public PrefPro(IDalamudPluginInterface pi)
    {
        DalamudApi.Initialize(pi);

        _loginState = new LoginState();

        var configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        configuration.Initialize(this);

        _luaHandler = new LuaHandler(_loginState, configuration);
        _stringHandler = new StringHandler(_loginState, configuration);
        _genderHandler = new GenderHandler(_loginState, configuration);

        _ui = new PluginUI(configuration);

        DalamudApi.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Display the PrefPro configuration interface.",
        });
        
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
    
    public void OnConfigSave()
    {
        _stringHandler.RefreshConfig();
    }
}