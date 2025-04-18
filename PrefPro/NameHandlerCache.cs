using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using PrefPro.Settings;

namespace PrefPro;

public class NameHandlerCache
{
    private readonly Configuration _configuration;
    public string? PlayerName;

    public HandlerConfig Config { get; private set; } = HandlerConfig.None;

    public NameHandlerCache(Configuration configuration)
    {
        _configuration = configuration;
        DalamudApi.ClientState.Logout += OnLogout;
        DalamudApi.Framework.Update += FrameworkOnUpdate;
    }

    private void FrameworkOnUpdate(IFramework framework)
    {
        if (DalamudApi.ClientState.IsLoggedIn && DalamudApi.ClientState.LocalPlayer is { } localPlayer) {
            DalamudApi.Framework.Update -= FrameworkOnUpdate;
            PlayerName = localPlayer.Name.TextValue;
            Refresh();
        }
    }

    private void OnLogout(int type, int code)
    {
        if (PlayerName != null) {
            PlayerName = null;
            Config = HandlerConfig.None;
            DalamudApi.Framework.Update += FrameworkOnUpdate;
        }
    }

    public void Refresh()
    {
        if (PlayerName != null) {
            Config = CreateConfig(_configuration, PlayerName);
        }
    }

    private static HandlerConfig CreateConfig(Configuration config, string playerName)
    {
        var data = new HandlerConfig();

        if (string.IsNullOrEmpty(config.Name))
        {
            return HandlerConfig.None;
        }

        if (config.Name != playerName)
        {
            data.ApplyFull = true;
            data.ApplyFirst = true;
            data.ApplyLast = true;
        }
        else
        {
            data.ApplyFull = config.FullName != NameSetting.FirstLast;
            data.ApplyFirst = config.FirstName != NameSetting.FirstOnly;
            data.ApplyLast = config.LastName != NameSetting.LastOnly;
        }

        if (data is { ApplyFirst: false, ApplyLast: false, ApplyFull: false })
        {
            data.Apply = false;
        }
        else
        {
            data.Apply = true;

            data.NameFull = new TextPayload(GetNameText(playerName, config.Name, config.FullName));
            data.NameFirst = new TextPayload(GetNameText(playerName, config.Name, config.FirstName));
            data.NameLast = new TextPayload(GetNameText(playerName, config.Name, config.LastName));
        }

        return data;
    }

    private static string GetNameText(string playerName, string configName, NameSetting setting)
    {
        switch (setting)
        {
            case NameSetting.FirstLast:
                return configName;
            case NameSetting.FirstOnly:
                return configName.Split(' ')[0];
            case NameSetting.LastOnly:
                return configName.Split(' ')[1];
            case NameSetting.LastFirst:
                var split = configName.Split(' ');
                return $"{split[1]} {split[0]}";
            default:
                return playerName;
        }
    }
}

public class HandlerConfig
{
    public bool Apply;
    public bool ApplyFull;
    public bool ApplyFirst;
    public bool ApplyLast;
    public TextPayload NameFull = null!;
    public TextPayload NameFirst = null!;
    public TextPayload NameLast = null!;

    public static readonly HandlerConfig None = new()
    {
        Apply = false
    };
}
