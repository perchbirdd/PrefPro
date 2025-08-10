using System;
using System.Text;
using Dalamud.Hooking;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.Text;
using FFXIVClientStructs.STD;
using InteropGenerator.Runtime;
using Lumina.Text;
using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;
using PrefPro.Settings;

namespace PrefPro;

public sealed unsafe class StringHandler : IDisposable
{
    private readonly Hook<TextModule.Delegates.FormatString>? _formatStringHook;

    private readonly LoginState _loginState;
    private readonly Configuration _configuration;

    private NameHandlerConfig _nameHandlerConfig = NameHandlerConfig.None;

    public StringHandler(LoginState loginState, Configuration configuration)
    {
        _loginState = loginState;
        _configuration = configuration;

        var formatStringSig =
            "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 83 B9 ?? ?? ?? ?? ?? 49 8B F9 49 8B F0 48 8B EA 48 8B D9 75 09 48 8B 01 FF 90";
        if (DalamudApi.SigScanner.TryScanText(formatStringSig, out var getStringPtr))
        {
            _formatStringHook =
                DalamudApi.Hooks.HookFromAddress<TextModule.Delegates.FormatString>(getStringPtr, FormatStringDetour);
        }
        else
        {
            DalamudApi.PluginLog.Error("Failed to hook FormatString.");
            return;
        }

        loginState.Login += OnLogin;
        loginState.Logout += OnLogout;
    }

    private void OnLogin()
    {
        DalamudApi.PluginLog.Debug("Enabling StringHandler");
        RefreshConfig();
        _formatStringHook?.Enable();
    }

    private void OnLogout()
    {
        DalamudApi.PluginLog.Debug("Disabling StringHandler");
        RefreshConfig();
        _formatStringHook?.Disable();
    }

    public void Dispose()
    {
        _formatStringHook?.Dispose();
    }

    /**
     * This function is still necessary because of the name options provided in earlier versions.
     * So, we will never really be able to get rid of the string parsing in PrefPro.
     */
    private bool FormatStringDetour(TextModule* thisPtr, CStringPointer input, StdDeque<TextParameter>* localParameters,
        Utf8String* output)
    {
        if (!_configuration.Enabled)
            goto originalFormatString;

        var seString = input.AsReadOnlySeStringSpan();
        if (seString.IsEmpty || seString.IsTextOnly())
            goto originalFormatString;

        ref var decoderParams = ref thisPtr->GlobalParameters;
        if (decoderParams.Count < 70)
            goto originalFormatString;

        ref var raceParam = ref decoderParams[70];
        if (raceParam.ValuePtr == null)
            goto originalFormatString;

        ref var genderParam = ref decoderParams[3];
        if (genderParam.ValuePtr == null)
            goto originalFormatString;

        var oldRace = raceParam.IntValue;
        raceParam.IntValue = (int)_configuration.Race;

        var oldGender = genderParam.IntValue;
        genderParam.IntValue = _configuration.GetGender();

        var nameConfig = _nameHandlerConfig;
        try
        {
            if (nameConfig.Apply)
            {
                var sb = SeStringBuilder.SharedPool.Get();
                try
                {
                    foreach (var payload in seString)
                    {
                        if (nameConfig.ApplyFull && ShouldHandleStringPayload(payload))
                        {
                            sb.Append(nameConfig.NameFull);
                        }
                        else if (nameConfig.ApplyFirst && ShouldHandleSplitPayload(payload, 1))
                        {
                            sb.Append(nameConfig.NameFirst);
                        }
                        else if (nameConfig.ApplyLast && ShouldHandleSplitPayload(payload, 2))
                        {
                            sb.Append(nameConfig.NameLast);
                        }
                        else
                        {
                            sb.Append(payload);
                        }
                    }
                    fixed (byte* newInput = sb.GetViewAsSpan())
                        return _formatStringHook.Original(thisPtr, newInput, localParameters, output);
                }
                finally
                {
                    SeStringBuilder.SharedPool.Return(sb);
                }
            }
            else
            {
                return _formatStringHook.Original(thisPtr, input, localParameters, output);
            }
        }
        catch (Exception ex)
        {
            DalamudApi.PluginLog.Error(ex, "PrefPro Exception");
        }
        finally
        {
            raceParam.IntValue = oldRace;
            genderParam.IntValue = oldGender;
        }

        originalFormatString:
        return _formatStringHook.Original(thisPtr, input, localParameters, output);
    }

    public void RefreshConfig()
    {
        if (!_loginState.LoggedIn || string.IsNullOrEmpty(_configuration.Name))
        {
            _nameHandlerConfig = NameHandlerConfig.None;
        }
        else
        {
            _nameHandlerConfig = CreateConfig(_configuration, _loginState.PlayerName);
        }
    }

    private static NameHandlerConfig CreateConfig(Configuration config, string playerName)
    {
        var data = new NameHandlerConfig();

        if (string.IsNullOrEmpty(config.Name))
        {
            return NameHandlerConfig.None;
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

            var nameFull = GetNameText(playerName, config.Name, config.FullName);
            var nameFirst = GetNameText(playerName, config.Name, config.FirstName);
            var nameLast = GetNameText(playerName, config.Name, config.LastName);

            data.NameFull = MakePayload(nameFull);
            data.NameFirst = MakePayload(nameFirst);
            data.NameLast = MakePayload(nameLast);
        }

        return data;
    }

    private static ReadOnlySePayload MakePayload(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return new ReadOnlySePayload(ReadOnlySePayloadType.Text, macroCode: default, bytes);
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

    // <string(gstr1)> 
    private static bool ShouldHandleStringPayload(ReadOnlySePayloadSpan payload)
    {
        return payload.Type == ReadOnlySePayloadType.Macro
               && payload.MacroCode == MacroCode.String
               && payload.TryGetExpression(out var expr1)
               && expr1.TryGetParameterExpression(out var expressionType, out var operand)
               && expressionType == (byte)ExpressionType.GlobalString
               && operand.TryGetInt(out var gstrIndex)
               && gstrIndex == 1;
    }

    // <split(<string(gstr1)>, ,index)> 
    private static bool ShouldHandleSplitPayload(ReadOnlySePayloadSpan payload, int splitIndex)
    {
        if (payload.Type == ReadOnlySePayloadType.Macro
            && payload.MacroCode == MacroCode.Split
            && payload.TryGetExpression(out var expr1, out var _, out var expr3)
            && expr1.TryGetString(out var text)
            && text.PayloadCount == 1
            && expr3.TryGetInt(out var index)
            && index == splitIndex)
        {
            var enu = text.GetEnumerator();
            return enu.MoveNext() && ShouldHandleStringPayload(enu.Current);
        }

        return false;
    }

    public class NameHandlerConfig
    {
        public bool Apply;
        public bool ApplyFull;
        public bool ApplyFirst;
        public bool ApplyLast;
        public ReadOnlySePayload? NameFull;
        public ReadOnlySePayload? NameFirst;
        public ReadOnlySePayload? NameLast;

        public static readonly NameHandlerConfig None = new()
        {
            Apply = false
        };
    }
}