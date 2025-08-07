using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;
using System.Runtime.InteropServices;

namespace PrefPro;

public sealed unsafe class GenderHandler: IDisposable
{
    private delegate int GetCutVoGenderPrototype(nint a1, nint a2);
    private readonly Hook<GetCutVoGenderPrototype>? _getCutVoGenderHook;

    private delegate int GetCutVoLangPrototype(nint a1);
    private readonly GetCutVoLangPrototype? _getCutVoLang;

    private delegate byte GetLuaVarPrototype(nint poolBase, nint a2, nint a3);
    private readonly Hook<GetLuaVarPrototype>? _getLuaVarHook;

    private readonly Configuration _configuration;

    public GenderHandler(LoginState loginState, Configuration configuration)
    {
        _configuration = configuration;

        var getCutVoGender = "E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 8B D8 48 8B 89 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 63 4F 24";
        if (DalamudApi.SigScanner.TryScanText(getCutVoGender, out var getCutVoGenderPtr))
        {
            _getCutVoGenderHook = DalamudApi.Hooks.HookFromAddress<GetCutVoGenderPrototype>(getCutVoGenderPtr, GetCutVoGenderDetour);
        }
        else
        {
            DalamudApi.PluginLog.Error("Failed to hook GetCutVoGenderPrototype.");
            return;
        }

        var getCutVoLang = "E8 ?? ?? ?? ?? 49 63 56 1C";
        if (DalamudApi.SigScanner.TryScanText(getCutVoLang, out var getCutVoLangPtr))
        {
            _getCutVoLang = Marshal.GetDelegateForFunctionPointer<GetCutVoLangPrototype>(getCutVoLangPtr);
        }
        else
        {
            DalamudApi.PluginLog.Error("Failed to hook GetCutVoLangPrototype.");
            return;
        }

        var getLuaVar = "E8 ?? ?? ?? ?? 48 85 DB 74 1B 48 8D 8F";
        if (DalamudApi.SigScanner.TryScanText(getLuaVar, out var getLuaVarPtr))
        {
            _getLuaVarHook = DalamudApi.Hooks.HookFromAddress<GetLuaVarPrototype>(getLuaVarPtr, GetLuaVarDetour);
        }
        else
        {
            DalamudApi.PluginLog.Error("Failed to hook GetLuaVarPrototype.");
            return;
        }

        loginState.Login += OnLogin;
        loginState.Logout += OnLogout;
    }

    private void OnLogin()
    {
        DalamudApi.PluginLog.Debug("Enabling GenderHandler");
        _getCutVoGenderHook?.Enable();
        _getLuaVarHook?.Enable();
    }

    private void OnLogout()
    {
        DalamudApi.PluginLog.Debug("Disabling GenderHandler");
        _getCutVoGenderHook?.Disable();
        _getLuaVarHook?.Disable();
    }

    public void Dispose()
    {
        _getCutVoGenderHook?.Dispose();
        _getLuaVarHook?.Dispose();
    }

    private byte GetLuaVarDetour(nint poolBase, IntPtr a2, IntPtr a3)
    {
        var oldGender = GetLuaVarGender(poolBase);
        var newGender = _configuration.GetGender();
        SetLuaVarGender(poolBase, newGender);
        var returnValue = _getLuaVarHook!.Original(poolBase, a2, a3);
        SetLuaVarGender(poolBase, oldGender);
        return returnValue;
    }

    private int GetLuaVarGender(nint poolBase)
    {
        var genderVarId = 0x1B;
        var gender = *(int*)(poolBase + 4 * genderVarId);
        return gender;
    }

    private void SetLuaVarGender(nint poolBase, int gender)
    {
        var genderVarId = 0x1B;
        *(int*)(poolBase + 4 * genderVarId) = gender;
    }

    private int GetCutVoGenderDetour(nint a1, nint a2)
    {
        var originalRet = _getCutVoGenderHook.Original(a1, a2);

        if (!_configuration.Enabled)
            return originalRet;

        // see Client::System::Framework::EnvironmentManager.GetCutsceneLanguage
        var lang = (uint)Framework.Instance()->EnvironmentManager->CutsceneMovieVoice;
        if (lang == uint.MaxValue)
            lang = DalamudApi.GameConfig.System.GetUInt("CutsceneMovieVoice");
        if (lang == uint.MaxValue)
            lang = DalamudApi.GameConfig.System.GetUInt("Language");

        if (*(int*)(a2 + *(int*)(a2 + 0x1C) + (12 * lang)) != 1)
            return _configuration.GetGender();

        return 0;
    }
}
