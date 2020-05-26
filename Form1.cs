using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace _3DFun
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Text = "Cube";

            // Geometry:

            var initialCubePoints = new List<Vector3>
            {
               new Vector3(-1, +1, +1), // 0
               new Vector3(+1, +1, +1), // 1
               new Vector3(+1, -1, +1), // 2
               new Vector3(-1, -1, +1), // 3
                //                      // 
               new Vector3(-1, +1, -1), // 4
               new Vector3(+1, +1, -1), // 5
               new Vector3(+1, -1, -1), // 6
               new Vector3(-1, -1, -1), // 7
            };

            static List<(T A, T B)> GetCubeSegments<T>(IList<T> cubePoints) => new List<(T A, T B)>(12)
            {
                (cubePoints[0], cubePoints[1]),
                (cubePoints[1], cubePoints[2]),
                (cubePoints[2], cubePoints[3]),
                (cubePoints[3], cubePoints[0]),
                //
                (cubePoints[4], cubePoints[5]),
                (cubePoints[5], cubePoints[6]),
                (cubePoints[6], cubePoints[7]),
                (cubePoints[7], cubePoints[4]),
                //
                (cubePoints[0], cubePoints[4]),
                (cubePoints[1], cubePoints[5]),
                (cubePoints[2], cubePoints[6]),
                (cubePoints[3], cubePoints[7]),
            };

            var topPlaneDiagonal = Vector3.Add(initialCubePoints[0], initialCubePoints[5]);
            var rotationCenter = Vector3.Divide(topPlaneDiagonal, 2); // should be the origin in this case (ignoring Y)

            // Animation update timer:

            int animationStep = 0;
            float animationMaxStep = 300;
            var timer = new System.Timers.Timer(15);
            timer.Elapsed += (s, e) =>
            {
                animationStep++;
                if (animationStep > animationMaxStep)
                    animationStep = 0;
            };
            timer.Start();

            // Main update and draw method:

            var bgColor = SKColors.Black;
            var fgPaint = new SKPaint
            {
                Color = SKColors.Green,
                StrokeWidth = 2,
                IsAntialias = false
            };
            var moveZBackTranslation = new Vector3(0, 0, -4); // so that the cube is visible from origin
            var renderSize = new Vector2(400);
            Vector2 lastRenderTargetSize = Vector2.Zero;
            Matrix3x2 remap = Matrix3x2.Identity;

            static Vector2 Project2D(Vector3 v) => new Vector2(v.X / -v.Z, v.Y / -v.Z);
            static Matrix3x2 GetRemapMatrix(Vector2 size, Vector2 renderTargetSize)
            {
                var normalizeScale = Matrix3x2.CreateScale(.5f);
                var scaleToSize = Matrix3x2.CreateScale(size);
                var newOrigin = new Vector2(renderTargetSize.X / 2f, renderTargetSize.Y / 2f);
                var translateOrigin = Matrix3x2.CreateTranslation(newOrigin);
                return normalizeScale * scaleToSize * translateOrigin;
            }

            void DrawCube(SKPaintGLSurfaceEventArgs e)
            {
                var renderTargetSize = new Vector2(e.BackendRenderTarget.Width, e.BackendRenderTarget.Height);
                if (renderTargetSize != lastRenderTargetSize)
                {
                    remap = GetRemapMatrix(renderSize, renderTargetSize);
                    lastRenderTargetSize = renderTargetSize;
                }
                var currentStepAngle = (animationStep / animationMaxStep) * (Math.PI * 2);
                var rotationAndZBackTranslation = Matrix4x4.CreateRotationY((float)currentStepAngle, rotationCenter);
                rotationAndZBackTranslation.Translation = moveZBackTranslation;
                var projectedPoints = (from point in initialCubePoints
                                       let rotated = Vector3.Transform(point, rotationAndZBackTranslation)
                                       let projected = Project2D(rotated)
                                       let remapped = Vector2.Transform(projected, remap)
                                       select remapped).ToList();

                var projectedSegments = GetCubeSegments(projectedPoints);

                foreach (var segment in projectedSegments)
                {
                    e.Surface.Canvas.DrawLine(segment.A.X, segment.A.Y, segment.B.X, segment.B.Y, fgPaint);
                }
            };

            // FPS display:

            var fpsStopwatch = Stopwatch.StartNew();
            var halfSecond = TimeSpan.FromSeconds(.5f); // refresh time
            // https://stackoverflow.com/questions/4687430/c-calculating-moving-fps/4687507#4687507
            // Choose alpha depending on how fast or slow you want old averages to decay. 0.9 is usually a good choice.
            double fpsAlpha = 0.2;
            double fpsAvg = 1; // actually per duration. see above.
            int framesThisSecond = 0; // actually per duration. see above.
            var fpsPaint = new SKPaint
            {
                Color = SKColors.Yellow,
                IsAntialias = true,
                TextSize = 16,
                Typeface = SKTypeface.FromFamilyName("Consolas"),
            };

            void DrawFPS(SKPaintGLSurfaceEventArgs e)
            {
                framesThisSecond++;
                if (fpsStopwatch.Elapsed >= halfSecond)
                {
                    fpsStopwatch.Restart();
                    fpsAvg = (fpsAlpha * fpsAvg + (1.0 - fpsAlpha) * framesThisSecond);
                    framesThisSecond = 0;
                }

                var fps = fpsAvg * 2;
                var fpsString = $"{fps:00} FPS";
                e.Surface.Canvas.DrawText(fpsString, 10, 20, fpsPaint);
            };

            // Drawing surface and actual refresh method:

            var control = new SKGLControl();
            control.VSync = true;
            control.Dock = DockStyle.Fill;
            Controls.Add(control);
            control.PaintSurface += (s, e) =>
            {
                e.Surface.Canvas.Clear(bgColor);
                DrawCube(e);
                DrawFPS(e);
            };

            // Force refresh as much as possible:
            bool exit = false;
            FormClosed += (s, e) => exit = true;
            Shown += Form_Shown;
            void Form_Shown(object sender, EventArgs e)
            {
                Shown -= Form_Shown;
                while (!exit)
                {
                    control.Invalidate();
                    Application.DoEvents();
                }
            }
        }
    }
}
