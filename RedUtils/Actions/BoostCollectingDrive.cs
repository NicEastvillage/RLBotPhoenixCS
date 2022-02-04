using System;
using System.Drawing;
using System.Linq;
using RedUtils.Math;
using RLBotDotNet;

namespace RedUtils
{
    public class BoostCollectingDrive : IAction
    {
        /// <summary>Whether or not this action is finished</summary>
        public bool Finished { get; set; }

        /// <summary>Whether or not this action can be interrupted</summary>
        public bool Interruptible { get; set; }

        /// <summary>The boost pad we are going to grab next</summary>
        public Boost ChosenBoost;
        
        /// <summary>The boost pad we are going to grab after the next</summary>
        public Boost ChosenBoost2;

        /// <summary>This action's drive subaction</summary>
        public Arrive ArriveAction;

        public readonly Vec3 FinalDestination;

        private int _tick = 0;

        /// <summary>Whether or not this action was initially set as interruptible</summary>
        private readonly bool _initiallyInterruptible;

        public BoostCollectingDrive(Car car, Vec3 finalDestination, bool interruptible = true)
        {
            FinalDestination = finalDestination;
            _initiallyInterruptible = interruptible;

            ArriveAction = new Arrive(car, finalDestination);
        }

        public void Run(RUBot bot)
        {
            float distToTarget = bot.Me.Location.Dist(FinalDestination);

            _tick++;
            if (_tick >= 20 || (ChosenBoost != null && ChosenBoost.TimeUntilActive >= 2.8f))
            {
                _tick = 0;

                // Repick boost
                ChosenBoost = Field.Boosts.FindAll(boost =>
                    {
                        float distToBoost = boost.Location.Dist(bot.Me.Location);
                        return (boost.IsActive || distToBoost / boost.TimeUntilActive > bot.Me.Velocity.Length()) &&
                               distToBoost + boost.Location.Dist(FinalDestination) < 1.3f * distToTarget;
                    })
                    .OrderBy(boost => 1.25f * boost.Location.Dist(bot.Me.Location) +
                                      1.0f * boost.Location.Dist(FinalDestination) +
                                      0.75f * MathF.Abs(bot.Me.Right.Dot(boost.Location - bot.Me.Location)))
                    .FirstOrDefault();

                if (ChosenBoost == null) ChosenBoost2 = null;
                else
                {
                    ChosenBoost2 = Field.Boosts.FindAll(boost =>
                        {
                            float boostToBoostDist = boost.Location.Dist(ChosenBoost.Location);
                            return boost != ChosenBoost && (boost.IsActive || (ChosenBoost.Location.Dist(bot.Me.Location) + boostToBoostDist) / boost.TimeUntilActive > bot.Me.Velocity.Length()) &&
                                   boostToBoostDist + boost.Location.Dist(FinalDestination) < 1.25f * distToTarget;
                        })
                        .OrderBy(boost => 1.25f * boost.Location.Dist(ChosenBoost.Location) +
                                          1.0f * boost.Location.Dist(FinalDestination) +
                                          1.0f * MathF.Abs(ChosenBoost.Location.Direction(FinalDestination).Rotate(MathF.PI / 2f).Dot(boost.Location - bot.Me.Location)))
                        .FirstOrDefault();
                }

                // Update Drive sub action
                ArriveAction.Target = ChosenBoost?.Location ?? FinalDestination;
                ArriveAction.Direction = ChosenBoost2 != null ?
                    Utils.Lerp(0.5f, bot.Me.Forward, bot.Me.Location.Direction(ChosenBoost2.Location)).FlatNorm() :
                    Utils.Lerp(0.5f, bot.Me.Forward, bot.Me.Location.Direction(FinalDestination)).FlatNorm();
                ArriveAction.Drive.WasteBoost = bot.Me.Velocity.Length() < 500 || distToTarget > 2500f;
            }

            if (ChosenBoost != null) bot.Renderer.Line3D(bot.Me.Location, ChosenBoost.Location.WithZ(20f), Color.GreenYellow);
            if (ChosenBoost != null && ChosenBoost2 != null) bot.Renderer.Line3D(ChosenBoost.Location.WithZ(20f), ChosenBoost2.Location.WithZ(20f), Color.GreenYellow);
            
            ArriveAction.Run(bot);

            Interruptible = _initiallyInterruptible && ArriveAction.Interruptible;
            Finished = bot.Me.Boost > 65 || bot.Me.Location.Dist(FinalDestination) < 100;
        }
    }
}
