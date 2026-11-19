using UnityEngine;

namespace ARFunctionLab
{
    public static class GraphCoordinates
    {
        public static Vector3 ToUnity(double x, double y, double z)
        {
            return new Vector3((float)x, (float)z, (float)y);
        }

        public static Vector3 ToMath(Vector3 position)
        {
            return new Vector3(position.x, position.z, position.y);
        }
    }
}
