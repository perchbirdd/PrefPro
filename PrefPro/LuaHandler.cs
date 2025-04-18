using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;

namespace PrefPro;

public unsafe class LuaHandler : IDisposable
{
    public delegate nuint LuaFunction(nuint a1);

    private readonly Configuration _configuration;

    private Hook<LuaFunction>? _getRace;
    private Hook<LuaFunction>? _getSex;
    private Hook<LuaFunction>? _getTribe;

    private bool _initialized = false;

    public LuaHandler(Configuration configuration)
    {
        _configuration = configuration;

        try
        {
            Initialize();
        }
        catch (Exception e)
        {
            DalamudApi.PluginLog.Error(e, "PrefPro Lua Handler initialization failed.");
        }
    }

    private void Initialize()
    {
        if (_initialized) return;

        var raceFunctionAddress = GetAddress("return Pc.GetRace");
        var sexFunctionAddress = GetAddress("return Pc.GetSex");
        var tribeFunctionAddress = GetAddress("return Pc.GetTribe");

        _getRace = DalamudApi.Hooks.HookFromAddress<LuaFunction>(raceFunctionAddress, RaceFunctionDetour);
        _getSex = DalamudApi.Hooks.HookFromAddress<LuaFunction>(sexFunctionAddress, SexFunctionDetour);
        _getTribe = DalamudApi.Hooks.HookFromAddress<LuaFunction>(tribeFunctionAddress, TribeFunctionDetour);

        DalamudApi.PluginLog.Debug($"[LuaHandler] Race function address: {raceFunctionAddress:X}");
        DalamudApi.PluginLog.Debug($"[LuaHandler] Sex function address: {sexFunctionAddress:X}");
        DalamudApi.PluginLog.Debug($"[LuaHandler] Tribe function address: {tribeFunctionAddress:X}");

        _getRace.Enable();
        _getSex.Enable();
        _getTribe.Enable();

        _initialized = true;
    }

    public void Dispose()
    {
        _getRace?.Dispose();
        _getSex?.Dispose();
        _getTribe?.Dispose();

        GC.SuppressFinalize(this);
    }

    private nint GetAddress(string code)
    {
        var l = Framework.Instance()->LuaState.State;
        l->luaL_loadbuffer(code, code.Length, "test_chunk");
        if (l->lua_pcall(0, 1, 0) != 0)
            throw new Exception(l->lua_tostring(-1));
        var luaFunc = *(nint*)l->index2adr(-1);
        l->lua_pop(1);
        return *(nint*)(luaFunc + 0x20);
    }

    private nuint RaceFunctionDetour(nuint a1)
    {
        try
        {
            Initialize();
            var oldRace = PlayerApi.Race;
            PlayerApi.Race = (byte)_configuration.Race;
            DalamudApi.PluginLog.Debug($"[RaceFunctionDetour] oldRace: {oldRace} race: {(byte)_configuration.Race}");
            var ret = _getRace!.Original(a1);
            PlayerApi.Race = oldRace;
            return ret;
        }
        catch (Exception e)
        {
            DalamudApi.PluginLog.Error(e, "PrefPro Exception");
            return _getRace!.Original(a1);
        }
    }

    private nuint SexFunctionDetour(nuint a1)
    {
        try
        {
            Initialize();
            var oldSex = PlayerApi.Sex;
            PlayerApi.Sex = (byte)_configuration.GetGender();
            DalamudApi.PluginLog.Debug($"[SexFunctionDetour] oldSex: {oldSex} sex: {(byte)_configuration.GetGender()}");
            var ret = _getSex!.Original(a1);
            PlayerApi.Sex = oldSex;
            return ret;
        }
        catch (Exception e)
        {
            DalamudApi.PluginLog.Error(e, "PrefPro Exception");
            return _getSex!.Original(a1);
        }
    }

    private nuint TribeFunctionDetour(nuint a1)
    {
        try
        {
            Initialize();
            var oldTribe = PlayerApi.Tribe;
            PlayerApi.Tribe = (byte)_configuration.Tribe;
            DalamudApi.PluginLog.Debug($"[TribeFunctionDetour] oldTribe: {oldTribe} tribe: {(byte)_configuration.Tribe}");
            var ret = _getTribe!.Original(a1);
            PlayerApi.Tribe = oldTribe;
            return ret;
        }
        catch (Exception e)
        {
            DalamudApi.PluginLog.Error(e, "PrefPro Exception");
            return _getTribe!.Original(a1);
        }
    }
}