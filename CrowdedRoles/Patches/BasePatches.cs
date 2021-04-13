using System.Collections.Generic;
using System.Linq;
using CrowdedRoles.Extensions;
using CrowdedRoles.Roles;
using CrowdedRoles.Rpc;
using HarmonyLib;
using Reactor.Networking;
using UnhollowerBaseLib;
using UnityEngine;
using Random = System.Random;

namespace CrowdedRoles.Patches
{
    internal static class BasePatches
    {
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetInfected))]
        public static class PlayerControl_RpcSetInfected
        {
            public static void Prefix([HarmonyArgument(0)] ref Il2CppReferenceArray<GameData.PlayerInfo> infected)
            {
                if (RoleManager.RolesSet)
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

                var random = new Random();
                foreach (var (role, limit) in RoleManager.Limits.OrderBy(_ => random.Next()))
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
        public static class IntroCutScene_BeginCrewmate
        {
            public static readonly int Color = Shader.PropertyToID("_Color");

            public static bool Prefix(ref IntroCutscene __instance)
            {
                BaseRole? myRole = PlayerControl.LocalPlayer.GetRole();
                if (myRole == null || myRole.PatchFilterFlags.HasFlag(PatchFilter.IntroCutScene))
                {
                    return true;
                }
                
                PlayerControl.LocalPlayer.SetKillTimer(10f);
                HudManager.Instance.KillButton.gameObject.SetActive(myRole.CanKill(null));

                List<PlayerControl> myTeam = PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => PlayerControl.LocalPlayer.CanSeeSpecial(p))
                    .OrderBy(p => !p.AmOwner)
                    .ToList();

                __instance.Title.text = myRole.Name;
                __instance.Title.color = myRole.Color;
                __instance.BackgroundBar.material.SetColor(Color, myRole.Color);
                __instance.ImpostorText.text = myRole.Description;
                
                for(var i = 0; i < myTeam.Count; i++)
                {
                    GameData.PlayerInfo data = myTeam[i].Data;
                    int oddness = (i + 1) / 2;
                    PoolablePlayer player = Object.Instantiate(__instance.PlayerPrefab, __instance.transform);
                    player.transform.localPosition = new Vector3(
                        0.8f* oddness * (i % 2 == 0 ? -1 : 1) * (1 - oddness * 0.08f),
                        __instance.BaseY + oddness * 0.15f,
                        (i == 0 ? -8 : -1) + oddness * 0.01f
                    ) * 1.5f;
                    player.SetFlipX(i % 2 == 0);
                    PlayerControl.SetPlayerMaterialColors(data.ColorId, player.Body);
                    DestroyableSingleton<HatManager>.Instance.SetSkin(player.SkinSlot, data.SkinId); // SetSkin
                    player.HatSlot.SetHat(data.HatId, data.ColorId);
                    PlayerControl.SetPetImage(data.PetId, data.ColorId, player.PetSlot);
                    float scale = 1f - oddness * 0.075f;
                    Vector3 scaleVec = new Vector3(scale, scale, scale) * 1.5f;
                    player.transform.localScale = scaleVec;
                    player.NameText.transform.localScale = global::Extensions.Inv(scaleVec);
                    player.NameText.text = myRole.FormatName(data);
                    if (i > 0 && myRole.Visibility != Visibility.Everyone)
                    {
                        player.NameText.gameObject.SetActive(true);
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CreateButton))]
        [HarmonyPatch(new[] { typeof(GameData.PlayerInfo) })]
        public static class MeetingHud_CreateButton
        {
            public static void Postfix([HarmonyArgument(0)] ref GameData.PlayerInfo data, ref PlayerVoteArea __result)
            {
                BaseRole? role = data.GetRole();
                if (role?.PatchFilterFlags.HasFlag(PatchFilter.MeetingHud) ?? false)
                {
                    return;
                }
                
                if(PlayerControl.LocalPlayer.Data.CanSee(data) && data.HasRole())
                {
                    __result.NameText.color = role?.Color ?? Palette.ImpostorRed;
                    if (role != null)
                    {
                        __result.NameText.text = role.FormatName(data);
                    }
                }
            }
        }

        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(IntroCutscene.Nested_0), nameof(IntroCutscene.Nested_0.MoveNext))]
        public static class IntroCutScene_CoBegin
        {
            public static bool Prefix(ref bool __result, IntroCutscene.Nested_0 __instance)
            {
                // wait until we set our roles to prevent bugs
                if (!RoleManager.RolesSet)
                {
                    return false;
                }

                if (!PlayerControl.LocalPlayer.HasCustomRole() && PlayerControl.LocalPlayer.Data.IsImpostor)
                {
                    foreach (var player in PlayerControl.AllPlayerControls.ToArray().Where(p => PlayerControl.LocalPlayer.CanSeeSpecial(p)).ToArray())
                    {
                        __instance.yourTeam.Add(player);
                    }
                }
                return __result = true;
            }
            public static void Postfix(bool __result)
            {
                if (!__result) // yield break
                {
                    foreach (var player in PlayerControl.AllPlayerControls.ToArray().Where(p => PlayerControl.LocalPlayer.CanSeeSpecial(p)))
                    {
                        BaseRole? role = player.GetRole();
                        player.nameText.color = role?.Color ?? Palette.ImpostorRed;
                        if (role != null)
                        { 
                            player.nameText.text = role.FormatName(player.Data);
                        }
                    }

                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        player.GetRole()?.OnRoleAssign(player); // called here because before RpcSetInfected PlayerControls are not set yet
                    }
                }
            }
        }

        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
        public static class ExileController_Animate
        {
            public static void Postfix(ExileController __instance)
            {
                if (__instance.exiled == null) return;
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
