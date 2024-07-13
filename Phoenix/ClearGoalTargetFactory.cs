using RedUtils;
using RedUtils.Math;
using RLBotDotNet;

namespace Phoenix
{
    public class ClearGoalTargetFactory : ITargetFactory
    {
        private Goal _ourGoal;

        public ClearGoalTargetFactory(Goal ourGoal)
        {
            _ourGoal = ourGoal;
        }

        public Target GetTarget(Car car, BallSlice slice)
        {
            Vec3 carToBallDir = (slice.Location - car.Location).Normalize();
            Vec3 goalToBallDir = (slice.Location - _ourGoal.Location).Normalize();
            Vec3 dir = (carToBallDir + goalToBallDir).Flatten().Normalize();
            Vec3 perp = new Vec3(-dir.y, dir.x, 0);
            Vec3 left = slice.Location + dir * 369 + perp * 555;
            Vec3 right = slice.Location + dir * 369 - perp * 555;
            return new Target(left.WithZ(Goal.Height + 200), right.WithZ(0));
        }
    }
}
