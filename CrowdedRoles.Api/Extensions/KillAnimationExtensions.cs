using System.Collections;
using PowerTools;
using UnityEngine;

namespace CrowdedRoles.Api.Extensions
{
    public static class KillAnimationExtensions
    {
        public static IEnumerator CoPerformCustomKill(this KillAnimation anim, PlayerControl source, PlayerControl target, bool noSnap)
        {
            FollowerCamera camera = Camera.main!.GetComponent<FollowerCamera>();
            bool isParticipant = source == PlayerControl.LocalPlayer || target == PlayerControl.LocalPlayer;
            KillAnimation.SetMovement(source, false);
            KillAnimation.SetMovement(target, false);
            if (isParticipant)
            {
                camera.Locked = true;
            }
            target.Die(DeathReason.Kill);
            SpriteAnim sourceAnim = source.GetComponent<SpriteAnim>();
            yield return new WaitForAnimationFinish(sourceAnim, anim.BlurAnim);
            if (!noSnap)
            {
                source.NetTransform.SnapTo(target.transform.position);
            }
            sourceAnim.Play(source.MyPhysics.IdleAnim, 1f);
            KillAnimation.SetMovement(source, true);
            DeadBody deadBody = Object.Instantiate(anim.bodyPrefab);
            Vector3 vector = target.transform.position + anim.BodyOffset;
            vector.z = vector.y / 1000;
            deadBody.transform.position = vector;
            deadBody.ParentId = target.PlayerId;
            target.SetPlayerMaterialColors(deadBody.GetComponent<Renderer>());
            KillAnimation.SetMovement(target, true);
            if (isParticipant)
            {
                camera.Locked = false;
            }
        }
    }
}