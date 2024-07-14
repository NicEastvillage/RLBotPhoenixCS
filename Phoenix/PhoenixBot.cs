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
        private RoleFinder _roleFinder = new RoleFinder();

        public List<ITargetFactory> WallReflectTargetFactories { get; }
        
        public PhoenixBot(string botName, int botTeam, int botIndex) : base(botName, botTeam, botIndex)
        {
            WallReflectTargetFactories = new List<ITargetFactory>
            {
                new WallReflectTargetFactory(Field.Side(Team) * Vec3.X,
                    new Vec3(-Field.Side(Team) * Field.Width / 2, Field.Side(Team) * 4000),
                    new Vec3(-Field.Side(Team) * Field.Width / 2, 3700)), // Left wall
                new WallReflectTargetFactory(-Field.Side(Team) * Vec3.X,
                    new Vec3(Field.Side(Team) * Field.Width / 2, 3700),
                    new Vec3(Field.Side(Team) * Field.Width / 2, Field.Side(Team) * 4000)), // Right wall
                new WallReflectTargetFactory(new Vec3(Field.Side(Team), Field.Side(Team)).Normalize(),
                    new Vec3(-Field.Side(Team) * 3900, -Field.Side(Team) * 4164),
                    new Vec3(-Field.Side(Team) * 3200, -Field.Side(Team) * 4864)), // Enemy left corner wall
                new WallReflectTargetFactory(new Vec3(-Field.Side(Team), Field.Side(Team)).Normalize(),
                    new Vec3(Field.Side(Team) * 3200, -Field.Side(Team) * 4864),
                    new Vec3(Field.Side(Team) * 3900, -Field.Side(Team) * 4164)), // Enemy right corner wall
                new WallReflectTargetFactory(new Vec3(Field.Side(Team), -Field.Side(Team)).Normalize(),
                    new Vec3(-Field.Side(Team) * 3200, Field.Side(Team) * 4864),
                    new Vec3(-Field.Side(Team) * 3900, Field.Side(Team) * 4164)), // Our left corner wall, artificially extended for better clears
                new WallReflectTargetFactory(new Vec3(-Field.Side(Team), -Field.Side(Team)).Normalize(),
                    new Vec3(Field.Side(Team) * 3900, Field.Side(Team) * 4164),
                    new Vec3(Field.Side(Team) * 3200, Field.Side(Team) * 4864)), // Our right corner wall, artificially extended for better clears
            };
        }

        // Runs every tick. Should be used to find an Action to execute
        public override void Run()
        {
            //GameAnalysis.Update(this);
            //BoostNetwork.FindPath(Me, OurGoal.Location, Renderer);
            _kickOffPicker.Evaluate(this);
            _kickOffPicker.DrawSummary(Renderer);

            Role role = _roleFinder.Update(this);
            
            // Prints out the current action to the screen, so we know what our bot is doing
            String actionStr = Action != null ? Action.ToString()!.Substring(9) : "null";
            Renderer.Text2D($"{Name,14}: {role}/{actionStr}", new Vec3(30, 400 + 18 * Index), 1, Color.White);
            Renderer.Text3D($"{role}/{actionStr}", Me.Location + Vec3.Up * 30, 1, Color.White);

            if (IsKickoff && Action == null)
            {
                Action = _kickOffPicker.PickKickOffAction(this);
                return;
            }

            // TODO Handle other roles
            switch (role)
            {
                case Role.Attack:
                    RunAttackLogic();
                    break;
                case Role.Assist:
                    RunAssistLogic();
                    break;
                case Role.Defend:
                    RunDefendLogic();
                    break;
                default:
                    RunDefaultLogic();
                    break;
            }
        }

        private void RunDefaultLogic()
        {
            var considerNewActions = Action == null || ((Action is Drive || Action is BoostCollectingDrive) && Action?.Interruptible != false);
            if (!considerNewActions) return;
            
            Shot shot = null;
            // search for the first available shot using NoAerialsShotCheck
            RotatingShotChecker.Next(Me);
            List<ITargetFactory> goalTargetFactories = Field.Side(Me.Team) == MathF.Sign(Ball.Location.y)
                ? new List<ITargetFactory> { new ClearGoalTargetFactory(OurGoal), new StaticTargetFactory(new(TheirGoal)) }
                : new List<ITargetFactory> { new StaticTargetFactory(new Target(TheirGoal)) };
            Shot directShot = FindShot(RotatingShotChecker.ShotCheck, goalTargetFactories);
            Shot forwardShot = FindShot(RotatingShotChecker.ShotCheck, new ForwardTargetFactory());

            shot = directShot ?? forwardShot;

            // Shot is too far away to be concerned about?
            if (shot != null && shot.Slice.Location.Dist(Me.Location) >= 5000)
            {
                shot = null;
            }

            NearestCarsByEtaData carEtas = Cars.NearestCarsByEta();
            // Renderer.Rect3D(carEtas.nearestCar.Location, 30, 30, color: Color.Fuchsia);
            if (carEtas.nearestCar == null)
            {
                // All cars are demolished
            }
            else if (carEtas.nearestCar == Me)
            {
                // Nearest car is me
            }
            else
            {
                // Abandon shot if someone else will get there much sooner,
                // unless that someone is an enemy and we have an ally in defence
                // if A unless B === if A and !B
                bool anyAllyDefending = Cars.AlliesAndMe.FindAll(car => car.Location.Dist(OurGoal.Location) < 1000).Count > 0;
                bool nearestCarIsEnemy = carEtas.nearestCar.Team != Me.Team;
                if (shot != null && !(anyAllyDefending && nearestCarIsEnemy) && Game.Time + carEtas.nearestCarEta <= shot.Slice.Time - 0.5f)
                {
                    // They will hit it first
                    shot = null;
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
                    Renderer.Rect3D(Me.Location, 5, 5, color: Color.Bisque);
                }
                else
                {
                    // Approach
                    alternative = new BoostCollectingDrive(Me, shadowLocation);
                }
            }
            
            // if a shot is found, go for the shot. Otherwise, if there is an Action to execute, execute it. If none of the others apply, drive back to goal.
            Action = shot ?? alternative ?? Action ?? new BoostCollectingDrive(Me, shadowLocation);
        }

        private void RunAttackLogic()
        {
            var considerNewActions = Action == null || ((Action is Drive || Action is BoostCollectingDrive) && Action?.Interruptible != false);
            if (!considerNewActions) return;
            
            Car dribbler = _dribbleDetector.GetDribbler(DeltaTime);
            if (dribbler != null && dribbler.Team != Team && _dribbleDetector.Duration() > 0.4f)
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
                return;
            }
            
            Shot shot = null;
            RotatingShotChecker.Next(Me);
            List<ITargetFactory> goalTargetFactories = Field.Side(Me.Team) == MathF.Sign(Ball.Location.y)
                ? new List<ITargetFactory> { new ClearGoalTargetFactory(OurGoal), new StaticTargetFactory(new(TheirGoal)) }
                : new List<ITargetFactory> { new StaticTargetFactory(new Target(TheirGoal)) };
            Shot directShot = FindShot(RotatingShotChecker.ShotCheck, goalTargetFactories);
            Shot forwardShot = FindShot(RotatingShotChecker.ShotCheck, new ForwardTargetFactory());

            shot = directShot ?? forwardShot;

            // Shot is too far away to be concerned about?
            if (shot != null && shot.Slice.Location.Dist(Me.Location) >= 5000)
            {
                shot = null;
            }

            NearestCarsByEtaData carEtas = Cars.NearestCarsByEta();
            // Abandon shot if an enemy will get there sooner.
            // If difference is small and we have an ally in defence, then go for it anyway.
            bool anyAllyDefending = Cars.AlliesAndMe.FindAll(car => car.Location.Dist(OurGoal.Location) < 1000).Count > 0;
            float etaThreshold = anyAllyDefending ? 0.12f : 0.06f;
            if (shot != null && !anyAllyDefending && Game.Time + carEtas.nearestCarEta <= shot.Slice.Time - etaThreshold)
            {
                // They will hit it first
                shot = null;
            }

            if (shot != null)
            {
                Action = shot;
                return;
            }

            float roughEta = Me.Location.Dist(Ball.Location) / 2300f;
            Vec3 roughBallLoc = Ball.Prediction.AtTime(Game.Time + roughEta)?.Location ?? Ball.Location;
            
            // Avoid chasing towards own goal // TODO Improve detection and backup plan 
            if ((OurGoal.Location - Me.Location).Angle(OurGoal.Location - Ball.Location) < 0.1f)
            {
                RunDefendLogic();
            }

            float heightIssues01 = 0.55f * (roughBallLoc.z - Ball.Radius) / Me.Location.Dist(roughBallLoc.Flatten());
            float speed = Utils.Cap(Utils.Lerp(heightIssues01, 2100f, 50f), 0, 2100f);

            if (Action is Drive drive)
            {
                drive.Target = roughBallLoc;
                drive.TargetSpeed = speed;
                drive.AllowDodges = true;
                drive.WasteBoost = Me.Forward.Angle(Me.Location.Direction(Ball.Location)) < 0.9f;
            }
            else Action = new Drive(Me, roughBallLoc);
        }

        private void RunAssistLogic()
        {
            RunDefaultLogic();
        }

        private void RunDefendLogic()
        {
            var considerNewActions = Action == null || ((Action is Drive || Action is BoostCollectingDrive) && Action?.Interruptible != false);
            if (!considerNewActions) return;

            Vec3 halfHomeLoc = (Me.Location + OurGoal.Location * 0.91f) / 2;
            Vec3 ballSideLoc = Ball.Location.WithX(MathF.Sign(Ball.Location.x) * (Field.Width / 2 - 250)).Flatten();
            float homeSickness01 = Me.Location.Dist(OurGoal.Location * 0.9f) / Field.Length;
            Vec3 fallBackLoc = halfHomeLoc + ballSideLoc.Direction(halfHomeLoc) * Field.Width * homeSickness01 / 2;

            // TODO Need faster-driving version of BoostCollectingDrive
            if (Action is BoostCollectingDrive drive)
            {
                drive.FinalDestination = fallBackLoc;
            }
            else Action = new BoostCollectingDrive(Me, fallBackLoc);
        }
    }
}
