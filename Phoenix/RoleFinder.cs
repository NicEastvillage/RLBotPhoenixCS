using RedUtils;
using RLBotDotNet;

namespace Phoenix
{
    public enum Role
    {
        /// <summary>Take a shot, ball chase, dribble, or challenge</summary>
        Attack,
        /// <summary>Prepare to take a shot (at opponent goal or away from our goal) once the attacker loses possession or passes.</summary>
        Assist,
        /// <summary>Get to goal using other half of map and stay close to our goal</summary>
        Defend,
    }
    
    public class RoleFinder
    {
        /// <summary>Roles of all cars. Only allies roles are updated</summary>
        public Role[] Roles { get; private set; }

        private Car _prevAttacker;
        private Car _prevAssister;
        
        /// <summary>Update role of all allies. Returns my role.</summary>
        public Role Update(PhoenixBot bot)
        {
            if (Roles == null || Cars.Count != Roles.Length)
            {
                Roles = new Role[Cars.Count];
            }

            foreach (var car in Cars.AlliesAndMe)
            {
                Roles[car.Index] = Role.Defend;
            }
            
            var attacker = FindBestAttacker(bot) ?? Cars.Me;
            Roles[attacker.Index] = Role.Attack;

            var assister = FindBestAssister(bot, attacker);
            if (assister != null) Roles[assister.Index] = Role.Assist;

            _prevAttacker = attacker;
            _prevAssister = assister;
            
            return Roles[bot.Me.Index];
        }
        
        /// <summary>Returns car in position for attack role.</summary>
        private Car FindBestAttacker(PhoenixBot bot)
        {
            Car attacker = null;
            var attackerValue = float.MaxValue;
            var ballToGoal = bot.OurGoal.Location - Ball.Location;
            foreach (var car in Cars.AlliesAndMe)
            {
                // Does the car have enough boost to reach the balls height?
                if ((Ball.Location.z + Ball.Velocity.z) / 28 > car.Boost + 15) continue;
                
                // Is the car falling? 
                if (car.Velocity.z < -500 && !car.IsGrounded) continue;
                
                var locProjOntoBallToGoal = car.Location.ProjToLineSegment(Ball.Location, bot.OurGoal.Location);
                var distToProj = car.Location.Dist(locProjOntoBallToGoal);
                var angle = (car.Location - Ball.Location).Angle(ballToGoal);
                var value = distToProj + distToProj * angle / 1.5f + car.Location.Dist(Ball.Location) / 5;
                if (car == _prevAttacker) value -= 600;
                else if (car == _prevAssister) value -= 200;
                if (value < attackerValue)
                {
                    attacker = car;
                    attackerValue = value;
                }
            }

            return attacker;
        }

        private Car FindBestAssister(PhoenixBot bot, Car attacker)
        {
            Car preparer = null;
            float preparerValue = float.MaxValue;
            
            foreach (var car in Cars.AlliesAndMe)
            {
                if (car == attacker) continue;
                
                // Must be further from the opponent goal than the attacker - otherwise out of position.
                if (car.Location.DistSquared(bot.TheirGoal.Location) <
                    attacker.Location.DistSquared(bot.TheirGoal.Location))
                    continue;

                float value = car.Location.Dist(Ball.Location);
                if (car == _prevAssister) value -= 400f;
                if (value < preparerValue)
                {
                    preparer = car;
                    preparerValue = value;
                }
            }

            return preparer;
        }
    }
}
