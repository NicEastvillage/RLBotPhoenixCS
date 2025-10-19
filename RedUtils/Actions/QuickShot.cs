using System;
using System.Collections.Generic;
using System.Drawing;
using RedUtils.Math;

namespace RedUtils
{
    /// <summary>An action that will attempt to hit without considering direction much.</summary>
	public class QuickShot : IAction
	{
        /// <summary>Whether this action has finished</summary>
        public bool Finished { get; private set; }
        /// <summary>Whether this action can be interrupted</summary>
        public bool Interruptible { get; private set; }
        
        public bool Navigational => false;

        /// <summary>The location we're going to shoot at. Often just a point behind the ball</summary>
        public Vec3 Target;
        /// <summary>This action's arrive sub action, which will take us to the ball</summary>
        public Arrive ArriveAction;

        /// <summary>Keeps track of the last time the ball was touched</summary>
        private float _latestTouchTime = -1;

        private float _predTao = -1f;

        public QuickShot(Car car)
        {
            Interruptible = true;

            Target = Ball.Location;
            ArriveAction = new Arrive(car, Ball.Location);
        }

		public void Run(RUBot bot)
		{
            // Updates latest touch time with the last time the ball was hit
            if (_latestTouchTime < 0 && Ball.LatestTouch != null)
                _latestTouchTime = Ball.LatestTouch.Time;

            // Find some reasonable target
            var (eta, ballLoc) = GetPredBallLocBruteforce(bot);
            Vec3 behindBall = ballLoc + bot.Me.Location.Direction(ballLoc) * 1000f;
            Target = Utils.Lerp(0.15f, behindBall, bot.TheirGoal.Location);
            bot.Renderer.Polyline3D(new List<Vec3> { Ball.Location, ballLoc, Target }, Color.Gray);
            bot.Renderer.Octahedron(ballLoc, 200, Color.Orange);
            bot.Renderer.CrossAngled(Target, 100, Color.Orange);
            
            // Calculates the direction we should shoot in
            Vec3 shotDirection = ballLoc.Direction(Target);
            // Gets the normal of the surface closest to the ball
            Vec3 surfaceNormal = Field.NearestSurface(ballLoc).Normal;

            // Updates and runs the arrive action
            ArriveAction.Target = ballLoc;
            ArriveAction.Direction = shotDirection;
            ArriveAction.Run(bot);
            bot.Throttle(bot.Me.Location.Dist(ballLoc.WithZ(0f)) / MathF.Max(eta, 0.01f));

            // This action is only interruptible when the arrive action is
            Interruptible = ArriveAction.Interruptible;
            if (Interruptible && Ball.LatestTouch != null && _latestTouchTime != Ball.LatestTouch.Time && Ball.LatestTouch.PlayerIndex == bot.Index)
            {
                // If we have hit the ball, finish this action
                Finished = true;
            }
            else if (Interruptible && (ArriveAction.TimeRemaining + eta) / 2f < 0.2f || bot.Me.Location.WithZ(bot.Me.Location.z / 3).Dist(Ball.Location) < 300f)
            {
                // When we are close enough, dodge into the ball
                Vec3 dodgeLoc = Ball.Prediction.InTime((ArriveAction.TimeRemaining + eta) / 3f)?.Location ?? Ball.Location;
                bot.Action = new Dodge(bot.Me.Location.FlatDirection(dodgeLoc, surfaceNormal));
            }
        }

        private (float, Vec3) GetPredBallLocBruteforce(RUBot bot)
        {
            if (Ball.Prediction.Length == 0) return (bot.Me.Location.Dist(Ball.Location) / Car.MaxSpeed, Ball.Location);
            if (_predTao < 0) _predTao = Game.Time;

            const int stepCount = 25;
            const float stepSize = 2f / 120f;
            float eta = MathF.Max(Game.Time, _predTao - stepSize * (stepCount - 1) / 2f) - Game.Time;
            for (int i = 0; i < stepCount; i++, eta += stepSize)
            {
                Vec3 loc = Ball.Prediction.InTime(eta).Location;
                if (bot.Me.Location.Dist(loc) < eta * Car.MaxSpeed)
                {
                    break; // Found reachable
                }
            }
            
            bot.Renderer.Octahedron(Ball.Prediction.InTime(eta).Location, 180, Color.Red);

            // Wait extra for it to get close to some surface
            for (; eta < 6f; eta += stepSize)
            {
                Vec3 loc = Ball.Prediction.InTime(eta).Location;
                Surface surface = Field.NearestSurface(loc);
                if (surface.Limit(loc).DistSquared(loc) <= 180f * 180f)
                {
                    break;
                }
            }
            
            _predTao = Game.Time + eta;
            return (eta, Ball.Prediction.InTime(eta).Location);
        }
    }
}
