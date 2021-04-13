using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using ListButtonMatrix = System.Collections.Generic.List<System.Collections.Generic.List<AspectPosition>>;

namespace CrowdedRoles.UI
{
    public static class ButtonManager
    {
        internal static List<CooldownButton> RegisteredButtons { get; } = new();
        internal static List<CooldownButton> ActiveButtons { get; } = new();
        private static int _currentIndex;

        private const float Delta = 0.7f;
        private const float ButtonSize = 0.5f; // it is hard-coded for now

        private static int MaxButtons => (int) ((Screen.safeArea.height - Delta) / ButtonSize);

        public static void AlignButton(CooldownButton button)
        {
            if (button.alignIndex == -1) button.alignIndex = _currentIndex++;

            var aspectPosition = button.CustomButtonManager.gameObject.GetComponent<AspectPosition>();
            if (aspectPosition == null)
            {
                aspectPosition = button.CustomButtonManager.gameObject.AddComponent<AspectPosition>();
            }
            aspectPosition.Alignment = AspectPosition.EdgeAlignments.LeftBottom;

            aspectPosition.DistanceFromEdge = new Vector3((Delta + ButtonSize) * (button.alignIndex / MaxButtons) + Delta, (Delta + ButtonSize) * (button.alignIndex % MaxButtons) + Delta);
            aspectPosition.AdjustPosition();
        }

        public static T? GetInstance<T>(bool activeOnly = true) where T : CooldownButton
            => (activeOnly ? ActiveButtons : RegisteredButtons).FirstOrDefault(b => b is T) as T;

        internal static void ResetButtons()
        {
            foreach (var button in ActiveButtons)
            {
                button.alignIndex = -1;
            }
            ActiveButtons.Clear();
            _currentIndex = 0;
        }

        public static void AddButton(CooldownButton button)
        {
            ActiveButtons.Add(button);
            button.Active = true;
        }
    }

    public static class ButtonSingleton<T> where T : CooldownButton
    {
        /// <summary>
        /// Gets an instance of active (!) button
        /// </summary>
        /// <exception cref="NullReferenceException">If button is not active/registered</exception>
        public static T Instance => ButtonManager.ActiveButtons.Single(b => b is T) as T ?? throw new NullReferenceException($"Button {typeof(T).FullDescription()} is not registered or/and active");

        /// <summary>
        /// Gets an instance of registered button. Please use carefully because buttons get destroyed on game start if <see cref="CooldownButton.DontDestroyOnGameStart"/> is false (default)
        /// </summary>
        /// <exception cref="NullReferenceException">If button is not registered/destroyed</exception>
        public static T RegisteredInstance => ButtonManager.ActiveButtons.Single(b => b is T) as T ?? throw new NullReferenceException($"Button {typeof(T).FullDescription()} is not registered or destroyed");
    }
}