using System;
using System.Linq;
using System.Text;
using CrowdedRoles.Options;
using CrowdedRoles.Roles;
using Reactor;
using Reactor.Extensions;
using UnityEngine;

namespace CrowdedRoles.Components
{
    [RegisterInIl2Cpp]
    public class CustomGameOptions : MonoBehaviour
    {
        public CustomGameOptions(IntPtr ptr) : base(ptr)
        {
        }
        
        public TextRenderer text = new();

        public void Start()
        {
            text = gameObject.AddTextRenderer();
            text.RightAligned = true;
            text.scale = 0.6f;
            
            var myAspect = gameObject.AddComponent<AspectPosition>();
            myAspect.Alignment = AspectPosition.EdgeAlignments.RightTop;
            myAspect.DistanceFromEdge = new Vector3(
                0.1f,
                HudManager.Instance.transform.FindChild("MenuButton")?.lossyScale.y ?? 1.5f,
                myAspect.DistanceFromEdge.z
            );
            myAspect.AdjustPosition();

            foreach (var option in OptionsManager.CustomOptions.SelectMany(p => p.Value))
            {
                if (option is CustomNumberOption opt)
                {
                    opt.Value = opt.Value; // update ValueText if TranslationController has changed its language
                }
            }

            UpdateText();
        }

        internal void UpdateText()
        {
            var builder = new StringBuilder();

            builder.AppendLine("Limits:");
            foreach ((var role, byte limit) in RoleManager.EditableLimits)
            {
                builder.AppendLine($"{role.Name}: {limit}");
            }
            
            builder.AppendLine();

            foreach (var option in OptionsManager.CustomOptions.SelectMany(p => p.Value))
            {
                builder.AppendLine($"{option.Name}: {option.ValueText}");
            }

            text.Text = builder.ToString();
        }
    }
}