using BepInEx.IL2CPP;
using CrowdedRoles.Api.Roles;
using UnityEngine;

namespace CrowdedRoles.Api
{
    public class TestRole : BaseRole
    {
        public TestRole(BasePlugin plugin) : base(plugin)
        {
        }

        public override string Name { get; } = "TestRole";
        public override Color Color { get; } = Color.cyan;
    }
}