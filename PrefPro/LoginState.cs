using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;

namespace PrefPro;

public class LoginState
{
    public bool LoggedIn;
    public string PlayerName = "";

    public event Action? Login;
    public event Action? Logout;

    public void Start()
    {
        DalamudApi.ClientState.Login += OnLogin;
        DalamudApi.ClientState.Logout += OnLogout;

        if (DalamudApi.ClientState.IsLoggedIn) {
            OnLogin();
        }
    }

    private unsafe void OnLogin()
    {
        if (LoggedIn) return;

        var playerState = PlayerState.Instance();
        if (playerState->IsLoaded == 1) {
            var name = playerState->CharacterNameString;
            if (name.Length > 0) {
                PlayerName = name;
                LoggedIn = true;
                Login?.Invoke();
                return;
            }
        }

        DalamudApi.PluginLog.Error("Login failed due to missing player information.");
    }

    private void OnLogout(int type, int code)
    {
        if (!LoggedIn) return;

        PlayerName = "";
        LoggedIn = false;
        Logout?.Invoke();
    }
}