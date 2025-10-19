using RedUtils.Math;
using RLBot.Flat;

namespace RedUtils
{
	/// <summary>Contains basic info on the current match</summary>
	public static class Game
	{
		/// <summary>The scores for the blue team, and orange team (in that order)</summary>
		public static int[] Scores { get; private set; }
		/// <summary>The blue team's score</summary>
		public static int BlueScore => Scores[1];
		/// <summary>The orange team's score</summary>
		public static int OrangeScore => Scores[0];

		/// <summary>How much time has passed since the game has began</summary>
		public static float Time { get; private set; }
		/// <summary>How much time is remaining before the game ends</summary>
		public static float TimeRemaining { get; private set; }
		/// <summary>The speed at which game plays</summary>
		public static float GameSpeed { get; private set; }

		/// <summary>Whether or not the game is configured with unlimited time</summary>
		public static bool IsUnlimitedTime { get; private set; }
		/// <summary>Whether or not the game is currently in overtime</summary>
		public static bool IsOvertime { get; private set; }
		/// <summary>Whether or not a round is currently active (meaning we aren't in a replay)</summary>
		public static bool IsRoundActive { get; private set; }
		/// <summary>Whether or not the game/timer has paused for kickoff</summary>
		public static bool IsKickoffPause { get; private set; }
		/// <summary>Whether or not the game has concluded</summary>
		public static bool IsMatchEnded { get; private set; }

		/// <summary>The acceleration caused by gravity</summary>
		public static Vec3 Gravity { get; private set; }

		static Game()
		{
			Scores = new int[2] { 0, 0 };;

			Time = 0;
			TimeRemaining = 300;
			GameSpeed = 1;

			IsUnlimitedTime = false;
			IsOvertime = false;
			IsRoundActive = false;
			IsKickoffPause = false;
			IsMatchEnded = false;

			Gravity = new Vec3(0, 0, -650);
		}

		/// <summary>Updates info about the game using data from the packet</summary>
		public static void Update(GamePacketT packet)
		{
			Scores = new int[2] { (int)packet.Teams[0].Score, (int)packet.Teams[1].Score };

			Time = packet.MatchInfo.SecondsElapsed;
			TimeRemaining = packet.MatchInfo.GameTimeRemaining;
			GameSpeed = packet.MatchInfo.GameSpeed;

			IsUnlimitedTime = packet.MatchInfo.IsUnlimitedTime;
			IsOvertime = packet.MatchInfo.IsOvertime;
			IsRoundActive = packet.MatchInfo.MatchPhase is MatchPhase.Active or MatchPhase.Kickoff;
			IsKickoffPause = packet.MatchInfo.MatchPhase == MatchPhase.Kickoff; 
			IsMatchEnded = packet.MatchInfo.MatchPhase == MatchPhase.Ended;

			Gravity = new Vec3(0, 0, packet.MatchInfo.WorldGravityZ);
		}
	}
}
