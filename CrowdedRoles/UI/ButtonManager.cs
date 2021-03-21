using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ListButtonMatrix = System.Collections.Generic.List<System.Collections.Generic.List<AspectPosition>>;

namespace CrowdedRoles.UI
{
    internal static class ButtonManager
    {
        public static List<CooldownButton> RegisteredButtons { get; } = new();
        public static List<CooldownButton> ActiveButtons { get; } = new();
        public static ListButtonMatrix AlignedButtons { get; } = new();
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

        public static void ResetButtons()
        {
            foreach (var button in ActiveButtons)
            {
                button.alignIndex = -1;
            }
            ActiveButtons.Clear();
            AlignedButtons.Clear();
            _currentIndex = 0;
        }
    }
}