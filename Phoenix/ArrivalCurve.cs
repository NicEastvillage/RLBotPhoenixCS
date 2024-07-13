using RedUtils;
using RedUtils.Math;

namespace Phoenix
{
    public static class ArrivalCurve
    {
        /// <summary>Essentially returns a point equally far from `to` and `from` on the line given through
        /// `to` parallel with `dir`. If continuously driven towards, a circular curve emerges.</summary>
        public static Vec3 GetMidPoint(Vec3 from, Vec3 to, Vec3 arriveDir)
        {
            var dir = arriveDir.Normalize();

            var t = -(to.x * to.x - 2 * to.x * from.x + to.y * to.y - 2 * to.y * from.y + from.x * from.x + from.y * from.y) /
                    (2 * (to.x * dir.x + to.y * dir.y - from.x * dir.x - from.y * dir.y));
            t = Utils.Cap(t, -1700, 1700);

            return to + t * dir;
        } 
    }
}
