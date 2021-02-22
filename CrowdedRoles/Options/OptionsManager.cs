using System.Collections.Generic;
using BepInEx;
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
        internal static Dictionary<BaseRole, CustomOption> LimitOptions { get; } = new();

        public static T AddCustomOption<T>(BasePlugin plugin, T option) where T : CustomOption
        {
            string guid = MetadataHelper.GetMetadata(plugin).GUID;
            if (!CustomOptions.TryGetValue(guid, out var options))
            {
                options = new List<CustomOption>();
                CustomOptions.Add(guid, options);
            }
            
            options.Add(option);
            CustomOptions[guid] = options;
            return option;
        }

        internal static void AddLimitOptionIfNecessary(BaseRole role)
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
            
            LimitOptions.Add(role, option);
        }
        
        internal static void ValueChanged()
        {
            HudManager.Instance.GetComponentInChildren<CustomGameOptions>()?.UpdateText();
        }
    }

    public class OptionPluginWrapper
    {
        private BasePlugin plugin { get; }

        public OptionPluginWrapper(BasePlugin plugin)
        {
            this.plugin = plugin;
        }

        public T AddCustomOption<T>(T option) where T : CustomOption
        {
            return OptionsManager.AddCustomOption(plugin, option);
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