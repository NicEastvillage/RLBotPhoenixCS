using System;
using RedUtils;

namespace Phoenix
{
    /// <summary>TargetFactory for targets that do not depend on the car or the ball slice</summary>
    public class StaticTargetFactory : ITargetFactory
    {
        public readonly Target Target;

        public StaticTargetFactory(Target target)
        {
            Target = target;
        }

        public Target? GetTarget(Car car, BallSlice slice)
        {
            return Target;
        }
    }
}
