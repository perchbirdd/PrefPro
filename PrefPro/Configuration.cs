using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Newtonsoft.Json;
using PrefPro.Settings;

namespace PrefPro
{
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

        public Dictionary<ulong, ConfigHolder> Configs { get; set; } = new Dictionary<ulong, ConfigHolder>();

        [JsonIgnore]
        public bool Enabled
        {
            get => GetOrDefault().Enabled;
            set
            {
                var config = GetOrDefault();
                config.Enabled = value;
                Configs[_prefPro.CurrentPlayerContentId] = config;
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
                Configs[_prefPro.CurrentPlayerContentId] = config;
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
                Configs[_prefPro.CurrentPlayerContentId] = config;
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
                Configs[_prefPro.CurrentPlayerContentId] = config;
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
                Configs[_prefPro.CurrentPlayerContentId] = config;
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
                Configs[_prefPro.CurrentPlayerContentId] = config;
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
                Configs[_prefPro.CurrentPlayerContentId] = config;
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
                Configs[_prefPro.CurrentPlayerContentId] = config;
            }
        }
        
        public int GetGender()
        {
            switch (Gender)
            {
                case GenderSetting.Male:
                    return 0;
                case GenderSetting.Female:
                    return 1;
                case GenderSetting.Random:
                    var ret = new Random().Next(0, 2);
                    return ret;
                case GenderSetting.Model:
                    return _prefPro.PlayerGender;
            }
            return 0;
        }
        
        [NonSerialized] private PrefPro _prefPro;

        public void Initialize(PrefPro prefPro)
        {
            _prefPro = prefPro;
        }

        public ConfigHolder GetOrDefault()
        {
            bool result = Configs.TryGetValue(_prefPro.CurrentPlayerContentId, out var holder);
            if (!result)
            {
                var ch = new ConfigHolder
                {
                    Name = _prefPro.PlayerName ?? "",
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
            _prefPro.OnConfigSave();
        }
    }
}
