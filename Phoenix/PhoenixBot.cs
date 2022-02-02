using System;
using System.Collections.Generic;
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
        public PhoenixBot(string botName, int botTeam, int botIndex) : base(botName, botTeam, botIndex)
        {
        }

        // Runs every tick. Should be used to find an Action to execute
        public override void Run()
        {
            //GameAnalysis.Update(this);

            // Prints out the current action to the screen, so we know what our bot is doing
            String actionStr = Action != null ? Action.ToString() : "null";
            Renderer.Text2D($"{Name}: {actionStr}", new Vec3(30, 400 + 18 * Index), 1, Color.White);

            if (IsKickoff && Action == null)
            {
                PickKickoffAction();
            }
            else if (Action == null || ((Action is Drive || Action is BoostCollectingDrive) && Action.Interruptible))
            {
                Shot shot;
                // search for the first avaliable shot using NoAerialsShotCheck
                CheapNoAerialShotCheck.Next(Me);
                Shot directShot = FindShot(CheapNoAerialShotCheck.ShotCheck, new Target(TheirGoal));
                Shot reflectShot = FindShot(CheapNoAerialShotCheck.ShotCheck, WallReflectTargets());

                if (directShot != null && reflectShot != null && reflectShot.Slice.Time + 0.08 < directShot.Slice.Time)
                {
                    // Early reflect shot is possible
                    shot = reflectShot;
                }
                else
                {
                    shot = directShot ?? reflectShot;
                }

                // Shot is too far away to be concerned about?
                if (shot != null && shot.Slice.Location.Dist(Me.Location) >= 5000)
                {
                    shot = null;
                }
                
                if (shot != null)
                {
                    // If the shot happens in a corner, special rules apply
                    if (MathF.Abs(shot.Slice.Location.x) + MathF.Abs(shot.Slice.Location.y) >= 5700)
                    {
                        if (MathF.Sign(shot.Slice.Location.y) != Field.Side(Team))
                        {
                            // Enemy corner. Never go for these
                            shot = null;
                        }
                        else
                        {
                            // Our corner. Only go if we are approoching for the middle
                            if (MathF.Abs(shot.Slice.Location.x) - MathF.Abs(Me.Location.x) <= 0) shot = null;
                        }
                    }
                }
                
                IAction alternative = Action is BoostCollectingDrive ? Action : null;
                Vec3 shadowLocation = (Ball.Location + OurGoal.Location) / 2f;
                bool onOurSideOfShadowLocation = (shadowLocation.y - Me.Location.y) * Field.Side(Me.Team) >= 0;
                
                if (shot == null && alternative == null)
                {
                    if (Ball.Location.y * -Field.Side(Team) >= 3000)
                    {
                        // Ball is far from our goal

                        // Collect boost
                        if (Me.Boost <= 20)
                        {
                            List<Boost> boosts = Field.Boosts.FindAll(boost =>
                                boost.IsLarge && (boost.Location.y - Me.Location.y) * Field.Side(Me.Team) >= 0);
                            alternative = new GetBoost(Me, boosts);
                        }
                    }
                    else if (Me.Boost <= 50 && !onOurSideOfShadowLocation)
                    {
                        // Get back but also collect boost
                        alternative = new BoostCollectingDrive(Me,
                            0.83f * OurGoal.Location + new Vec3(0.6f * Me.Location.x, 0));
                    }
                    else if (Me.Boost >= 50 && !onOurSideOfShadowLocation)
                    {
                        // Get back!
                        alternative = new Drive(Me, 0.83f * OurGoal.Location + new Vec3(0.6f * Me.Location.x, 0));
                    }
                    else if (Me.Boost <= 50 && onOurSideOfShadowLocation)
                    {
                        // Collect boost on defence
                        alternative = new BoostCollectingDrive(Me,
                            0.83f * OurGoal.Location - new Vec3(0.8f * Me.Location.x, 0));
                    }
                    else
                    {
                        // Approach
                        alternative = new BoostCollectingDrive(Me, shadowLocation);
                    }
                }

                // if a shot is found, go for the shot. Otherwise, if there is an Action to execute, execute it. If none of the others apply, drive back to goal.
                Action = shot ?? alternative ??
                    Action ?? new BoostCollectingDrive(Me, shadowLocation);
            }
        }

        private void PickKickoffAction()
        {
            // Use left-goes protocol
            Car kicker = Cars.AllCars
                .FindAll(car => car.Team == Me.Team)
                .OrderBy(car => car.Location.Length() + MathF.Sign(car.Location.x * Field.Side(car.Team)))
                .First();

            Action = kicker == Me
                ? new Kickoff(Me)
                : new GetBoost(Me, interruptible: false); // if we aren't going for the kickoff, get boost
        }

        private List<Target> WallReflectTargets()
        {
            BallSlice slice = Ball.Prediction.AtTime(Game.Time + 0.5f);
            List<Target> targets = new List<Target>();
            
            if (slice == null) return targets;

            List<(Vec3, Vec3, Vec3)> reflectWalls = new List<(Vec3, Vec3, Vec3)>
            {
                (Field.Side(Team) * Vec3.X, new Vec3(-Field.Side(Team) * Field.Width / 2, Field.Side(Team) * 3000),
                    new Vec3(-Field.Side(Team) * Field.Width / 2, 3700)), // Left wall
                (-Field.Side(Team) * Vec3.X, new Vec3(Field.Side(Team) * Field.Width / 2, 3700),
                    new Vec3(Field.Side(Team) * Field.Width / 2, Field.Side(Team) * 3000)), // Right wall
                (new Vec3(Field.Side(Team), Field.Side(Team)).Normalize(),
                    new Vec3(-Field.Side(Team) * 3900, -Field.Side(Team) * 4164),
                    new Vec3(-Field.Side(Team) * 3200, -Field.Side(Team) * 4864)), // Left corner wall
                (new Vec3(-Field.Side(Team), Field.Side(Team)).Normalize(),
                    new Vec3(Field.Side(Team) * 3200, -Field.Side(Team) * 4864),
                    new Vec3(Field.Side(Team) * 3900, -Field.Side(Team) * 4164)), // Right corner wall
            };

            foreach (var (normal, a, b) in reflectWalls)
            {
                Vec3 a2B = a.Direction(b);

                Vec3 ballOnA2B = a + a2B * (slice.Location - a).Dot(a2B);
                float ballDistA2B = (slice.Location - a).Dot(normal);

                Vec3 goalOnA2B = a + a2B * (Field.Goals[1 - Me.Team].Location - a).Dot(a2B);
                float goalDistA2B = (Field.Goals[1 - Me.Team].Location - a).Dot(normal);

                Vec3 reflectPoint = Utils.Lerp(ballDistA2B / (ballDistA2B + goalDistA2B), ballOnA2B, goalOnA2B);
                float reflectT = (slice.Location - a).Dot(a2B);

                Target target = new Target(
                    reflectPoint - a2B * 200 + Vec3.Z * 1800,
                    reflectPoint + a2B * 200 + Vec3.Z * 190
                );

                if (reflectT < 0 || a.Dist(b) < reflectT || slice.Location.FlatDist(reflectPoint) > 2500) continue;

                Renderer.Polyline3D(new List<Vec3>
                {
                    a + Vec3.Z * 1800,
                    b + Vec3.Z * 1800,
                    b + Vec3.Z * 190,
                    a + Vec3.Z * 190,
                    a + Vec3.Z * 1800,
                }, Color.Yellow);
                Renderer.Line3D(Vec3.Z * 800 + (a + b) / 2, Vec3.Z * 800 + (a + b) / 2 + normal * 100, Color.Yellow);
                Renderer.Polyline3D(new List<Vec3>
                {
                    reflectPoint - a2B * 200 + Vec3.Z * 1800,
                    reflectPoint + a2B * 200 + Vec3.Z * 1800,
                    reflectPoint + a2B * 200 + Vec3.Z * 190,
                    reflectPoint - a2B * 200 + Vec3.Z * 190,
                    reflectPoint - a2B * 200 + Vec3.Z * 1800,
                }, Color.Azure);
                Renderer.Line3D(slice.Location, reflectPoint + Vec3.Z * 800, Color.Fuchsia);
                Renderer.Line3D(Field.Goals[1 - Me.Team].Location, reflectPoint + Vec3.Z * 800, Color.Fuchsia);

                targets.Add(target);
            }

            return targets;
        }
    }
}
