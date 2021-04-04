using System;
using System.Linq;
using CrowdedRoles.Components;
using CrowdedRoles.Options;
using HarmonyLib;
using Reactor.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CrowdedRoles.Patches
{
    internal static class OptionPatches
    {
        private static GameObject? CustomGameOptionsObject;
        
        [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
        [HarmonyPriority(Priority.First)]
        public static class GameOptionsMenu_Start
        {
            public static void Postfix(ref GameOptionsMenu __instance)
            {
                float lowestY = __instance.GetComponentsInChildren<OptionBehaviour>().Min(o => o.transform.position.y) - 0.2f;
                ToggleOption togglePrefab = __instance.Children.Where(o => o.TryCast<ToggleOption>() != null).First(o => o.Title != StringNames.GameRecommendedSettings).Cast<ToggleOption>(); // GameRecommendedSettings has a specific design
                NumberOption numberPrefab = __instance.GetComponentInChildren<NumberOption>();
                StringOption stringPrefab = __instance.GetComponentInChildren<StringOption>();
                byte optionsAdded = 0;
                
                foreach (var baseOption in 
                    OptionsManager.LimitOptions.Values
                        .Concat(OptionsManager.CustomOptions.SelectMany(p => p.Value))
                )
                {
                    var option = Object.Instantiate<OptionBehaviour>(
                        baseOption switch
                        {
                            CustomToggleOption => togglePrefab,
                            CustomNumberOption => numberPrefab,
                            CustomStringOption => stringPrefab,
                            _ => throw new InvalidCastException($"{nameof(baseOption)} was unknown type")
                        },
                        __instance.transform
                    );
                    
                    option.Title = OptionsManager.CustomOptionStringName;
                    baseOption.ImplementOption(ref option);

                    var transform = option.transform;
                    Vector3 oldPos = transform.position;
                    oldPos.y = lowestY -= 0.5f;
                    transform.position = oldPos;
                    option.name = $"{baseOption.Name}_{optionsAdded++}";
                }

                DestroyableSingleton<GameSettingMenu>.Instance.GetComponent<Scroller>().YBounds.max += optionsAdded * 0.5f;
                OptionsManager.ValueChanged();
            }
        }

        [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
        public static class LobbyBehaviour_Start
        {
            public static void Postfix()
            {
                CustomGameOptionsObject = new GameObject("CustomRoleOptions");
                CustomGameOptionsObject.transform.SetParent(HudManager.Instance.transform);
                CustomGameOptionsObject.AddComponent<CustomGameOptions>();
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
        public static class ShipStatus_Start
        {
            public static void Postfix()
            {
                CustomGameOptionsObject.Destroy();
            }
        }
    }
}