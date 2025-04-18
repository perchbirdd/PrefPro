using Dalamud.Configuration;
using Newtonsoft.Json;
using PrefPro.Settings;
using System;
using System.Collections.Generic;

namespace PrefPro;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public struct ConfigHolder
    {
        public bool Enabled;
        public string Name;
        public NameSetting FullName;
        public NameSetting FirstName;
        public NameSetting LastName;
        public GenderSetting Gender;
        public RaceSetting Race;
        public TribeSetting Tribe;
    }

    public Dictionary<ulong, ConfigHolder> Configs { get; set; } = [];

    [JsonIgnore]
    public bool Enabled
    {
        get => GetOrDefault().Enabled;
        set
        {
            var config = GetOrDefault();
            config.Enabled = value;
            Configs[PlayerApi.ContentId] = config;
        }
    }
    [JsonIgnore]
    public string Name
    {
        get => GetOrDefault().Name;
        set
        {
            var config = GetOrDefault();
            config.Name = value;
            Configs[PlayerApi.ContentId] = config;
        }
    }
    [JsonIgnore]
    public NameSetting FullName
    {
        get => GetOrDefault().FullName;
        set
        {
            var config = GetOrDefault();
            config.FullName = value;
            Configs[PlayerApi.ContentId] = config;
        }
    }
    [JsonIgnore]
    public NameSetting FirstName
    {
        get => GetOrDefault().FirstName;
        set
        {
            var config = GetOrDefault();
            config.FirstName = value;
            Configs[PlayerApi.ContentId] = config;
        }
    }
    [JsonIgnore]
    public NameSetting LastName
    {
        get => GetOrDefault().LastName;
        set
        {
            var config = GetOrDefault();
            config.LastName = value;
            Configs[PlayerApi.ContentId] = config;
        }
    }
    [JsonIgnore]
    public GenderSetting Gender
    {
        get => GetOrDefault().Gender;
        set
        {
            var config = GetOrDefault();
            config.Gender = value;
            Configs[PlayerApi.ContentId] = config;
        }
    }
    [JsonIgnore]
    public RaceSetting Race
    {
        get => GetOrDefault().Race;
        set
        {
            var config = GetOrDefault();
            config.Race = value;
            Configs[PlayerApi.ContentId] = config;
        }
    }
    [JsonIgnore]
    public TribeSetting Tribe
    {
        get => GetOrDefault().Tribe;
        set
        {
            var config = GetOrDefault();
            config.Tribe = value;
            Configs[PlayerApi.ContentId] = config;
        }
    }

    public int GetGender()
    {
        return Gender switch
        {
            GenderSetting.Male => 0,
            GenderSetting.Female => 1,
            GenderSetting.Random => new Random().Next(0, 2),
            GenderSetting.Model => PlayerApi.Sex,
            _ => 0,
        };
    }

    [NonSerialized] private PrefPro _prefPro = null!;

    public void Initialize(PrefPro prefPro)
    {
        _prefPro = prefPro;
    }

    public ConfigHolder GetOrDefault()
    {
        bool result = Configs.TryGetValue(PlayerApi.ContentId, out var holder);
        if (!result)
        {
            var ch = new ConfigHolder
            {
                Name = PlayerApi.CharacterName,
                FullName = NameSetting.FirstLast,
                FirstName = NameSetting.FirstOnly,
                LastName = NameSetting.LastOnly,
                Gender = GenderSetting.Model,
                Enabled = false
            };
            return ch;
        }
        return holder;
    }

    public void Save()
    {
        DalamudApi.PluginInterface.SavePluginConfig(this);
        _prefPro.NameHandlerCache.Refresh();
    }
}
