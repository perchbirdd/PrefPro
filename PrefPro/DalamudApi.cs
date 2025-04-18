using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace PrefPro;

public class DalamudApi
{
    public static void Initialize(IDalamudPluginInterface pluginInterface) => pluginInterface.Create<DalamudApi>();

    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider Hooks { get; private set; } = null!;
    [PluginService] public static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] public static IPluginLog PluginLog { get; private set; } = null!;
}