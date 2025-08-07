using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.STD;
using PrefPro.Settings;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PrefPro;

public sealed unsafe class StringHandler : IDisposable
{
    //reEncode[1] == 0x29 && reEncode[2] == 0x3 && reEncode[3] == 0xEB && reEncode[4] == 0x2
    private static readonly byte[] FullNameBytes = [0x02, 0x29, 0x03, 0xEB, 0x02, 0x03];
    private static readonly byte[] FirstNameBytes = [0x02, 0x2C, 0x0D, 0xFF, 0x07, 0x02, 0x29, 0x03, 0xEB, 0x02, 0x03, 0xFF, 0x02, 0x20, 0x02, 0x03];
    private static readonly byte[] LastNameBytes = [0x02, 0x2C, 0x0D, 0xFF, 0x07, 0x02, 0x29, 0x03, 0xEB, 0x02, 0x03, 0xFF, 0x02, 0x20, 0x03, 0x03];

    private delegate int GetStringPrototype(RaptureTextModule* textModule, byte* text, void* decoder, Utf8String* stringStruct);
    private readonly Hook<GetStringPrototype>? _getStringHook;

    private readonly LoginState _loginState;
    private readonly Configuration _configuration;

    private HandlerConfig _handlerConfig = HandlerConfig.None;

    public StringHandler(LoginState loginState, Configuration configuration)
    {
        _loginState = loginState;
        _configuration = configuration;

        var getStringStr = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 83 B9 ?? ?? ?? ?? ?? 49 8B F9 49 8B F0 48 8B EA 48 8B D9 75 09 48 8B 01 FF 90";
        if (DalamudApi.SigScanner.TryScanText(getStringStr, out var getStringPtr))
        {
            _getStringHook = DalamudApi.Hooks.HookFromAddress<GetStringPrototype>(getStringPtr, GetStringDetour);
        }
        else
        {
            DalamudApi.PluginLog.Error("Failed to hook GetStringPrototype.");
            return;
        }

        loginState.Login += OnLogin;
        loginState.Logout += OnLogout;
    }

    private void OnLogin()
    {
        DalamudApi.PluginLog.Debug("Enabling StringHandler");
        RefreshConfig();
        _getStringHook?.Enable();
    }

    private void OnLogout()
    {
        DalamudApi.PluginLog.Debug("Disabling StringHandler");
        RefreshConfig();
        _getStringHook?.Disable();
    }

    public void Dispose()
    {
        _getStringHook?.Dispose();
    }

    private int GetStringDetour(RaptureTextModule* raptureTextModule, byte* text, void* unknown2, Utf8String* output)
    {
        // DalamudApi.PluginLog.Verbose($"[getStringDetour] {(nuint)raptureTextModule:X2} {(nuint)text:X2} {(nuint)unknown2:X2} {(nuint)stringStruct:X2}");
        if (!_configuration.Enabled)
            return _getStringHook!.Original(raptureTextModule, text, unknown2, output);

        var decoderParams = ***(StdDeque<UnknownStruct>***)((nuint)RaptureTextModule.Instance() + 0x40);

        var raceParam = decoderParams[70];
        var oldRace = raceParam.Value;
        if (raceParam.Self == 0)
            return _getStringHook!.Original(raptureTextModule, text, unknown2, output);
        var racePtr = (ulong*)raceParam.Self;
        *racePtr = (ulong)_configuration.Race;

        var genderParam = decoderParams[3];
        var oldGender = genderParam.Value;
        if (genderParam.Self == 0)
            return _getStringHook!.Original(raptureTextModule, text, unknown2, output);
        var genderPtr = (ulong*)genderParam.Self;
        *genderPtr = (ulong)_configuration.GetGender();

        HandleName(ref text);
        var result = _getStringHook!.Original(raptureTextModule, text, unknown2, output);
        // Marshal.FreeHGlobal((IntPtr)text);

        raceParam = decoderParams[70];
        racePtr = (ulong*)raceParam.Self;
        *racePtr = oldRace;

        genderParam = decoderParams[3];
        genderPtr = (ulong*)genderParam.Self;
        *genderPtr = oldGender;

        return result;
    }

    /**
     * This function is still necessary because of the name options provided in earlier versions.
     * So, we will never really be able to get rid of the string parsing in PrefPro.
     */
    private void HandleName(ref byte* ptr)
    {
        var data = _handlerConfig;
        if (!data.Apply)
            return;

        var bytes = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr);
        if (!bytes.Contains((byte)0x02))
            return;

        var parsed = SeString.Parse(ptr, bytes.Length);

        var payloads = parsed.Payloads;
        var numPayloads = payloads.Count;
        var replaced = false;
        for (var payloadIndex = 0; payloadIndex < numPayloads; payloadIndex++)
        {
            var payload = payloads[payloadIndex];
            if (payload.Type == PayloadType.Unknown)
            {
                var payloadBytes = payload.Encode();
                if (data.ApplyFull && ByteArrayEquals(payloadBytes, FullNameBytes))
                {
                    payloads[payloadIndex] = data.NameFull;
                    replaced = true;
                }
                else if (data.ApplyFirst && ByteArrayEquals(payloadBytes, FirstNameBytes))
                {
                    payloads[payloadIndex] = data.NameFirst;
                    replaced = true;
                }
                else if (data.ApplyLast && ByteArrayEquals(payloadBytes, LastNameBytes))
                {
                    payloads[payloadIndex] = data.NameLast;
                    replaced = true;
                }
            }
        }

        if (!replaced) return;

        var src = parsed.EncodeWithNullTerminator();
        var srcLength = src.Length;
        var destLength = bytes.Length + 1; // Add 1 for null terminator not included in Span

        if (srcLength <= destLength)
        {
            src.CopyTo(new Span<byte>(ptr, destLength));
        }
        else
        {
            var newStr = (byte*)Marshal.AllocHGlobal(srcLength);
            src.CopyTo(new Span<byte>(newStr, srcLength));
            ptr = newStr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ByteArrayEquals(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
    {
        return a1.SequenceEqual(a2);
    }

    public void RefreshConfig()
    {
        if (!_loginState.LoggedIn || string.IsNullOrEmpty(_configuration.Name))
        {
            _handlerConfig = HandlerConfig.None;
        }
        else
        {
            _handlerConfig = CreateConfig(_configuration, _loginState.PlayerName);
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