using UnityEngine;

namespace Harvest
{
    public enum FootstepSurfaceKind { Gravel, Wood }
    // Attach to walkable colliders; unmarked surfaces use the actor's default bank.
    public sealed class FootstepSurface : MonoBehaviour
    {
        public FootstepSurfaceKind Kind = FootstepSurfaceKind.Wood;
    }
}
