using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace GroupProject.Opening
{
    // One proportional turn path for XR and the desktop preview. No cardinal snapping.
    public sealed class SmoothStickTurnProvider : ContinuousTurnProvider
    {
        public const float DegreesPerSecond = 45f;
        public static float TurnDelta(float horizontal, float seconds, float speed = DegreesPerSecond)
        {
            const float deadzone = .15f;
            float amount = Mathf.Clamp01((Mathf.Abs(horizontal) - deadzone) / (1f - deadzone));
            return Mathf.Sign(horizontal) * amount * speed * Mathf.Max(0, seconds);
        }
        protected override float GetTurnAmount(Vector2 input) =>
            enableTurnLeftRight ? TurnDelta(input.x, Time.deltaTime, turnSpeed) : 0;
    }
}
