using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using CrowdedRoles.Extensions;
using CrowdedRoles.Roles;
using CrowdedRoles.Rpc;
using Reactor;
using UnhollowerBaseLib;

namespace CrowdedRoles.Patches
{
    internal static class BasePatches
    {
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetInfected))]
        private static class PlayerControl_RpcSetInfected
        {
            private static void Prefix([HarmonyArgument(0)] ref Il2CppReferenceArray<GameData.PlayerInfo> infected)
            {
                if (RoleManager.rolesSet)
                {
                    RoleApiPlugin.Logger.LogWarning("Trying to override roles");
                    return;
                }
                var goodPlayers = new RoleHolders(GameData.Instance.AllPlayers.ToArray(), infected.ToArray())
                {
                    CustomRoleHolders = GameData.Instance.AllPlayers
                        .ToArray()
                        .Where(p => p.GetRole() != null)
                        .GroupBy(p => p.GetRole()!)
                        .ToDictionary(p => p.Key, p => (IEnumerable<GameData.PlayerInfo>)p)
                };

                foreach (var (role, limit) in RoleManager.Limits)
                {
                    var localHolders = role.SelectHolders(goodPlayers, limit);
                    if (goodPlayers.CustomRoleHolders.ContainsKey(role))
                    {
                        goodPlayers.CustomRoleHolders[role] = goodPlayers.CustomRoleHolders[role].Concat(localHolders);
                    }
                    else
                    {
                        goodPlayers.CustomRoleHolders.Add(role, localHolders);
                    }
                }

                infected = goodPlayers.Impostors.ToArray();

                Rpc<SelectCustomRole>.Instance.Send(
                    goodPlayers.CustomRoleHolders.ToDictionary(
                        p => p.Key.Data, 
                        p => p.Value.Select(player => player.PlayerId).ToArray()
                    )
                );
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        private static class IntroCutScene_BeginCrewmate
        {
            private static readonly int Color = Shader.PropertyToID("_Color");

            private static bool Prefix(ref IntroCutscene __instance)
            {
                BaseRole? myRole = PlayerControl.LocalPlayer.GetRole();
                if (myRole == null || myRole.PatchFilterFlags.HasFlag(PatchFilter.IntroCutScene))
                {
                    return true;
                }

                List<PlayerControl> myTeam = PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => PlayerControl.LocalPlayer.IsVisibleTeammate(p))
                    .OrderBy(p => !p.AmOwner)
                    .ToList();

                __instance.Title.Text = myRole.Name;
                __instance.Title.Color = myRole.Color;
                __instance.BackgroundBar.material.SetColor(Color, myRole.Color);
                __instance.ImpostorText.Text = myRole.Description;
                
                for(var i = 0; i < myTeam.Count; i++)
                {
                    GameData.PlayerInfo data = myTeam[i].Data;
                    int oddness = (i + 1) / 2;
                    PoolablePlayer player = Object.Instantiate(__instance.PlayerPrefab, __instance.transform);
                    player.transform.localPosition = new Vector3(
                        0.8f* oddness * (i % 2 == 0 ? -1 : 1) * (1 - oddness * 0.08f),
                        __instance.BaseY - 0.25f + oddness * 0.1f,
                        (i == 0 ? -8 : -1) + oddness * 0.01f
                    ) * 1.5f;
                    player.SetFlipX(i % 2 == 0);
                    PlayerControl.SetPlayerMaterialColors(data.ColorId, player.Body);
                    DestroyableSingleton<HatManager>.Instance.Method_4(player.SkinSlot, data.SkinId);
                    player.HatSlot.SetHat(data.HatId, data.ColorId);
                    PlayerControl.SetPetImage(data.PetId, data.ColorId, player.PetSlot);
                    float scale = 1f - oddness * 0.075f;
                    Vector3 scaleVec = new Vector3(scale, scale, scale) * 1.5f;
                    player.transform.localScale = scaleVec;
                    player.NameText.transform.localScale = global::Extensions.Inv(scaleVec);
                    player.NameText.Text = myRole.FormatName(data);
                    if (i > 0 && myRole.Visibility != Visibility.Everyone)
                    {
                        player.NameText.gameObject.SetActive(true);
                    }
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
                BaseRole? role = data.GetRole();
                if (role?.PatchFilterFlags.HasFlag(PatchFilter.MeetingHud) ?? false)
                {
                    return;
                }
                
                if(PlayerControl.LocalPlayer.CanSee(data.Object))
                {
                    __result.NameText.Color = role?.Color ?? Palette.ImpostorRed;
                    if (role != null)
                    {
                        __result.NameText.Text = role.FormatName(data);
                    }
                }
            }
        }

        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(IntroCutscene.CoBegin__d), nameof(IntroCutscene.CoBegin__d.MoveNext))]
        private static class IntroCutScene_CoBegin
        {
            private static bool Prefix(ref bool __result)
            {
                return RoleManager.rolesSet && (__result = true); // wait until we set our roles to prevent bugs
            }
            private static void Postfix(bool __result)
            {
                if (!__result) // yield break
                {
                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        BaseRole? role = player.GetRole();
                        if (PlayerControl.LocalPlayer.CanSee(player))
                        {
                            player.nameText.Color = role?.Color ?? Palette.ImpostorRed;
                            if (role != null)
                            {
                                player.nameText.Text = role.FormatName(player.Data);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
        private static class ExileController_Animate
        {
            private static void Postfix(ExileController __instance)
            {
                var role = __instance.exiled.GetRole();
                if (role == null) return;
                var revealRole = role.RevealExiledRole;
                if (revealRole != RevealRole.Never && (revealRole == RevealRole.Always || 
                                                       revealRole == RevealRole.Default &&
                                                       PlayerControl.GameOptions.ConfirmImpostor))
                { 
                    __instance.completeString = $"{__instance.exiled.PlayerName} was {(role.Name.ToLower().StartsWith("the") ? "" : "The ")}{role.Name}.";
                }
            }
        }
    }
}
