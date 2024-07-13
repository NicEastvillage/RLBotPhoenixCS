using System.Drawing;
using RedUtils;

namespace Phoenix
{
    public enum Role
    {
        /// <summary>Take a shot or drive towards ball</summary>
        Attack,
        /// <summary>Line up to shoot towards opponent goal or away from our goal.
        /// Approach if already in position ball </summary>
        Prepare,
        /// <summary>Get to goal using other half of map and stay close to our goal</summary>
        Defend,
    }
    
    public class RoleFinder
    {
        /// <summary>Roles of all cars. Only allies roles are updated</summary>
        public Role[] Roles { get; private set; }

        /// <summary>Update role of all allies. Returns my role.</summary>
        public Role Update(PhoenixBot bot)
        {
            if (Roles == null || Cars.Count != Roles.Length)
            {
                Roles = new Role[Cars.Count];
            }

            foreach (var car in Cars.AlliesAndMe)
            {
                Roles[car.Index] = Role.Prepare;
            }
            
            var attacker = FindBestAttacker(bot);
            if (attacker != null)
            {
                Roles[attacker.Index] = Role.Attack;                
            }
            else
            {
                Roles[bot.Index] = Role.Attack;
            }

            return Roles[bot.Me.Index];
        }
        
        /// <summary>Returns car in position for attack role.</summary>
        public Car FindBestAttacker(PhoenixBot bot)
        {
            Car attacker = null;
            var attackerValue = float.MaxValue;
            var ballToGoal = bot.OurGoal.Location - Ball.Location;
            foreach (var car in Cars.AlliesAndMe)
            {
                // Does the car have enough boost to reach the balls height?
                if ((Ball.Location.z + Ball.Velocity.z) / 28 > car.Boost + 15)
                {
                    continue;
                }
                // Is the car falling? 
                if (car.Velocity.z < -200 && !car.IsGrounded)
                {
                    continue;
                }
                
                var locProjOntoBallToGoal = car.Location.ProjToLineSegment(Ball.Location, bot.OurGoal.Location);
                var distToProj = car.Location.Dist(locProjOntoBallToGoal);
                var angle = (car.Location - Ball.Location).Angle(ballToGoal);
                var value = distToProj + distToProj * angle / 1.5f;
                if (value < attackerValue)
                {
                    attacker = car;
                    attackerValue = value;
                }
            }

            return attacker;
        }
    }
}
