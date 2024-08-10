using System;
using System.Collections.Generic;
using System.Drawing;
using RedUtils.Math;
using RLBotDotNet;

namespace RedUtils
{
    /// <summary>An action that will attempt to hit without considering direction much.</summary>
	public class QuickShot : IAction
	{
        /// <summary>Whether this action has finished</summary>
        public bool Finished { get; private set; }
        /// <summary>Whether this action can be interrupted</summary>
        public bool Interruptible { get; private set; }

        /// <summary>The location we're going to shoot at. Often just a point behind the ball</summary>
        public Vec3 Target;
        /// <summary>This action's arrive sub action, which will take us to the ball</summary>
        public Arrive ArriveAction;

        /// <summary>Keeps track of the last time the ball was touched</summary>
        private float _latestTouchTime = -1;

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
            float roughEta = bot.Me.Location.Dist(Ball.Location) / 2300f;
            Vec3 roughBallLoc = Ball.Prediction.AtTime(Game.Time + roughEta)?.Location ?? Ball.Location;
            Vec3 behindBall = roughBallLoc + bot.Me.Location.Direction(roughBallLoc) * 1000f;
            Target = Utils.Lerp(0.15f, behindBall, bot.TheirGoal.Location);
            bot.Renderer.Octahedron(behindBall, 90, Color.Orange);
            bot.Renderer.Octahedron(Target, 100, Color.Red);
            
            // Calculates the direction we should shoot in
            Vec3 shotDirection = roughBallLoc.Direction(Target);
            // Gets the normal of the surface closest to the ball
            Vec3 surfaceNormal = Field.NearestSurface(roughBallLoc).Normal;

            // Updates and runs the arrive action
            ArriveAction.Target = roughBallLoc;
            ArriveAction.Direction = shotDirection;
            ArriveAction.Run(bot);

            // This action is only interruptible when the arrive action is
            Interruptible = ArriveAction.Interruptible;
            if (Interruptible && Ball.LatestTouch != null && _latestTouchTime != Ball.LatestTouch.Time && Ball.LatestTouch.PlayerIndex == bot.Index)
            {
                // If we have hit the ball, finish this action
                Finished = true;
            }
            else if (Interruptible && ArriveAction.TimeRemaining < 0.2f || bot.Me.Location.WithY(bot.Me.Location.y / 3).Dist(Ball.Location) < 300)
            {
                // When we are close enough, dodge into the ball
                bot.Action = new Dodge(bot.Me.Location.FlatDirection(Ball.Location, surfaceNormal));
            }
        }
	}
}
