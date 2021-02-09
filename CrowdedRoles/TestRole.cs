#if DEBUG
using BepInEx.IL2CPP;
using CrowdedRoles.Attributes;
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
        public override string StartTip { get; } = "say meow pls";
        public override bool AbleToKill { get; } = true;
        public override Side Side { get; } = Side.Impostor;
        public override bool PreKill(ref PlayerControl killer, ref PlayerControl target, ref bool noSnap)
        {
            return noSnap = true;
        }
    }
}
#endif