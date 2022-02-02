using System;
using RedUtils.Math;

namespace RedUtils
{
	/// <summary>A kickoff action, which performs a speedflip kickoff</summary>
	public class Kickoff : IAction
	{
		enum KickOffType
		{
			FarBack,
			BackSide,
			Diagoal,
		}
		
		/// <summary>Kickoffs aren't interruptible, so this will always be false</summary>
		public bool Interruptible
		{ get; set; }
		/// <summary>Whether or not the kickoff pepriod has ended</summary>
		public bool Finished
		{ get; set; }

		/// <summary>Which type of kickoff spawn location it is</summary>
		private readonly KickOffType _type;

		/// <summary>Whether or not we have speedflipped</summary>
		private bool _speedFlipped = false;
		/// <summary>The speedflip sub action</summary>
		private SpeedFlip _speedFlip = null;
		
		/// <summary>A random number close to 0.5</summary>
		private float _rand1;
		/// <summary>A random number close to 0.5</summary>
		private float _rand2;
		/// <summary>Whether or not we fake the kickoff. Only applies on far kickoffs</summary>
		private readonly bool _fake;

		private float _fakeBeginTime = -1f;
		private Drive _fakeDrive;

		/// <summary>Initializes a new kickoff action</summary>
		public Kickoff(Car car)
		{
			Interruptible = false;
			Finished = false;
			_type = MathF.Abs(car.Location.y) switch
			{
				<= 3500f => KickOffType.Diagoal,
				<= 4000f => KickOffType.BackSide,
				_ => KickOffType.FarBack,
			};
			
			Random rng = new Random();
			_rand1 = rng.NextMiddleFloatOf3();
			_rand2 = rng.NextMiddleFloatOf3();
			_fake = _type == KickOffType.FarBack && rng.NextFloat() < 0.4f;
		}

		/// <summary>Performs this kickoff action</summary>
		public void Run(RUBot bot)
		{
			Finished = Ball.Location.x != 0 || Ball.Location.y != 0 || !Game.IsKickoffPause;
			
			if (_type == KickOffType.FarBack && _fake)
			{
				if (_fakeBeginTime < 0)
				{
					// Begin fake
					_fakeBeginTime = Game.Time;
				}
				else if (_fakeDrive != null)
				{
					// Drive forwards until someone hits the ball 
					_fakeDrive.Run(bot);
					Finished = Game.Time > _fakeBeginTime + 2.7f;
				}
				else if (Game.Time < _fakeBeginTime + 0.6f)
				{
					// Creep backwards
					bot.Controller.Throttle = -1f;
				}
				else if (Game.Time > _fakeBeginTime + 1.8f)
				{
					// Begin driving forwards
					_fakeDrive = new Drive(bot.Me, Vec3.Zero, 1400, false);
				}
			}
			else if (_speedFlip != null && !_speedFlip.Finished)
			{
				// If we are speed flipping, make sure to hold down boost
				bot.Controller.Boost = true;
				_speedFlip.Run(bot);
			}
			else
			{
				bot.Throttle(Car.MaxSpeed);
				
				// Aim at a point slightly offset from the ball, so we get an optimal 50/50 on the kickoff
				if (bot.Me.Location.Dist(Ball.Location) < 3300)
				{
					bot.AimAt(Ball.Location - Ball.Location.Direction(bot.TheirGoal.Location) * (166 + _rand1 * 30));
				}
				else
				{
					bot.AimAt(Ball.Location - Ball.Location.Direction(bot.TheirGoal.Location) * 2800);
				}

				if (!bot.IsKickoff)
				{
					// If the kickoff period has ended, finish this action
					Finished = true;
				}
				else if (bot.Me.Velocity.Length() > 620 && (_type == KickOffType.FarBack || bot.Me.Location.Dist(Ball.Location) < 3480) && !_speedFlipped)
				{
					// When we are moving fast enough, start speed flipping
					_speedFlipped = true;
					_speedFlip = new SpeedFlip(bot.Me.Location.Direction(Ball.Location - Ball.Location.Direction(bot.TheirGoal.Location) * (160 + _rand2 * 30)));
				}
				else if (bot.Me.Location.Dist(Ball.Location) < 800 && bot.Me.IsGrounded)
				{
					// When we are close enough to the ball, dodge into it
					bot.Action = new Dodge(Ball.Location.Direction(bot.TheirGoal.Location), 0.18f);
				}
			}
		}
	}
}
