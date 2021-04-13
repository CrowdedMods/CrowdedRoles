using System;
using CrowdedRoles.UI;
using Reactor;
using TMPro;
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
        public TextMeshPro TimerText { get; internal set; } = null!;

        private float _timer;
        private bool _executedCooldownEnd;
        private bool _effectEnabled;
        private static readonly int Percent = Shader.PropertyToID("_Percent");

        [HideFromIl2Cpp]
        public bool IsEffectEnabled
        {
            get => _effectEnabled;
            set
            {
                if (!_effectEnabled && value)
                {
                    Timer = Button.EffectDuration;
                    Button.Triggered = false;
                    Button.OnEffectStart();
                } else if (_effectEnabled && !value)
                {
                    Timer = Button.MaxTimer;
                    Button.OnEffectEnd();
                }

                _effectEnabled = value;
            }
        }
        
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
                        Renderer.material.SetFloat(Percent, Mathf.Clamp(_timer / max, 0f, 1f));
                    }
                }

                if (value > 0)
                {
                    TimerText.text = Mathf.CeilToInt(_timer).ToString();
                    if (Button.Visible)
                    {
                        TimerText.gameObject.SetActive(true);
                    }
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
            if (!isActiveAndEnabled || !Button.Visible || Button.IsCoolingDown || IsEffectEnabled) return;

            if (Button.OnClick())
            {
                _executedCooldownEnd = false; // workaround to prevent bugs if EffectDuration is 0
                IsEffectEnabled = true;
            }
        }

        public void OnEnable()
        {
            Button.Position.AdjustPosition(gameObject);
        }

        public void Start()
        {
            Renderer = gameObject.GetComponent<SpriteRenderer>();
            Timer = Button.MaxTimer;
            Button.Triggered = false;

            var button = gameObject.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener((Action)OnClick);
            
            Button.Sprite = Button.DefaultSprite;
            Button.OnStart();
        }

        public void Update()
        {
            if (Button.IsCoolingDown && Button.ShouldCooldown())
            {
                Timer -= Time.deltaTime;
            } 
            if (!Button.IsCoolingDown && !_executedCooldownEnd)
            {
                _executedCooldownEnd = true;
                if (IsEffectEnabled)
                {
                    IsEffectEnabled = false;
                    _executedCooldownEnd = false;
                }
                else
                {
                    Button.OnCooldownEnd();
                }
            }
            Button.OnUpdate();
        }
    }
}