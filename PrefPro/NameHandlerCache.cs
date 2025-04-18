using PrefPro.Settings;
using System;

namespace PrefPro;

public class NameHandlerCache : IDisposable
{
    private readonly Configuration _configuration;

    private HandlerConfig _config = HandlerConfig.None;

    public NameHandlerCache(Configuration configuration)
    {
        _configuration = configuration;

        DalamudApi.ClientState.Login += OnLogin;
        DalamudApi.ClientState.Logout += OnLogout;
    }

    public void Dispose()
    {
        DalamudApi.ClientState.Login -= OnLogin;
        DalamudApi.ClientState.Logout -= OnLogout;

        GC.SuppressFinalize(this);
    }

    public HandlerConfig GetConfig()
    {
        if (_config == HandlerConfig.None)
            Refresh();

        return _config;
    }

    private void OnLogin()
    {
        Refresh();
    }

    private void OnLogout(int type, int code)
    {
        _config = HandlerConfig.None;
    }

    public void Refresh()
    {
        if (DalamudApi.ClientState.IsLoggedIn)
            _config = CreateConfig(_configuration);
    }

    private static HandlerConfig CreateConfig(Configuration config)
    {
        var data = new HandlerConfig();
        var playerName = PlayerApi.CharacterName;

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

            data.NameFull = GetNameText(playerName, config.Name, config.FullName);
            data.NameFirst = GetNameText(playerName, config.Name, config.FirstName);
            data.NameLast = GetNameText(playerName, config.Name, config.LastName);
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
    public string NameFull = string.Empty;
    public string NameFirst = string.Empty;
    public string NameLast = string.Empty;

    public static readonly HandlerConfig None = new()
    {
        Apply = false
    };
}
