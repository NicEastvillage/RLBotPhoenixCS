using RedUtils.Math;
using RLBot.Flat;

namespace RedUtils
{
	/// <summary>Contains info on a collision between the ball and a car</summary>
	public class BallTouch
	{
		/// <summary>The time at which point this collision happened</summary>
		public readonly float Time;
		/// <summary>The location of this collision</summary>
		public readonly Vec3 Location;
		/// <summary>The normal of this collision</summary>
		public readonly Vec3 Normal;
		/// <summary>The index of the player who collided with the ball</summary>
		public readonly int PlayerIndex;
		/// <summary>The team of the player who collided with the ball</summary>
		public readonly int Team;

		/// <summary>Initializes a new ball touch with data from the packet</summary>
		public BallTouch(TouchT touch, int playerIndex, int team)
		{
			Time = touch.GameSeconds;
			Location = new Vec3(touch.Location);
			Normal = new Vec3(touch.Normal);
			PlayerIndex = playerIndex;
			Team = team;
		}
	}
}
