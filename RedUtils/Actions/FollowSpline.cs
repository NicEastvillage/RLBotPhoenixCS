using System;
using System.Collections.Generic;
using RedUtils.Math;
using Color = System.Drawing.Color;

namespace RedUtils.Actions;

public class FollowSpline : IAction
{
    public const bool DoDebugRender = true;
    
    public bool Finished { get; private set; }
    public bool Interruptible => Drive?.Interruptible ?? true;
    public bool Navigational => true;

    public SplinePath SplinePath { get; private set; }

    public Drive Drive { get; private set; }
    
    public FollowSpline(List<Vec3> path)
    {
        if (path.Count < 2) throw new ArgumentException("Path must have at least two points and first point must be our current position");
        SplinePath = new SplinePath(path);
    }

    public void Run(RUBot bot)
    {
        float u = SplinePath.InverseEval(bot.Me.Location);
        Vec3 closestPoint = SplinePath.Eval(u);
        float w = System.Math.Min(u + 400f / SplinePath.Length(), 1f);
        Vec3 target = SplinePath.Eval(w);

        float minimumSpeed = 1700f;
        if (bot.Me.Location.y * Field.Side(bot.Team) < 0) minimumSpeed += 200f;
        if (Ball.Location.y * Field.Side(bot.Team) > 0) minimumSpeed += 200f;
        
        Drive ??= new Drive(bot.Me, target, allowDodges: false);
        Drive.Target = target;
        Drive.Backwards = false;
        Drive.WasteBoost = bot.Me.Forward.Dot(bot.Me.Velocity) < minimumSpeed || bot.Me.Boost > 80;
        Drive.Run(bot);

        Finished = bot.Me.Location.Dist(closestPoint) > 180f || SplinePath.ControlPoints[^1].Dist(bot.Me.Location) < 100f;

        if (DoDebugRender)
        {
            bot.Renderer.Polyline3D(SplinePath.ControlPoints, Color.Gray);
            bot.Renderer.Circle(closestPoint, Vec3.Up, 70, Color.MediumPurple);
            bot.Renderer.Color = Color.MediumPurple;
            SplinePath.Draw(bot.Renderer);
        }
    }
}
