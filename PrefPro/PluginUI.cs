using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface.Utility;
using PrefPro.Settings;

namespace PrefPro;

class PluginUI : IDisposable
{
    private readonly Configuration _configuration;
    private bool _settingsVisible;
    
    private string _tmpFirstName = "";
    private string _tmpLastName = "";
    private bool _resetNames;

    public bool SettingsVisible
    {
        get => _settingsVisible;
        set
        {
            if (value)
            {
                _resetNames = true;
            }
            _settingsVisible = value;
        }
    }

    public PluginUI(Configuration configuration)
    {
        _configuration = configuration;
        
        DalamudApi.PluginInterface.UiBuilder.Draw += Draw;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += () => SettingsVisible = true;
    }

    public void Dispose()
    {
			
    }

    public void Draw()
    {
        DrawSettingsWindow();
    }

    public void DrawSettingsWindow()
    {
        if (!SettingsVisible) return;

        var height = 340;
        var width = _configuration.Gender == GenderSetting.Random ? 390 : 360;
        var size = new Vector2(height, width) * ImGui.GetIO().FontGlobalScale;
        ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
        if (ImGui.Begin("PrefPro Config", ref _settingsVisible, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (DalamudApi.ClientState.LocalPlayer is null)
            {
                ImGui.TextWrapped("Configuration is not available while logged out or in a loading screen.");
                ImGui.End();
                return;
            }

            if (_configuration.Name == "")
            {
                DalamudApi.PluginLog.Debug($"Configuration name is empty, setting to current player name ({PlayerApi.CharacterName}).");
                _configuration.Name = PlayerApi.CharacterName;
            }

            if (_resetNames)
            {
                var split = _configuration.Name.Split(' ');
                _tmpFirstName = split[0];
                _tmpLastName = split[1];
                _resetNames = false;
            }

            var enabled = _configuration.Enabled;
            var currentGender = _configuration.Gender;
            var currentRace = _configuration.Race;
            var currentTribe = _configuration.Tribe;

            if (currentRace == RaceSetting.Unknown || currentTribe == TribeSetting.Unknown)
            {
                currentRace = (RaceSetting)PlayerApi.Race;
                currentTribe = (TribeSetting)PlayerApi.Tribe;
                _configuration.Race = currentRace;
                _configuration.Tribe = currentTribe;
                _configuration.Save();
            }
                
            var nameFull = _configuration.FullName;
            var nameFirst = _configuration.FirstName;
            var nameLast = _configuration.LastName;
                
            if (ImGui.Checkbox("Enable PrefPro", ref enabled))
            {
                _configuration.Enabled = enabled;
                _configuration.Save();
            }

            if (ImGui.CollapsingHeader("Developer note regarding they/them pronouns"))
            {
                ImGui.TextWrapped("PrefPro currently cannot and will never have support for they/them pronouns. " +
                                  "This is entirely due to technical limitations and the amount of work for such a feature. " +
                                  "This would require rewriting most dialogue in the game across all languages as well as upkeep on new patches.");
                ImGui.End();
                return;
            }
                
            ImGui.Text("For name replacement, PrefPro should use the name...");
            ImGui.Indent(10f * ImGuiHelpers.GlobalScale);
            ImGui.PushItemWidth(105f * ImGuiHelpers.GlobalScale);
            ImGui.InputText("##newFirstName", ref _tmpFirstName, 15);
            ImGui.SameLine();
            ImGui.InputText("##newLastName", ref _tmpLastName, 15);
            ImGui.PopItemWidth();
            ImGui.PushItemWidth(20f * ImGuiHelpers.GlobalScale);
            ImGui.SameLine();
            if (ImGui.Button("Set##prefProNameSet"))
            {
                string setName = SanitizeName(_tmpFirstName, _tmpLastName);
                _configuration.Name = setName;
                var split = setName.Split(' ');
                _tmpFirstName = split[0];
                _tmpLastName = split[1];
                _configuration.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button("Reset##prefProNameReset"))
            {
                string resetName = PlayerApi.CharacterName;
                _configuration.Name = resetName;
                var split = resetName.Split(' ');
                _tmpFirstName = split[0];
                _tmpLastName = split[1];
                _configuration.Save();
            }
            ImGui.PopItemWidth();
            ImGui.Indent(-10f * ImGuiHelpers.GlobalScale);
                
            ImGui.Text("When NPCs and dialogue use my full name, instead use...");
            ImGui.Indent(10f * ImGuiHelpers.GlobalScale);
            ImGui.PushItemWidth(300f * ImGuiHelpers.GlobalScale);
            if (DrawSettingSelector("##fullNameCombo", _configuration.FullName, out var newFullName))
            {
                _configuration.FullName = newFullName;
                _configuration.Save();
            }
            ImGui.Indent(-10f * ImGuiHelpers.GlobalScale);
                
            ImGui.Text("When NPCs and dialogue use my first name, instead use...");
            ImGui.Indent(10f * ImGuiHelpers.GlobalScale);
            if (DrawSettingSelector("##firstNameCombo", _configuration.FirstName, out var newFirstName))
            {
                _configuration.FirstName = newFirstName;
                _configuration.Save();
            }
            ImGui.Indent(-10f * ImGuiHelpers.GlobalScale);
                
            ImGui.Text("When NPCs and dialogue use my last name, instead use...");
            ImGui.Indent(10f * ImGuiHelpers.GlobalScale);
            if (DrawSettingSelector("##lastNameCombo", _configuration.LastName, out var newLastName))
            {
                _configuration.LastName = newLastName;
                _configuration.Save();
            }
            ImGui.PopItemWidth();
            ImGui.Indent(-10f * ImGuiHelpers.GlobalScale);

            ImGui.TextWrapped("When NPCs and dialogue use gendered text, refer to me as if my character is...");

            ImGui.Indent(10f * ImGuiHelpers.GlobalScale);
            ImGui.PushItemWidth(140 * ImGuiHelpers.GlobalScale);
            if (DrawSettingSelector("##genderCombo", _configuration.Gender, out var newGender))
            {
                _configuration.Gender = newGender;
                _configuration.Save();
            }
            if (_configuration.Gender == GenderSetting.Random)
                ImGui.TextWrapped("Please note that the gender used in text may not match the gender used in voiceovers.");
            ImGui.Indent(-10f * ImGuiHelpers.GlobalScale);
                
            ImGui.TextWrapped("When NPCs and dialogue refer to my race, refer to me as if my character is...");

            ImGui.Indent(10f * ImGuiHelpers.GlobalScale);
            ImGui.PushItemWidth(140 * ImGuiHelpers.GlobalScale);
            if (DrawSettingSelector("##raceCombo", _configuration.Race, out var newRace, 1))
            {
                _configuration.Race = newRace;
                _configuration.Save();
            }
            ImGui.Indent(-10f * ImGuiHelpers.GlobalScale);
                
            ImGui.TextWrapped("When NPCs and dialogue refer to my tribe, refer to me as if my character is...");

            ImGui.Indent(10f * ImGuiHelpers.GlobalScale);
            ImGui.PushItemWidth(200 * ImGuiHelpers.GlobalScale);
            if (DrawSettingSelector("##tribeCombo", _configuration.Tribe, out var newTribe, 1))
            {
                _configuration.Tribe = newTribe;
                _configuration.Save();
            }
            ImGui.PopItemWidth();
            ImGui.Indent(-10f * ImGuiHelpers.GlobalScale);
        }
        ImGui.End();
    }
        
    private string SanitizeName(string first, string last)
    {
        string newFirst = first;
        string newLast = last;
            
        // Save the last valid name for fail cases
        string lastValid = _configuration.Name;
            
        if (newFirst.Length > 15 || newLast.Length > 15)
            return lastValid;
        string combined = $"{newFirst}{newLast}";
        if (combined.Length > 20)
            return lastValid;

        newFirst = Regex.Replace(newFirst, "[^A-Za-z'\\-\\s{1}]", "");
        newLast = Regex.Replace(newLast, "[^A-Za-z'\\-\\s{1}]", "");

        return $"{newFirst} {newLast}";
    }
    
    private bool DrawSettingSelector<T>(string key, T current, out T newValue, int startIndex = 0) where T : struct, Enum
    {
        var changed = false;
        newValue = current;

        if (ImGui.BeginCombo(key, GetOptionDescriptor(current)))
        {
            var values = Enum.GetValues<T>();
            for (int i = startIndex; i < values.Length; i++)
            {
                var value = values[i];
                var selected = EqualityComparer<T>.Default.Equals(current, value);
                if (selected) ImGui.SetItemDefaultFocus();
                if (ImGui.Selectable(GetOptionDescriptor(value), selected))
                {
                    changed = true;
                    newValue = value;
                }
            }
            ImGui.EndCombo();
        }

        return changed;
    }
    
    private string GetOptionDescriptor(Enum current)
    {
        return current switch
        {
            NameSetting ns => GetNameOptionDescriptor(ns),
            GenderSetting gs => GetGenderOptionDescriptor(gs),
            RaceSetting rs => GetRaceOptionDescriptor(rs),
            TribeSetting ts => GetTribeOptionDescriptor(ts),
            _ => string.Empty,
        };
    }

    private string GetNameOptionDescriptor(NameSetting setting)
    {
        return setting switch
        {
            NameSetting.FirstLast => "First name, then last",
            NameSetting.FirstOnly => "First name only",
            NameSetting.LastOnly => "Last name only",
            NameSetting.LastFirst => "Last name, then first",
            _ => ""
        };
    }

    private string GetGenderOptionDescriptor(GenderSetting setting)
    {
        return setting switch
        {
            GenderSetting.Male => "Male",
            GenderSetting.Female => "Female",
            GenderSetting.Random => "Random gender",
            GenderSetting.Model => "Model gender",
            _ => ""
        };
    }
        
    private string GetRaceOptionDescriptor(RaceSetting setting)
    {
        return setting switch
        {
            RaceSetting.Hyur => "Hyur",
            RaceSetting.Elezen => "Elezen",
            RaceSetting.Lalafell => "Lalafell",
            RaceSetting.Miqote => "Miqo'te",
            RaceSetting.Roegadyn => "Roegadyn",
            RaceSetting.AuRa => "Au Ra",
            RaceSetting.Hrothgar => "Hrothgar",
            RaceSetting.Viera => "Viera",
            _ => "",
        };
    }

    private string GetTribeOptionDescriptor(TribeSetting setting)
    {
        return setting switch
        {
            TribeSetting.Midlander => "Midlander",
            TribeSetting.Highlander => "Highlander",
            TribeSetting.Wildwood => "Wildwood",
            TribeSetting.Duskwight => "Duskwight",
            TribeSetting.Plainsfolk => "Plainsfolk",
            TribeSetting.Dunesfolk => "Dunesfolk",
            TribeSetting.SeekerOfTheSun => "Seeker of the Sun",
            TribeSetting.KeeperOfTheMoon => "Keeper of the Moon",
            TribeSetting.SeaWolf => "Sea Wolf",
            TribeSetting.Hellsguard => "Hellsguard",
            TribeSetting.Raen => "Raen",
            TribeSetting.Xaela => "Xaela",
            TribeSetting.Helions => "Helions",
            TribeSetting.TheLost => "The Lost",
            TribeSetting.Rava => "Rava",
            TribeSetting.Veena => "Veena",
            _ => "",
        };
    }
}