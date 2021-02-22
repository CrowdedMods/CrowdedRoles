using Reactor;
using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using CrowdedRoles.Attributes;
using HarmonyLib;

namespace CrowdedRoles
{
    [BepInPlugin(Id)]
    [BepInDependency(ReactorPlugin.Id)]
    [ReactorPluginSide(PluginSide.ClientOnly)]
    public class RoleApiPlugin : BasePlugin
    {
        public const string Id = "xyz.crowdedmods.crowdedroles";
        private Harmony Harmony { get; } = new(Id);
#pragma warning disable CS8618
        public static ManualLogSource Logger { get; private set; }
#pragma warning restore CS8618

        public override void Load()
        {
            RegisterCustomRpcAttribute.Register(this);
            RegisterInIl2CppAttribute.Register();

#if DEBUG
            RegisterCustomRoleAttribute.Register(this);
            MyCustomOptions.RegisterOptions(this);
#endif
            Harmony.PatchAll();
            Logger = Log;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}