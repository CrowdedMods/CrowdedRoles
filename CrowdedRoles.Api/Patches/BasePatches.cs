using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using CrowdedRoles.Api.Roles;
using CrowdedRoles.Api.Extensions;
using CrowdedRoles.Api.Rpc;
using Reactor;

namespace CrowdedRoles.Api.Patches
{
    internal static class BasePatches
    {
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.SelectInfected))]
        private static class ShipStatus_SelectInfected
        {
            public static void Postfix()
            {
                var goodPlayers = new List<byte>();
                foreach(var p in GameData.Instance.AllPlayers)
                {
                    if(!p.Disconnected && !p.IsImpostor)
                    {
                        goodPlayers.Add(p.PlayerId);
                    }
                }

                foreach((BaseRole role, byte limit) in RoleManager.Limits)
                {
                    if (limit == 0) continue; // fast skip
                    if (role.PatchFilterFlags.HasFlag(PatchFilter.SelectInfected)) continue;
                    
                    List<byte> shuffledPlayers = goodPlayers.OrderBy(_ => new Guid()).ToList();
                    goodPlayers = shuffledPlayers.Skip(limit).ToList();
                    
                    Rpc<SelectCustomRole>.Instance.Send(
                        new SelectCustomRole.Data {
                            role = role.Data, 
                            holders = shuffledPlayers.Take(limit).ToArray()
                        }
                    );
                }
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        private static class IntroCutScene_BeginCrewmate
        {
            private static bool Prefix(ref IntroCutscene __instance)
            {
                BaseRole? myRole = PlayerControl.LocalPlayer.GetRole();
                if (myRole == null || myRole.PatchFilterFlags.HasFlag(PatchFilter.IntroCutScene))
                {
                    return true;
                }

                List<PlayerControl> myTeam = myRole.Visibility == Visibility.Myself ?
                    new List<PlayerControl> { PlayerControl.LocalPlayer } : 
                    PlayerControl.LocalPlayer.GetTeam();

                for(var i = 0; i < myTeam.Count; i++)
                {
                    GameData.PlayerInfo data = myTeam[i].Data;
                    int oddness = (i + 1) / 2;
                    PoolablePlayer player = UnityEngine.Object.Instantiate(__instance.PlayerPrefab, __instance.transform);
                    player.transform.position = new Vector3(
                        oddness * (i % 2 == 0 ? -1 : 1) * (1 - oddness*0.035f),
                        __instance.BaseY + oddness * 0.15f,
                        (i == 0 ? -8 : -1) + oddness * 0.01f
                    ) * 1.5f;
                    player.SetFlipX(i % 2 == 1);
                    PlayerControl.SetPlayerMaterialColors(data.ColorId, player.Body);
                    DestroyableSingleton<HatManager>.Instance.Method_4(player.SkinSlot, data.SkinId);
                    player.HatSlot.SetHat(data.HatId, data.ColorId);
                    PlayerControl.SetPetImage(data.PetId, data.ColorId, player.PetSlot);
                    float scale = 1 - oddness * 0.1125f;
                    player.transform.localScale = player.NameText.transform.localScale = new Vector3(scale, scale, scale);
                    player.NameText.Text = myRole.FormatName(player.NameText.Text);
                    if (i > 0 && myRole.Visibility != Visibility.Everyone)
                    {
                        player.NameText.gameObject.SetActive(true);
                    }
                    __instance.ImpostorText.Text = myRole.StartTip;
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Method_7))]
        [HarmonyPatch(new[] { typeof(GameData.PlayerInfo) })]
        static class MeetingHud_CreateButton
        {
            static void Postfix([HarmonyArgument(0)] ref GameData.PlayerInfo data, ref PlayerVoteArea __result)
            {
                BaseRole? role = data.Object.GetRole();
                if (role == null || role.PatchFilterFlags.HasFlag(PatchFilter.MeetingHud))
                {
                    return;
                }
                
                if(PlayerControl.LocalPlayer.CanSee(data.Object))
                {
                    __result.NameText.Color = role.Color;
                    __result.NameText.Text = role.FormatName(__result.NameText.Text);
                }
            }
        }
    }
}
