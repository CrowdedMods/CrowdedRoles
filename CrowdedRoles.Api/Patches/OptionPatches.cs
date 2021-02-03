using System;
using System.Linq;
using CrowdedRoles.Api.Options;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CrowdedRoles.Api.Patches
{
    internal static class OptionPatches
    {
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
                
                foreach (var baseOption in OptionsManager.CustomOptions.SelectMany(p => p.Value))
                {
                    /*OptionBehaviour? behaviour = null;
                    switch (baseOption)
                    {
                        case CustomToggleOption toggleOption:
                        {
                            ToggleOption option = Object.Instantiate(copyableToggle, __instance.transform);
                            toggleOption.ImplementOption(ref option);
                            behaviour = option;
                            break;
                        }
                        case CustomNumberOption numberOption:
                        {
                            NumberOption option = Object.Instantiate(copyableNumber, __instance.transform);
                            numberOption.ImplementOption(ref option);
                            behaviour = option;
                            break;
                        }
                    }

                    if (behaviour == null)
                    {
                        // shouldn't be thrown
                        throw new NullReferenceException($"Custom option type of {baseOption.Name} is undefined");
                    }
                    
                    Vector3 oldPos = behaviour.transform.position;
                    oldPos.y = lowestY -= 0.5f;
                    behaviour.transform.position = oldPos;*/

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
    }
}