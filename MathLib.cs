using System;

namespace SoftRender.Net
{
    public class MathLib
    {
        public static float Clamp(float value, float min = 0f, float max = 1f)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public static float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Clamp(gradient);
        }
    }
}
