using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _3DFun
{
    static class Program
    {
        class MainForm : Form { }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var mainForm = new MainForm();
            mainForm.Load += (s, e) => Everyting(mainForm);
            Application.Run(mainForm);
            return;

            static void Everyting(MainForm form)
            {
                form.Text = "Cube";

                // Model:

                var cube = Data.CreateCube();
                var rotationCenter = Model.GetCenter(cube);

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
                var moveZBackTranslation = new Vector3(0, 0, -4); // so that the model is visible from origin
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
                    var projectedVertices = (from vertex in cube.Vertices
                                             let rotated = Vector3.Transform(vertex, rotationAndZBackTranslation)
                                             let projected = Project2D(rotated)
                                             let remapped = Vector2.Transform(projected, remap)
                                             select remapped).ToList();

                    var projectedEdges = cube.TemplatedWireframeEdges.Select(x => new Edge<Vector2>(projectedVertices[x.A], projectedVertices[x.B]));

                    foreach (var (A, B) in projectedEdges)
                    {
                        e.Surface.Canvas.DrawLine(A.X, A.Y, B.X, B.Y, fgPaint);
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
                form.Controls.Add(control);
                control.PaintSurface += (s, e) =>
                {
                    e.Surface.Canvas.Clear(bgColor);
                    DrawCube(e);
                    DrawFPS(e);
                };

                // Force refresh as much as possible:
                bool exit = false;
                form.FormClosed += (s, e) => exit = true;
                form.Shown += Form_Shown;
                void Form_Shown(object sender, EventArgs e)
                {
                    form.Shown -= Form_Shown;
                    while (!exit)
                    {
                        control.Invalidate();
                        Application.DoEvents();
                    }
                }
            }
        }
    }
}
