using System;
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

        /// <summary>This action's drive subaction</summary>
        public Drive DriveAction;

        public readonly Vec3 FinalDestination;

        private int _tick = 0;

        /// <summary>Whether or not this action was initially set as interruptible</summary>
        private readonly bool _initiallyInterruptible;

        public BoostCollectingDrive(Car car, Vec3 finalDestination, bool interruptible = true)
        {
            FinalDestination = finalDestination;
            _initiallyInterruptible = interruptible;

            DriveAction = new Drive(car, finalDestination);
        }

        public void Run(RUBot bot)
        {
            float distToTarget = bot.Me.Location.Dist(FinalDestination);

            _tick++;
            if (_tick >= 8 || (ChosenBoost != null && ChosenBoost.TimeUntilActive >= 2.8f))
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
                                      0.8f * MathF.Abs(bot.Me.Right.Dot(boost.Location)))
                    .FirstOrDefault();

                // Update Drive subaction
                DriveAction.Target = ChosenBoost?.Location ?? FinalDestination;
                DriveAction.WasteBoost = bot.Me.Velocity.Length() < 400;
            }

            DriveAction.Run(bot);

            Interruptible = _initiallyInterruptible && DriveAction.Interruptible;
            Finished = bot.Me.Boost > 65;
        }
    }
}
