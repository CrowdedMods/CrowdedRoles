using Reactor;
using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

namespace CrowdedRoles.Api
{
    [BepInPlugin(Id)]
    [BepInDependency(ReactorPlugin.Id)]
    public class MainPlugin : BasePlugin
    {
        public const string Id = "ru.galster.CrowdedRoles.Api";
        public Harmony Harmony { get; } = new Harmony(Id);
#pragma warning disable CS8618
        public static ManualLogSource Logger { get; private set; }
#pragma warning restore CS8618

        public override void Load()
        {
            Harmony.PatchAll();
            Logger = Log;
        }
    }
}
