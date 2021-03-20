using Reactor;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
#if DEBUG
using CrowdedRoles.Attributes;
#endif
using CrowdedRoles.Options;
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
        public static ManualLogSource Logger { get; private set; } = null!;

        public override void Load()
        {
            RegisterCustomRpcAttribute.Register(this);
            RegisterInIl2CppAttribute.Register();
            
            BepInPlugin metadata = MetadataHelper.GetMetadata(this);
            OptionsManager.SaveOptionsFile = new ConfigFile(Utility.CombinePaths(Paths.ConfigPath, Id + ".options.cfg"), false, metadata);

#if DEBUG
            RegisterCustomRoleAttribute.Register(this);
            MyCustomOptions.RegisterOptions(this);
            RegisterCustomGameOverReasonAttribute.Register(this);
            RegisterCustomButtonAttribute.Register();
#endif
            Harmony.PatchAll();
            Logger = Log;
        }
    }
}