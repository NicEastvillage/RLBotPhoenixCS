/*
 * Author: Nicolaj 'Eastvillage', @NicEastvillage
 */

using System;
using System.Collections.Generic;
using System.Linq;
using RedUtils.Math;
using RLBot.Flat;
using RLBot.Manager;
using Color = System.Drawing.Color;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace RedUtils
{
    /// <summary>
    /// An extension of the default Renderer to draw debug lines in-game.
    /// </summary>
    public class ExtendedRenderer
    {
        /// <summary>Reference to the default renderer</summary>
        private readonly Renderer _renderer;
        private readonly float _screenWidth;
        private readonly float _screenHeight;

        public Color Color
        {
            get => _renderer.Color;
            set => _renderer.Color = value;
        }

        /// <summary>Initialize an ExtendedRenderer.</summary>
        public ExtendedRenderer(Renderer renderer, float screenWidth, float screenHeight)
        {
            _renderer = renderer;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
        }

        /// <summary>Draws text in screenspace</summary>
        public void Text2D(
            string text,
            float x,
            float y,
            float scale = 1f,
            Color? foreground = null,
            Color? background = null,
            TextHAlign hAlign = TextHAlign.Left,
            TextVAlign vAlign = TextVAlign.Top)
        {
            _renderer.DrawText2D(text, x / _screenWidth, y / _screenHeight, scale, foreground, background, hAlign, vAlign);
        }

        /// <summary>Draws text at a point in world space</summary>
        public void Text3D(
            string text,
            RenderAnchorT anchor,
            float scale = 1f,
            Color? foreground = null,
            Color? background = null,
            TextHAlign hAlign = TextHAlign.Left,
            TextVAlign vAlign = TextVAlign.Top)
        {
            _renderer.DrawText3D(text, anchor, scale, foreground, background, hAlign, vAlign);
        }
        
        /// <summary>Draws text at a point in world space</summary>
        public void Text3D(
            string text,
            Vec3 pos,
            float scale = 1f,
            Color? foreground = null,
            Color? background = null,
            TextHAlign hAlign = TextHAlign.Left,
            TextVAlign vAlign = TextVAlign.Top)
        {
            _renderer.DrawText3D(text, pos.ToAnchor(), scale, foreground, background, hAlign, vAlign);
        }

        /// <summary>Draws a rectangle at a point in world space</summary>
        public void Rect3D(
            RenderAnchorT anchor,
            float width,
            float height,
            Color? color = null)
        {
            _renderer.DrawRect3D(anchor, width, height, color);
        }
        
        /// <summary>Draws a rectangle at a point in world space</summary>
        public void Rect3D(
            Vec3 pos,
            float width,
            float height,
            Color? color = null)
        {
            _renderer.DrawRect3D(pos.ToAnchor(), width, height, color);
        }

        /// <summary>Draws a line in world space</summary>
        public void Line3D(RenderAnchorT start, RenderAnchorT end, Color? color = null)
        {
            _renderer.DrawLine3D(start, end, color);
        }
        
        /// <summary>Draws a line in world space</summary>
        public void Line3D(Vec3 start, Vec3 end, Color? color = null)
        {
            _renderer.DrawLine3D(start.ToAnchor(), end.ToAnchor(), color);
        }

        /// <summary>Draws a line in world space consisting between each pair of points in the given array</summary>
        public void Polyline3D(IEnumerable<Vec3> points, Color? color = null)
        {
            _renderer.DrawPolyLine3D(points.Select(v => v.ToFlatBuf()), color);
        }

        /// <summary>Draws a circle</summary>
        public void Circle(Vec3 pos, Vec3 normal, float radius, Color? color = null)
        {
            Vec3 offset = normal.Cross(pos).Normalize() * radius;
            int segments = (int)MathF.Pow(radius, 0.69f) + 4;
            float angle = 2 * MathF.PI / segments;
            Mat3x3 rotMat = Mat3x3.RotationFromAxis(normal.Normalize(), angle);

            Vec3[] points = new Vec3[segments + 1];
            for (int i = 0; i <= segments; i++) {
                offset = rotMat.Dot(offset);
                points[i] = pos + offset;
            }

            Polyline3D(points, color);
        }

        /// <summary>Draws a cross</summary>
        public void Cross(Vec3 pos, float size, Color? color = null)
        {
            float half = size / 2;
            Line3D(pos + half * Vec3.X, pos - half * Vec3.X, color);
            Line3D(pos + half * Vec3.Y, pos - half * Vec3.Y, color);
            Line3D(pos + half * Vec3.Z, pos - half * Vec3.Z, color);
        }

        /// <summary>Draws an angled cross</summary>
        public void CrossAngled(Vec3 pos, float size, Color? color = null)
        {
            float r = 0.5f * size / MathF.Sqrt(2);;
            Line3D(pos + new Vec3(r, r, r), pos + new Vec3(-r, -r, -r), color);
            Line3D(pos + new Vec3(r, r, -r), pos + new Vec3(-r, -r, r), color);
            Line3D(pos + new Vec3(r, -r, -r), pos + new Vec3(-r, r, r), color);
            Line3D(pos + new Vec3(r, -r, r), pos + new Vec3(-r, r, -r), color);
        }

        /// <summary>Draws a cube</summary>
        public void Cube(Vec3 pos, float size, Color? color = null)
        {
            Cube(pos, new Vec3(size, size, size), color);
        }

        /// <summary>Draws a cube</summary>
        public void Cube(Vec3 pos, Vec3 size, Color? color = null)
        {
            Vec3 half = size / 2;
            Line3D(pos + new Vec3(-half.x, -half.y, -half.z), pos + new Vec3(-half.x, -half.y, half.z), color);
            Line3D(pos + new Vec3(half.x, -half.y, -half.z), pos + new Vec3(half.x, -half.y, half.z), color);
            Line3D(pos + new Vec3(-half.x, half.y, -half.z), pos + new Vec3(-half.x, half.y, half.z), color);
            Line3D(pos + new Vec3(half.x, half.y, -half.z), pos + new Vec3(half.x, half.y, half.z), color);
            
            Line3D(pos + new Vec3(-half.x, -half.y, -half.z), pos + new Vec3(-half.x, half.y, -half.z), color);
            Line3D(pos + new Vec3(half.x, -half.y, -half.z), pos + new Vec3(half.x, half.y, -half.z), color);
            Line3D(pos + new Vec3(-half.x, -half.y, half.z), pos + new Vec3(-half.x, half.y, half.z), color);
            Line3D(pos + new Vec3(half.x, -half.y, half.z), pos + new Vec3(half.x, half.y, half.z), color);
            
            Line3D(pos + new Vec3(-half.x, -half.y, -half.z), pos + new Vec3(half.x, -half.y, -half.z), color);
            Line3D(pos + new Vec3(-half.x, -half.y, half.z), pos + new Vec3(half.x, -half.y, half.z), color);
            Line3D(pos + new Vec3(-half.x, half.y, -half.z), pos + new Vec3(half.x, half.y, -half.z), color);
            Line3D(pos + new Vec3(-half.x, half.y, half.z), pos + new Vec3(half.x, half.y, half.z), color);
        }

        /// <summary>Draws a cube with the given rotation. Ideal to draw hit boxes.</summary>
        public void OrientatedCube(Vec3 pos, Mat3x3 orientation, Vec3 size, Color? color = null)
        {
            Vec3 half = size / 2;
            Mat3x3 rotT = orientation.Transpose();
            Line3D(pos + rotT.Dot(new Vec3(-half.x, -half.y, -half.z)), pos + rotT.Dot(new Vec3(-half.x, -half.y, half.z)), color);
            Line3D(pos + rotT.Dot(new Vec3(half.x, -half.y, -half.z)), pos + rotT.Dot(new Vec3(half.x, -half.y, half.z)), color);
            Line3D(pos + rotT.Dot(new Vec3(-half.x, half.y, -half.z)), pos + rotT.Dot(new Vec3(-half.x, half.y, half.z)), color);
            Line3D(pos + rotT.Dot(new Vec3(half.x, half.y, -half.z)), pos + rotT.Dot(new Vec3(half.x, half.y, half.z)), color);
            
            Line3D(pos + rotT.Dot(new Vec3(-half.x, -half.y, -half.z)), pos + rotT.Dot(new Vec3(-half.x, half.y, -half.z)), color);
            Line3D(pos + rotT.Dot(new Vec3(half.x, -half.y, -half.z)), pos + rotT.Dot(new Vec3(half.x, half.y, -half.z)), color);
            Line3D(pos + rotT.Dot(new Vec3(-half.x, -half.y, half.z)), pos + rotT.Dot(new Vec3(-half.x, half.y, half.z)), color);
            Line3D(pos + rotT.Dot(new Vec3(half.x, -half.y, half.z)), pos + rotT.Dot(new Vec3(half.x, half.y, half.z)), color);
            
            Line3D(pos + rotT.Dot(new Vec3(-half.x, -half.y, -half.z)), pos + rotT.Dot(new Vec3(half.x, -half.y, -half.z)), color);
            Line3D(pos + rotT.Dot(new Vec3(-half.x, -half.y, half.z)), pos + rotT.Dot(new Vec3(half.x, -half.y, half.z)), color);
            Line3D(pos + rotT.Dot(new Vec3(-half.x, half.y, -half.z)), pos + rotT.Dot(new Vec3(half.x, half.y, -half.z)), color);
            Line3D(pos + rotT.Dot(new Vec3(-half.x, half.y, half.z)), pos + rotT.Dot(new Vec3(half.x, half.y, half.z)), color);
        }

        /// <summary>Draws an octahedron</summary>
        public void Octahedron(Vec3 pos, float size, Color? color = null)
        {
            float half = size / 2;
            Line3D(pos + new Vec3(half, 0, 0), pos + new Vec3(0, half, 0), color);
            Line3D(pos + new Vec3(0, half, 0), pos + new Vec3(-half, 0, 0), color);
            Line3D(pos + new Vec3(-half, 0, 0), pos + new Vec3(0, -half, 0), color);
            Line3D(pos + new Vec3(0, -half, 0), pos + new Vec3(half, 0, 0), color);
            
            Line3D(pos + new Vec3(half, 0, 0), pos + new Vec3(0, 0, half), color);
            Line3D(pos + new Vec3(0, 0, half), pos + new Vec3(-half, 0, 0), color);
            Line3D(pos + new Vec3(-half, 0, 0), pos + new Vec3(0, 0, -half), color);
            Line3D(pos + new Vec3(0, 0, -half), pos + new Vec3(half, 0, 0), color);
            
            Line3D(pos + new Vec3(0, half, 0), pos + new Vec3(0, 0, half), color);
            Line3D(pos + new Vec3(0, 0, half), pos + new Vec3(0, -half, 0), color);
            Line3D(pos + new Vec3(0, -half, 0), pos + new Vec3(0, 0, -half), color);
            Line3D(pos + new Vec3(0, 0, -half), pos + new Vec3(0, half, 0), color);
        }

        /// <summary>Helper function to convert a Vec3 to a Vector3</summary>
        private Vector3 NumVec(Vec3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
        
        /// <summary>Helper function to convert a Vec3 to a Vector2</summary>
        private Vector2 NumVec2(Vec3 v)
        {
            return new Vector2(v.x, v.y);
        }
    }
}
