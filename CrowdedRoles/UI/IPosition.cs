using CrowdedRoles.Components;
using UnityEngine;

namespace CrowdedRoles.UI
{
    public interface IPosition
    {
        public void AdjustPosition(GameObject gameObject);
    }

    public class AbsolutePosition : IPosition
    {
        public Vector2 Position { get; }
        public AbsolutePosition(Vector2 position)
        {
            Position = position;
        }
        
        public void AdjustPosition(GameObject gameObject)
        {
            gameObject.transform.localPosition = Position;
        }
    }

    public class AdaptivePosition : IPosition
    {
        public Vector2 Position { get; }
        public AspectPosition.EdgeAlignments EdgeAlignment { get; }
        public AspectPosition? AspectPosition { get; private set; }

        public AdaptivePosition(AspectPosition.EdgeAlignments alignment, Vector2 position)
        {
            EdgeAlignment = alignment;
            Position = position;
        }

        public void AdjustPosition(GameObject gameObject)
        {
            if (AspectPosition == null)
            {
                AspectPosition = gameObject.AddComponent<AspectPosition>();
                AspectPosition.Alignment = EdgeAlignment;
                AspectPosition.DistanceFromEdge = Position;
            }
            AspectPosition.AdjustPosition();
        }
    }

    public class AutomaticPosition : IPosition
    {
        public void AdjustPosition(GameObject gameObject)
        {
            ButtonManager.AlignButton(gameObject.GetComponent<CustomButtonManager>().Button);
        }
    }
}