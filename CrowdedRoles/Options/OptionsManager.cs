using System.Collections.Generic;
using BepInEx.IL2CPP;
using CrowdedRoles.Components;
using CrowdedRoles.Roles;
using Reactor;

namespace CrowdedRoles.Options
{
    public static class OptionsManager
    {

        internal static readonly CustomStringName CustomOptionStringName = CustomStringName.Register("You found a glitch!"); // should never appear
        internal static Dictionary<BasePlugin, List<CustomOption>> CustomOptions { get; } = new();
        internal static Dictionary<BaseRole, CustomOption> LimitOptions { get; } = new();

        public static void AddCustomOption(BasePlugin plugin, CustomOption option)
        {
            if (!CustomOptions.TryGetValue(plugin, out var options))
            {
                options = new List<CustomOption>();
                CustomOptions.Add(plugin, options);
            }
            
            options.Add(option);
            CustomOptions[plugin] = options;
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
            DestroyableSingleton<HudManager>.Instance.GetComponentInChildren<CustomGameOptions>()?.UpdateText();
        }
    }
}