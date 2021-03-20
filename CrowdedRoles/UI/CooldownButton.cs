using CrowdedRoles.Components;
using UnityEngine;

namespace CrowdedRoles.UI
{
    public enum SetActiveReason : byte
    {
        Hud = 0,
        Die = 1,
        Revive = 2
    }
    public abstract class CooldownButton
    {
        public abstract float MaxTimer { get; }
        public abstract Sprite DefaultSprite { get; }

        public float Timer
        {
            get => CustomButtonManager.Timer;
            set => CustomButtonManager.Timer = value;
        }
        
        public Sprite Sprite
        {
            get => CustomButtonManager.Renderer.sprite;
            set => CustomButtonManager.Renderer.sprite = value;
        }

        public bool Visible
        {
            get => CustomButtonManager.gameObject.active;
            set => CustomButtonManager.gameObject.SetActive(value);
        }

        // ReSharper disable once IdentifierTypo
        // ReSharper disable once StringLiteralTypo
        private static readonly int Desat = Shader.PropertyToID("_Desat");
        private bool _activated;

        public bool Activated
        {
            get => _activated;
            set
            {
                _activated = value;
                if (value)
                {
                    CustomButtonManager.Renderer.color = Palette.EnabledColor;
                    CustomButtonManager.Renderer.material.SetFloat(Desat, 0f);
                }
                else
                {
                    CustomButtonManager.Renderer.color = Palette.DisabledColor;
                    CustomButtonManager.Renderer.material.SetFloat(Desat, 1f);
                }
            }
        }

        public bool IsCoolingDown => Timer > 0f;

        public CustomButtonManager CustomButtonManager { get; internal set; } = null!;

        public virtual float EffectDuration => 0f;
        public virtual Vector2 Size => new(125, 125);

        public abstract bool OnClick();
        public abstract bool CanUse();

        public virtual bool ShouldSetActive(bool expected, SetActiveReason reason) => expected;
        public virtual bool ShouldCooldown() => PlayerControl.LocalPlayer.CanMove;
        public virtual void OnEffectStart() {}
        public virtual void OnEffectEnd() {}
        public virtual void OnCooldownEnd() {}

        public virtual void OnStart()
        {
            Sprite = DefaultSprite;
        }
        public virtual void OnFixedUpdate() {}
    }
}