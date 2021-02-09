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
        static class GameOptionsMenu_Start
        {
            static void Postfix(ref GameOptionsMenu __instance)
            {
                float lowestY = __instance.GetComponentsInChildren<OptionBehaviour>().Min(o => o.transform.position.y) - 0.2f;
                ToggleOption togglePrefab = __instance.GetComponentsInChildren<ToggleOption>().FirstOrDefault(o => o.Title != StringNames.GameRecommendedSettings)!; // GameRecommendedSettings has a specific design
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
                    baseOption.ImplementOption(ref option);
                    
                    Vector3 oldPos = option.transform.position;
                    oldPos.y = lowestY -= 0.5f;
                    option.transform.position = oldPos;
                    option.name = $"{baseOption.Name}_{optionsAdded++}";
                }

                DestroyableSingleton<GameSettingMenu>.Instance.GetComponent<Scroller>().YBounds.max += optionsAdded * 0.5f;
                OptionsManager.ValueChanged();
            }
        }

        [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
        private static class LobbyBehaviour_Start
        {
            private static void Postfix()
            {
                CustomGameOptionsObject = new GameObject("CustomRoleOptions");
                CustomGameOptionsObject.transform.SetParent(DestroyableSingleton<HudManager>.Instance.transform);
                CustomGameOptionsObject.AddComponent<CustomGameOptions>();
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
        private static class ShipStatus_Start
        {
            private static void Postfix()
            {
                CustomGameOptionsObject.Destroy();
            }
        }
    }
}