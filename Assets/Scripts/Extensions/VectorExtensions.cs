using UnityEngine;

namespace TurnBasedStrategy.Extensions
{
    public static class VectorExtensions
    {
        public static Vector3 Flat(this Vector3 vector)
        {
            return new Vector3(vector.x, 0, vector.z);
        }
    }
}
