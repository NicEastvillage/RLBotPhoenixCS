using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace RedUtils.Math;

/// <summary>
/// An Open Cubic(?) Uniform B-Spline(?)
/// </summary>
public class SplinePath
{
    private const int DEG = 3; // Cubic
    
    // B-spline characteristics matrix
    // https://youtu.be/jvPPXbo87ds?si=1SJB6dJy-_HgATQV&t=3219
    private static readonly Matrix4x4 MatBSpline = new Matrix4x4(
        1f, 4f, 1f, 0f,
        -3f, 0f, 3f, 0f,
        3f, -6f, 3f, 0f,
        -1f, 3f, -3f, 1f
    ) * (1f/6f);
    
    // Catmull-Rom characteristics matrix
    // https://youtu.be/jvPPXbo87ds?si=m0tx2oCRFnfsR0H_&t=2970
    private static readonly Matrix4x4 MatCatmullRom = new Matrix4x4(
        0f, 2f, 0f, 0f,
        -1f, 0f, 1f, 0f,
        2f, -5f, 4f, -1f,
        -1f, 3f, -3f, 1f
    ) * (1f/2f);

    private static readonly Matrix4x4 MyMat = MatBSpline * 0.5f + MatCatmullRom * 0.5f;
    
    public IReadOnlyList<Vec3> ControlPoints { get; }
    public int SegmentCount => ControlPoints.Count - 1;
    
    private readonly List<Vec3> _knots;
    private float _length = 0;  // Cached result of Length() if _lengthSegments > 0
    private int _lengthSegments = -1; 

    public SplinePath(IReadOnlyList<Vec3> path)
    {
        ControlPoints = path.ToList();
        _knots = new List<Vec3>(path.Count + 2 * (DEG - 1));
        _knots.Add(path[0]);
        foreach (Vec3 p in path)
        {
            _knots.Add(p);
        }
        _knots.Add(path[^1]);
    }

    /// <summary>Returns the length of this path; The length is approximated using a number of linear segments and then cached.</summary>
    public float Length(int samples = 32)
    {
        if (samples <= 0)
            throw new ArgumentException("Must approximate using at least one segment");
        if (samples <= _lengthSegments)
            return _length;
        _lengthSegments = samples;
        
        float step = 1f / samples;
        float sum = 0;
        Vec3 prev = Eval(0f);
        float t = step;
        for (int i = 0; i < samples; i++)
        {
            Vec3 next = Eval(t);
            sum += prev.Dist(next);
            prev = next;
            t += step;
        }

        _length = sum;
        return sum;
    }

    public float LinearLength()
    {
        float sum = 0;
        for (int i = 1; i < ControlPoints.Count; i++)
        {
            sum += ControlPoints[i - 1].Dist(ControlPoints[i]);
        }
        return sum;
    }

    public Vec3 Eval(float t)
    {
        if (t <= 0f) return _knots[0];
        if (t >= 1f) return _knots[^1];
        int seg = (int)(t * SegmentCount);
        float u = t * SegmentCount - seg;
        return EvalInSegment(seg, u);
    }

    public float EvalByDist(float dist)
    {
        float len = Length();
        return Utils.Cap(dist / len, 0, 1f);
    }

    public float InverseEval(Vec3 point)
    {
        // Assumes point is very close to curve
        
        float t = 0f;
        float bestDist = ControlPoints[0].Dist(point);
        for (int i = 1; i < SegmentCount; i++)
        {
            float altRoughDist = ControlPoints[i].Dist(point);
            if (altRoughDist < bestDist)
            {
                t = i / (float)SegmentCount;
                bestDist = altRoughDist;
            }
        }

        float lowT = System.Math.Max(t - 1f / SegmentCount, 0f);
        float highT = System.Math.Min(t + 1f / SegmentCount, 1f);

        for (int i = 0; i < 10; i++)
        {
            Vec3 lowPoint = Eval(lowT);
            Vec3 highPoint = Eval(highT);
            // Project onto line segment lowerP-upperP
            var diff = highPoint - lowPoint;
            var lenSqr = diff.LengthSquared();
            float frac = Utils.Cap((point - lowPoint).Dot(diff) / lenSqr, 0, 1);
            float mid = Utils.Lerp(frac, lowT, highT);
            // Update bounds
            lowT = Utils.Lerp(0.6f, lowT, mid);
            highT = Utils.Lerp(0.6f, mid, highT);
        }

        return (lowT + highT) / 2f;
    }

    public Vec3 EvalInSegment(int segment, float u)
    {
        Vector4 T = new Vector4(1, u, u * u, u * u * u);
        var res = Vector4.Transform(T, MyMat);
        return res.X * _knots[segment] + res.Y * _knots[segment + 1] + res.Z * _knots[segment + 2] + res.W * _knots[segment + 3];
    }

    public void Draw(ExtendedRenderer draw)
    {
        
        List<Vec3> curve = new();
        for (int i = 0; i <= 100; i++)
        {
            curve.Add(Eval(i / 100f));
        }
        draw.Polyline3D(curve);
    }
}
