using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using CrowdedRoles.Components;
using CrowdedRoles.Roles;
using Reactor;

namespace CrowdedRoles.Options
{
    public static class OptionsManager
    {

        internal static readonly CustomStringName CustomOptionStringName = CustomStringName.Register("You found a glitch!"); // should never appear
        internal static Dictionary<string, List<CustomOption>> CustomOptions { get; } = new();
        internal static Dictionary<BaseRole, CustomNumberOption> LimitOptions { get; } = new();
        internal static ConfigFile SaveOptionsFile { get; set; } = null!;
        private static readonly char[] invalidConfigChars = { '=', '\n', '\t', '\\', '"', '\'', '[', ']' };

        internal static string MakeSaveNameValid(string name)
        {
            return invalidConfigChars.Aggregate(name, (current, configChar) => current.Replace(configChar, '_'));
        }

        /// <summary>
        /// Get limit option for given <see cref="BaseRole"/>
        /// </summary>
        public static CustomNumberOption? GetLimitOption<T>() where T : BaseRole
        {
            foreach (var (role, option) in LimitOptions)
            {
                if (role is T)
                {
                    return option;
                }
            }

            return null;
        }

        public static void AddCustomOption<T>(BasePlugin plugin, T option) where T : CustomOption
        {
            string guid = MetadataHelper.GetMetadata(plugin).GUID;
            if (!CustomOptions.TryGetValue(guid, out var options))
            {
                options = new List<CustomOption>();
                CustomOptions.Add(guid, options);
            }

            option.LoadValue(SaveOptionsFile, guid);
            
            options.Add(option);
            CustomOptions[guid] = options;
        }

        internal static void AddLimitOptionIfNecessary(BaseRole role, string guid)
        {
            if (role.PatchFilterFlags.HasFlag(PatchFilter.AmountOption)) return;
            if (LimitOptions.ContainsKey(role)) return;
            
            var option = new CustomNumberOption(role.Name + " amount", new FloatRange(0, 127))
            {
                OnValueChanged = amount =>
                {
                    RoleManager.Limits[role] = (byte)amount;
                }
            };
            option.LoadValue(SaveOptionsFile, "Limits", $"{role.Name}:{guid}");
            RoleManager.Limits[role] = (byte)option.Value;
            
            LimitOptions.Add(role, option);
        }
        
        internal static void ValueChanged()
        {
            HudManager.Instance.GetComponentInChildren<CustomGameOptions>()?.UpdateText();
        }
    }

    /// <summary>
    /// Wrapper to register a couple of options easier
    /// </summary>
    public class OptionPluginWrapper
    {
        private BasePlugin plugin { get; }

        public OptionPluginWrapper(BasePlugin plugin)
        {
            this.plugin = plugin;
        }

        public OptionPluginWrapper AddCustomOption<T>(T option) where T : CustomOption
        {
            OptionsManager.AddCustomOption(plugin, option);
            return this;
        }

        public OptionPluginWrapper AddCustomOptions(IEnumerable<CustomOption> options)
        {
            foreach (CustomOption option in options)
            {
                OptionsManager.AddCustomOption(plugin, option);
            }

            return this;
        }
    }
}