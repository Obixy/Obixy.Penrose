using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameApp.Shared.GameLogic;

public class Constelation
{
    public string ExoplanetJobId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Votes { get; set; }
    public Stack<(string, Vector3)> Points { get; set; } = [];
}

public class ConstelationManager
{
    private Constelation? _currentConstellation => ConstellationUnderConstruction ?? ConstellationUnderAnalysys;
    public Constelation? ConstellationUnderAnalysys { get; set; }
    public Constelation? ConstellationUnderConstruction { get; set; }

    private readonly BasicEffect basicEffect;

    public ConstelationManager(GraphicsDevice graphicsDevice)
    {
        basicEffect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), graphicsDevice.Viewport.AspectRatio, 0.1f, 100f),
            View = Matrix.CreateLookAt(Vector3.Zero, Vector3.Zero + Vector3.Forward, Vector3.Up)
        };
    }

    public void AddStar(Star star)
    {
        if (_currentConstellation != null)
        {
            if (_currentConstellation.Points.LastOrDefault().Item1 != star.SourceId)
            {
                _currentConstellation.Points.Push((star.SourceId, star.ThreeDPosition));
            }
        }
    }

    public void Update(GraphicsDevice graphicsDevice, StarCamera starCamera, KeyboardState keyboardState)
    {
        basicEffect.View = starCamera.GetViewMatrix();
        basicEffect.Projection = starCamera.GetPerspectiveMatrix(graphicsDevice);

        if (keyboardState.IsKeyDown(Keys.LeftControl) && keyboardState.IsKeyDown(Keys.Z))
        {
            var current = ConstellationUnderConstruction ?? ConstellationUnderAnalysys;
            var points = current?.Points ?? (IEnumerable<(string, Vector3)>)Array.Empty<(string, Vector3)>();
            if (points.Any())
                current!.Points = new Stack<(string, Vector3)>(points.SkipLast(1));
        }

        if (keyboardState.IsKeyDown(Keys.LeftControl) && keyboardState.IsKeyDown(Keys.S))
        {
            var current = ConstellationUnderConstruction ?? ConstellationUnderAnalysys;
            var points = current?.Points ?? (IEnumerable<(string, Vector3)>)Array.Empty<(string, Vector3)>();
            if (points.Any())
                current!.Points = new Stack<(string, Vector3)>(points.SkipLast(1));
        }
    }

    public void Draw(GraphicsDevice graphicsDevice)
    {
        Stack<(string, Vector3)>? pointsToRender = default;
        if (ConstellationUnderConstruction != null)
            pointsToRender = ConstellationUnderConstruction.Points;
        else if (ConstellationUnderAnalysys != null)
            pointsToRender = ConstellationUnderAnalysys.Points;

        if (pointsToRender is not null)
        {
            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                var points = pointsToRender.Select(x => x.Item2).ToArray();

                for (int i = 0; i < points.Length - 1; i++)
                {
                    var lineVertices = new VertexPositionColor[2]
                    {
                        new VertexPositionColor(points[i], Color.Green),
                        new VertexPositionColor(points[i + 1], Color.Green)
                    };

                    graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, lineVertices, 0, 1);
                }
            }
        }
    }
}
