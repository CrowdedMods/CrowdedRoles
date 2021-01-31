using System;
using System.Text;
using CrowdedRoles.Api.Roles;
using Reactor;
using Reactor.Extensions;
using UnityEngine;

namespace CrowdedRoles.Api.Components
{
    [RegisterInIl2Cpp]
    public class CustomGameOptions : MonoBehaviour
    {
        public CustomGameOptions(IntPtr ptr) : base(ptr)
        {
        }
        
        public bool hasChanged;
        public TextRenderer text = new();

        public void Start()
        {
            text = gameObject.AddTextRenderer();
            text.RightAligned = true;
            text.scale = DestroyableSingleton<HudManager>.Instance.GameSettings.scale;
            
            var myAspect = gameObject.AddComponent<AspectPosition>();
            myAspect.Alignment = AspectPosition.EdgeAlignments.RightTop;
            myAspect.DistanceFromEdge = new Vector3(
                0.1f,
                DestroyableSingleton<HudManager>.Instance.transform.FindChild("MenuButton")?.lossyScale.y ?? 1.5f,
                myAspect.DistanceFromEdge.z
            );
            myAspect.AdjustPosition();

            UpdateText();
        }

        private void UpdateText()
        {
            var builder = new StringBuilder();

            builder.AppendLine("Limits:");
            foreach ((var role, byte limit) in RoleManager.EditableLimits)
            {
                builder.AppendLine($"{role.Name}: {limit}");
            }

            text.Text = builder.ToString();
        }
    }
}