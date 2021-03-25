using CrowdedRoles.Components;
using UnityEngine;

namespace CrowdedRoles.UI
{
    /// <summary>
    /// Interface declaring button position behaviour 
    /// </summary>
    public interface IPosition
    {
        /// <summary>
        /// Method updating button position. Gets called in <see cref="CustomButtonManager.OnEnable"/>
        /// </summary>
        /// <param name="gameObject">button object</param>
        public void AdjustPosition(GameObject gameObject);
    }

    /// <summary>
    /// Absolute position by <see cref="Vector3"/>
    /// </summary>
    public class AbsolutePosition : IPosition
    {
        public Vector3 Position { get; }
        public AbsolutePosition(Vector3 position)
        {
            Position = position;
        }
        
        public void AdjustPosition(GameObject gameObject)
        {
            gameObject.transform.localPosition = Position;
        }
    }

    /// <summary>
    /// Adaptive position using <see cref="AspectPosition"/>
    /// </summary>
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

    /// <summary>
    /// Managed by api button alignment to left corner (the best)
    /// </summary>
    public class AutomaticPosition : IPosition
    {
        public void AdjustPosition(GameObject gameObject)
        {
            ButtonManager.AlignButton(gameObject.GetComponent<CustomButtonManager>().Button);
        }
    }
}