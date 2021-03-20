using System;
using CrowdedRoles.UI;
using Reactor;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace CrowdedRoles.Components
{
    [RegisterInIl2Cpp]
    public class CustomButtonManager : MonoBehaviour
    {
        public CustomButtonManager(IntPtr ptr) : base(ptr)
        {
        }

        public SpriteRenderer Renderer { get; private set; } = null!;
        public TextRenderer TimerText { get; internal set; } = null!;

        private float _timer;
        private bool _executedCooldownEnd;
        private static readonly int Percent = Shader.PropertyToID("_Percent");

        [HideFromIl2Cpp]
        public bool IsEffectEnabled { get; set; }
        
        [HideFromIl2Cpp]
        public float Timer
        {
            get => _timer;
            set
            {
                if (value <= 0 && _timer > 0 )
                {
                    _executedCooldownEnd = false;
                }
                _timer = Mathf.Max(value, 0f);
            
                if (Renderer)
                {
                    var max = IsEffectEnabled ? Button.EffectDuration : Button.MaxTimer;
                    if (max == 0)
                    {
                        Renderer.material.SetFloat(Percent, 0);
                    }
                    else
                    {
                        Renderer.material.SetFloat(Percent, Mathf.Min(_timer / max, 1f));
                    }
                }

                if (value > 0)
                {
                    TimerText.Text = Mathf.CeilToInt(_timer).ToString();
                    TimerText.gameObject.SetActive(true);
                }
                else
                {
                    TimerText.gameObject.SetActive(false);
                }
            }
        }

        [HideFromIl2Cpp] 
        public CooldownButton Button { get; set; } = null!;
        
        private void OnClick()
        {
            if (!isActiveAndEnabled || Button.IsCoolingDown || IsEffectEnabled) return;

            if (Button.OnClick())
            {
                _executedCooldownEnd = false; // workaround to prevent bugs if EffectDuration is 0
                Timer = Button.EffectDuration;
                IsEffectEnabled = true;
                Button.Activated = false;
                Button.OnEffectStart();
            }
        }

        public void OnEnable()
        {
            var transform1 = transform;
            var pos = transform1.localPosition;
            pos.x -= 1.3f;
            transform1.localPosition = pos;
        }

        public void Start()
        {
            Renderer = gameObject.GetComponent<SpriteRenderer>();
            Timer = Button.MaxTimer;
            Button.Activated = false;

            var button = gameObject.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((Action)OnClick);
            
            CooldownHelpers.SetCooldownNormalizedUvs(Renderer);
            Button.OnStart();
        }

        public void FixedUpdate()
        {
            if (Button.IsCoolingDown && Button.ShouldCooldown())
            {
                Timer -= Time.fixedDeltaTime;
            } 
            if (!Button.IsCoolingDown && !_executedCooldownEnd)
            {
                _executedCooldownEnd = true;
                if (IsEffectEnabled)
                {
                    IsEffectEnabled = false;
                    _executedCooldownEnd = false;
                    Timer = Button.MaxTimer;
                    Button.OnEffectEnd();
                }
                else
                {
                    Button.OnCooldownEnd();
                }
            }
            Button.OnFixedUpdate();
        }
    }
}