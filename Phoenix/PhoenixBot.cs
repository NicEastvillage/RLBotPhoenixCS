using System;
using System.Collections.Generic;
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
        private KickOffPicker _kickOffPicker = new KickOffPicker();
        private DribbleDetector _dribbleDetector = new DribbleDetector();
        
        // We want the constructor for our Bot to extend from RUBot, but feel free to add some other initialization in here as well.
        public PhoenixBot(string botName, int botTeam, int botIndex) : base(botName, botTeam, botIndex)
        {
        }

        // Runs every tick. Should be used to find an Action to execute
        public override void Run()
        {
            //GameAnalysis.Update(this);
            //BoostNetwork.FindPath(Me, OurGoal.Location, Renderer);
            WallReflectTargets();
            _kickOffPicker.Evaluate(this);
            //_kickOffPicker.DrawSummary(Renderer);

            // Prints out the current action to the screen, so we know what our bot is doing
            String actionStr = Action != null ? Action.ToString() : "null";
            Renderer.Text2D($"{Name}: {actionStr}", new Vec3(30, 400 + 18 * Index), 1, Color.White);

            Car dribbler = _dribbleDetector.GetDribbler(DeltaTime);
            
            if (IsKickoff && Action == null)
            {
                Action = _kickOffPicker.PickKickOffAction(this);
            }
            else if (dribbler != null && dribbler.Team != Team && _dribbleDetector.Duration() > 0.4f && (Action == null || Action is Drive || Action.Interruptible))
            {
                // An enemy is dribbling. Tackle them!
                if (Action is not Drive)
                {
                    Action = new Drive(Me, dribbler.Location, wasteBoost: true);
                }
                
                // Predict location
                float naiveEta = Drive.GetEta(Me, dribbler.Location, true);
                Vec3 naiveLoc = dribbler.Location + naiveEta * dribbler.Velocity;
                float okayEta = Drive.GetEta(Me, naiveLoc, true);
                Vec3 okayLoc = dribbler.Location + okayEta * dribbler.Velocity;
                float eta = Drive.GetEta(Me, okayLoc, true);
                Vec3 loc = dribbler.Location + eta * dribbler.Velocity;;

                ((Drive)Action).Target = loc;
                Renderer.Rect3D(loc, 14, 14, color: Color.DarkOrange);
                Renderer.Rect3D(dribbler.Location, 20, 20, color: Color.Red);
            }
            else if (Action == null || ((Action is Drive || Action is BoostCollectingDrive) && Action.Interruptible))
            {
                Shot shot;
                // search for the first available shot using NoAerialsShotCheck
                CheapNoAerialShotCheck.Next(Me);
                List<Target> goalTargets = Field.Side(Me.Team) == MathF.Sign(Ball.Location.y)
                    ? new List<Target> { new(OurGoal, true), new(TheirGoal) }
                    : new List<Target> { new(TheirGoal) };
                Shot directShot = FindShot(CheapNoAerialShotCheck.ShotCheck, goalTargets);
                Shot reflectShot = FindShot(CheapNoAerialShotCheck.ShotCheck, WallReflectTargets());

                if (directShot != null && reflectShot != null && reflectShot.Slice.Time + 0.04 < directShot.Slice.Time)
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
                            // Our corner. Only go if we are approaching for the middle or if all enemies are far away
                            if (MathF.Abs(shot.Slice.Location.x) - MathF.Abs(Me.Location.x) <= 0 &&
                                Cars.AllLivingCars.Any(car => car.Team != Me.Team && car.Location.Dist(OurGoal.Location) < 2500))
                                shot = null;
                        }
                    }
                }
                
                IAction alternative = Action is BoostCollectingDrive ? Action : null;
                Vec3 shadowLocation = Utils.Lerp(0.35f, Ball.Location, OurGoal.Location);
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
                        alternative = new Drive(Me, 0.83f * OurGoal.Location + new Vec3(0.6f * Me.Location.x, 0), wasteBoost: true);
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

        private List<Target> WallReflectTargets()
        {
            float naiveTime = Drive.GetEta(Me, Ball.Location);
            BallSlice naiveSlice = Ball.Prediction.AtTime(Game.Time + naiveTime) ?? BallSlice.Now();
            float betterNaiveTime = Drive.GetEta(Me, naiveSlice.Location);
            BallSlice slice = Ball.Prediction.AtTime(Game.Time + betterNaiveTime) ?? BallSlice.Now();
            float ballToGoalDist = slice.Location.Dist(TheirGoal.Location);

            const float MIN_Z = 190;
            const float MAX_Z = 1800;
            float targetSemiWidth = MathF.Max(Utils.Lerp(ballToGoalDist / 5500 - 1.5f * MathF.Abs(slice.Location.x) / Field.Width, 0, 333), 50);
            
            List<Target> targets = new List<Target>();

            List<(Vec3, Vec3, Vec3)> reflectWalls = new List<(Vec3, Vec3, Vec3)>
            {
                (Field.Side(Team) * Vec3.X,
                    new Vec3(-Field.Side(Team) * Field.Width / 2, Field.Side(Team) * 4000),
                    new Vec3(-Field.Side(Team) * Field.Width / 2, 3700)), // Left wall
                (-Field.Side(Team) * Vec3.X,
                    new Vec3(Field.Side(Team) * Field.Width / 2, 3700),
                    new Vec3(Field.Side(Team) * Field.Width / 2, Field.Side(Team) * 4000)), // Right wall
                (new Vec3(Field.Side(Team), Field.Side(Team)).Normalize(),
                    new Vec3(-Field.Side(Team) * 3900, -Field.Side(Team) * 4164),
                    new Vec3(-Field.Side(Team) * 3200, -Field.Side(Team) * 4864)), // Enemy left corner wall
                (new Vec3(-Field.Side(Team), Field.Side(Team)).Normalize(),
                    new Vec3(Field.Side(Team) * 3200, -Field.Side(Team) * 4864),
                    new Vec3(Field.Side(Team) * 3900, -Field.Side(Team) * 4164)), // Enemy right corner wall
                (new Vec3(Field.Side(Team), -Field.Side(Team)).Normalize(),
                    new Vec3(-Field.Side(Team) * 3200, Field.Side(Team) * 4864),
                    new Vec3(-Field.Side(Team) * 3900, Field.Side(Team) * 4164)), // Our left corner wall, artificially extended for better clears
                (new Vec3(-Field.Side(Team), -Field.Side(Team)).Normalize(),
                    new Vec3(Field.Side(Team) * 3900, Field.Side(Team) * 4164),
                    new Vec3(Field.Side(Team) * 3200, Field.Side(Team) * 4864)), // Our right corner wall, artificially extended for better clears
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
                    reflectPoint - a2B * targetSemiWidth + Vec3.Z * MAX_Z,
                    reflectPoint + a2B * targetSemiWidth + Vec3.Z * MIN_Z
                );

                if (reflectT < 0 || a.Dist(b) < reflectT || slice.Location.FlatDist(reflectPoint) > 3000) continue;

                Renderer.Polyline3D(new List<Vec3>
                {
                    a + Vec3.Z * MAX_Z,
                    b + Vec3.Z * MAX_Z,
                    b + Vec3.Z * MIN_Z,
                    a + Vec3.Z * MIN_Z,
                    a + Vec3.Z * MAX_Z,
                }, Color.Yellow);
                Renderer.Line3D(Vec3.Z * 800 + (a + b) / 2, Vec3.Z * 800 + (a + b) / 2 + normal * 100, Color.Yellow);
                Renderer.Polyline3D(new List<Vec3>
                {
                    reflectPoint - a2B * targetSemiWidth + Vec3.Z * MAX_Z,
                    reflectPoint + a2B * targetSemiWidth + Vec3.Z * MAX_Z,
                    reflectPoint + a2B * targetSemiWidth + Vec3.Z * MIN_Z,
                    reflectPoint - a2B * targetSemiWidth + Vec3.Z * MIN_Z,
                    reflectPoint - a2B * targetSemiWidth + Vec3.Z * MAX_Z,
                }, Color.Azure);
                Renderer.Line3D(slice.Location, reflectPoint + Vec3.Z * 800, Color.Fuchsia);
                Renderer.Line3D(Field.Goals[1 - Me.Team].Location, reflectPoint + Vec3.Z * 800, Color.Fuchsia);

                targets.Add(target);
            }

            return targets;
        }
    }
}
