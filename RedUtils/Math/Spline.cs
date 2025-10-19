using System;
using System.Collections.Generic;
using System.Numerics;
using Color = System.Drawing.Color;

namespace RedUtils.Math;

public class Spline
{
    // Catmull-Rom characteristics matrix
    // https://youtu.be/jvPPXbo87ds?si=m0tx2oCRFnfsR0H_&t=2970
    private static Matrix4x4 _matCatmullRom = new Matrix4x4(
            0f, 2f, 0f, 0f,
            -1f, 0f, 1f, 0f,
            2f, -5f, 4f, -1f,
            -1f, 3f, -3f, 1f
        ) * (1f/2f);

    // B-spline characteristics matrix
    // https://youtu.be/jvPPXbo87ds?si=1SJB6dJy-_HgATQV&t=3219
    private static Matrix4x4 _matBSpline = new Matrix4x4(
        1f, 4f, 1f, 0f,
        -3f, 0f, 3f, 0f,
        3f, -6f, 3f, 0f,
        -1f, 3f, -3f, 1f
    ) * (1f/6f);

    private Matrix4x4 _mat = _matCatmullRom * 0.2f + _matBSpline * 0.8f;
    
    private Vec3[] _points;

    public Spline(Vec3 p1, Vec3 p2, Vec3 p3, Vec3 p4)
    {
        _points = [p1, p2, p3, p4];
    }

    public Spline(List<Vec3> points)
    {
        if (points.Count != 4) throw new ArgumentException("There must be exactly 4 points in a Spline");
        _points = [points[0], points[1], points[2], points[3]];
    }

    public void AdjustForBoost()
    {
        // A B-Spline does not go through the control points, but we if notch the controls points and evaluate
        // again, then we are close to
        _points[1] += 0.25f * (_points[1] - EvalWith(0f, _matBSpline));
        _points[2] += 0.25f * (_points[2] - EvalWith(1f, _matBSpline));
    }

    public float ApproxLength()
    {
        return Eval(0f).Dist(Eval(1f));
    }

    public Vec3 Eval(float t)
    {
        return EvalWith(t, _mat);
    }

    private Vec3 EvalWith(float t, Matrix4x4 mat)
    {
        Vector4 T = new Vector4(1, t, t * t, t * t * t);
        var res = Vector4.Transform(T, mat);
        return res.X * _points[0] + res.Y * _points[1] + res.Z * _points[2] + res.W * _points[3];
    }

    public void Draw(ExtendedRenderer draw)
    {
        draw.Color = Color.Fuchsia;
        foreach (Vec3 p in _points)
        {
            draw.Rect3D(p, 4, 4);
        }

        Vec3 prev = Eval(0f);
        float step = 1/20f;
        for (float t = step; t <= 1f; t += step)
        {
            Vec3 next = Eval(t);
            draw.Line3D(prev, next);
            prev = next;
        }
    }
}
