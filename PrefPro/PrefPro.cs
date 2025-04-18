using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.Text;
using FFXIVClientStructs.STD;
using InteropGenerator.Runtime;
using Lumina.Text;
using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;
using System;

namespace PrefPro;

public unsafe class PrefPro : IDalamudPlugin
{
    private const string CommandName = "/prefpro";

    private readonly Configuration _configuration;
    private readonly PluginUI _ui;
    private readonly LuaHandler _luaHandler;

    private readonly Hook<TextModule.Delegates.FormatString> _formatStringHook;

    private delegate int GetCutVoGenderDelegate(nint a1, nint a2);
    private readonly Hook<GetCutVoGenderDelegate> _getCutVoGenderHook;

    private delegate byte GetLuaVarDelegate(nint poolBase, nint a2, nint a3);
    private readonly Hook<GetLuaVarDelegate> _getLuaVarHook;

    public readonly NameHandlerCache NameHandlerCache;

    public PrefPro(IDalamudPluginInterface pi)
    {
        DalamudApi.Initialize(pi);

        _configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        _configuration.Initialize(this);

        _ui = new PluginUI(_configuration);
        _luaHandler = new LuaHandler(_configuration);
        NameHandlerCache = new NameHandlerCache(_configuration);

        DalamudApi.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Display the PrefPro configuration interface.",
        });

        _formatStringHook = DalamudApi.Hooks.HookFromSignature<TextModule.Delegates.FormatString>(
            "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 83 B9 ?? ?? ?? ?? ?? 49 8B F9 49 8B F0 48 8B EA 48 8B D9 75 09 48 8B 01 FF 90",
            FormatStringDetour);

        _getCutVoGenderHook = DalamudApi.Hooks.HookFromSignature<GetCutVoGenderDelegate>(
            "E8 ?? ?? ?? ?? 49 8B 17 85 DB",
            GetCutVoGenderDetour);

        _getLuaVarHook = DalamudApi.Hooks.HookFromSignature<GetLuaVarDelegate>(
            "E8 ?? ?? ?? ?? 48 85 DB 74 1B 48 8D 8F",
            GetLuaVarDetour);

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

        _formatStringHook.Enable();
        _getCutVoGenderHook.Enable();
        _getLuaVarHook.Enable();
    }

    public void Dispose()
    {
        DalamudApi.CommandManager.RemoveHandler(CommandName);

        _formatStringHook?.Disable();
        _formatStringHook?.Dispose();
        _getCutVoGenderHook?.Disable();
        _getCutVoGenderHook?.Dispose();
        _luaHandler?.Dispose();
        _getLuaVarHook?.Dispose();

        GC.SuppressFinalize(this);
    }

    private void OnCommand(string command, string args)
    {
        _ui.SettingsVisible = true;
    }

    /**
     * This function is still necessary because of the name options provided in earlier versions.
     * So, we will never really be able to get rid of the string parsing in PrefPro.
     */
    private bool FormatStringDetour(TextModule* thisPtr, CStringPointer input, StdDeque<TextParameter>* localParameters, Utf8String* output)
    {
        if (!_configuration.Enabled)
            goto originalFormatString;

        var data = NameHandlerCache.GetConfig();
        if (!data.Apply)
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

        var sb = SeStringBuilder.SharedPool.Get();

        try
        {
            foreach (var payload in seString)
            {
                if (data.ApplyFull && ShouldHandleStringPayload(payload))
                {
                    sb.Append(data.NameFull);
                }
                else if (data.ApplyFirst && ShouldHandleSplitPayload(payload, 1))
                {
                    sb.Append(data.NameFirst);
                }
                else if (data.ApplyLast && ShouldHandleSplitPayload(payload, 2))
                {
                    sb.Append(data.NameLast);
                }
                else
                {
                    sb.Append(payload);
                }
            }

            fixed (byte* newInput = sb.GetViewAsSpan())
                return _formatStringHook.Original(thisPtr, newInput, localParameters, output);
        }
        catch (Exception ex)
        {
            DalamudApi.PluginLog.Error(ex, "PrefPro Exception");
        }
        finally
        {
            SeStringBuilder.SharedPool.Return(sb);

            raceParam.IntValue = oldRace;
            genderParam.IntValue = oldGender;
        }

    originalFormatString:
        return _formatStringHook.Original(thisPtr, input, localParameters, output);
    }

    private byte GetLuaVarDetour(nint poolBase, nint a2, nint a3)
    {
        var oldGender = GetLuaVarGender(poolBase);
        var newGender = _configuration.GetGender();
        SetLuaVarGender(poolBase, newGender);
        var returnValue = _getLuaVarHook.Original(poolBase, a2, a3);
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
        // DalamudApi.PluginLog.Verbose($"[GetCutVoGenderDetour] original returned {originalRet}");

        if (!_configuration.Enabled)
            return originalRet;

        // see Client::System::Framework::EnvironmentManager.GetCutsceneLanguage
        var lang = (uint)Framework.Instance()->EnvironmentManager->CutsceneMovieVoice;
        if (lang == uint.MaxValue)
            lang = DalamudApi.GameConfig.System.GetUInt("CutsceneMovieVoice");
        if (lang == uint.MaxValue)
            lang = DalamudApi.GameConfig.System.GetUInt("Language");

        // DalamudApi.PluginLog.Verbose($"[GetCutVoGenderDetour] Lang returned {lang}");

        if (*(int*)(a2 + *(int*)(a2 + 0x1C) + (12 * lang)) != 1)
            return _configuration.GetGender();

        return 0;
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
}