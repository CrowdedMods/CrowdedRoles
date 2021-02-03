﻿using System.Collections.Generic;
using BepInEx.IL2CPP;
using CrowdedRoles.Api.Components;
using Reactor;

namespace CrowdedRoles.Api.Options
{
    public static class OptionsManager
    {

        internal static readonly CustomStringName CustomOptionStringName = CustomStringName.Register("You found a glitch!"); // should never appear
        internal static Dictionary<BasePlugin, List<CustomOption>> CustomOptions { get; } = new();

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
        
        internal static void ValueChanged()
        {
            DestroyableSingleton<HudManager>.Instance.GetComponentInChildren<CustomGameOptions>()?.UpdateText();
        }
    }
}