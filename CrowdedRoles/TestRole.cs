#if DEBUG
using BepInEx.IL2CPP;
using CrowdedRoles.Attributes;
using CrowdedRoles.Extensions;
using CrowdedRoles.Options;
using CrowdedRoles.Roles;
using UnityEngine;

namespace CrowdedRoles
{
    [RegisterCustomRole]
    public class TestRole : BaseRole
    {
        public TestRole(BasePlugin plugin) : base(plugin)
        {
        }

        public override string Name { get; } = "TestRole";
        public override Color Color { get; } = Color.cyan;
        public override Visibility Visibility { get; } = Visibility.Team;
        public override string Description { get; } = "say meow pls";
        public override PlayerAbilities Abilities { get; } = PlayerAbilities.Kill | PlayerAbilities.Vent | PlayerAbilities.Sabotage;
        public override Side Side { get; } = Side.Impostor;
        public override bool PreKill(ref PlayerControl killer, ref PlayerControl target, ref CustomMurderOptions options)
        {
            options |= CustomMurderOptions.NoAnimation | CustomMurderOptions.NoSnap;
            return true;
        }
    }

    public static class MyCustomOptions
    {
        public static readonly CustomToggleOption ToggleMe = new ("Super cool toggle option")
        { 
            OnValueChanged = v => RoleApiPlugin.Logger.LogDebug($"new test bool: {v}")
        };

        public static readonly CustomNumberOption IncrementMe = new ("Fake cooldown", new FloatRange(10, 100))
        {
            Increment = 0.25f,
            ValueFormat = "{0}s"
        };

        public static readonly CustomStringOption FixMe = new ("Omg still no arrows", new[] {"Everyone", "AOU", "No one"});

        public static void RegisterOptions(BasePlugin plugin)
        {
            new OptionPluginWrapper(plugin)
                .AddCustomOption(IncrementMe)
                .AddCustomOption(ToggleMe)
                .AddCustomOption(FixMe);
            // OR
            // new OptionPluginWrapper(plugin)
            //     .AddCustomOptions(new CustomOption[]
            //     {
            //         IncrementMe,
            //         ToggleMe,
            //         FixMe
            //     });
        }
    }
}
#endif