using System.Collections;
using PowerTools;
using Reactor;
using UnityEngine;

namespace CrowdedRoles.Extensions
{
    public static class KillAnimationExtensions
    {
        /// <summary>
        /// Api's reimplementation of <see cref="KillAnimation.CoPerformKill"/><br/>
        /// Shouldn't be used in regular code
        /// </summary>
        public static IEnumerator CoPerformCustomKill(this KillAnimation anim, PlayerControl source, PlayerControl target, CustomMurderOptions options)
        {
            FollowerCamera camera = Camera.main!.GetComponent<FollowerCamera>();
            bool isParticipant = source == PlayerControl.LocalPlayer || target == PlayerControl.LocalPlayer;
            PlayerPhysics sourcePhysics = source.MyPhysics;
            KillAnimation.SetMovement(target, false);
            DeadBody deadBody = Object.Instantiate(anim.bodyPrefab); // https://github.com/Herysia/AmongUsTryhard
            deadBody.enabled = false;
            Vector3 vector = target.transform.position + anim.BodyOffset;
            vector.z = vector.y / 1000;
            deadBody.transform.position = vector;
            deadBody.ParentId = target.PlayerId;
            target.SetPlayerMaterialColors(deadBody.bodyRenderer);
            target.SetPlayerMaterialColors(deadBody.bloodSplatter);
            if (isParticipant)
            {
                camera.Locked = true;
                ConsoleJoystick.SetMode_Task();
                if (PlayerControl.LocalPlayer.AmOwner)
                {
                    PlayerControl.LocalPlayer.MyPhysics.inputHandler.enabled = true;
                }
            }

            target.Die(DeathReason.Kill);
            if (!options.HasFlag(CustomMurderOptions.NoSnap))
            {
                KillAnimation.SetMovement(source, false);
                SpriteAnim sourceAnim = source.MyAnim;
                yield return new WaitForAnimationFinish(sourceAnim, anim.BlurAnim);
                source.NetTransform.SnapTo(target.transform.position);
                sourceAnim.Play(sourcePhysics.IdleAnim, 1f);
                KillAnimation.SetMovement(source, true);
            }
            KillAnimation.SetMovement(target, true);
            deadBody.enabled = true;
            if (isParticipant)
            {
                camera.Locked = false;
            }
        }
    }
}