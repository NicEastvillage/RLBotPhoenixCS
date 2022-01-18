using System;
using System.Threading;
using System.Drawing;
using System.Linq;
using RedUtils;
using RedUtils.Math;
/* 
 * This is the main file. It contains your bot class. Feel free to change the name!
 * An instance of this class will be created for each instance of your bot in the game.
 * Your bot derives from the "RedUtilsBot" class, contained in the Bot file inside the RedUtils project.
 * The run function listed below runs every tick, and should contain the custom strategy code (made by you!)
 * Right now though, it has a default ball chase strategy. Feel free to read up and use anything you like for your own strategy.
*/
namespace Phoenix
{
    // Your bot class! :D
    public class PhoenixBot : RUBot
    {
        // We want the constructor for our Bot to extend from RUBot, but feel free to add some other initialization in here as well.
        public PhoenixBot(string botName, int botTeam, int botIndex) : base(botName, botTeam, botIndex) { }

        // Runs every tick. Should be used to find an Action to execute
        public override void Run()
        {
            // Prints out the current action to the screen, so we know what our bot is doing
            String actionStr = Action != null ? Action.ToString() : "null"; 
            Renderer.Text2D($"{Name}: {actionStr}", new Vec3(30, 400 + 18 * Index), 1, Color.White);

            if (IsKickoff && Action == null)
            {
                PickKickoffAction();
            }
            else if (Action == null || (Action is Drive && Action.Interruptible))
            {
                // search for the first avaliable shot using DefaultShotCheck
                Shot shot = FindShot(DefaultShotCheck, new Target(TheirGoal));
                IAction alternative = null;

                if (shot != null)
                {
                    // If the shot happens in a corner, special rules apply
                    if (MathF.Abs(shot.Slice.Location.x) + MathF.Abs(shot.Slice.Location.y) >= 5700)
                    {
                        if (MathF.Sign(shot.Slice.Location.y) != 2 * Me.Team - 1)
                        {
                            // Enemy corner. Never go for these
                            shot = null;
                        }
                        else
                        {
                            // Our corner. Only go if we are approoching for the middle
                            if (shot.Slice.Location.x - Me.Location.x >= 0) shot = null;
                        }
                    }
                }
                
                if (shot == null)
                {
                    if (Ball.Location.y * (-2 * Me.Team + 1) >= 3000)
                    {
                        // Ball is far from our goal
                        if (Me.Boost <= 30)
                        {
                            alternative = new GetBoost(Me);
                        }
                    }
                }

                // if a shot is found, go for the shot. Otherwise, if there is an Action to execute, execute it. If none of the others apply, drive back to goal.
                Action = shot ?? alternative ?? Action ?? new Drive(Me, OurGoal.Location);
            }
        }

        private void PickKickoffAction()
        {
            // Use left-goes protocol
            Car kicker = Cars.AllCars
                .FindAll(car => car.Team == Me.Team)
                .OrderBy(car => car.Location.Length() + MathF.Sign(car.Location.x * (2 * car.Team - 1)))
                .First();

            Action = kicker == Me ? new Kickoff() : new GetBoost(Me, false); // if we aren't going for the kickoff, get boost
        }
    }
}
