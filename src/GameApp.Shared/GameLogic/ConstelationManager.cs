using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

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

    const string url = "https://nsac-obixy-penrose-data-auefcgedgjhyanbw.brazilsouth-01.azurewebsites.net/exoplanets/{0}/constellation";

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
        if (ConstellationUnderConstruction != null)
        {
            if (ConstellationUnderConstruction.Points.LastOrDefault().Item1 != star.SourceId)
            {
                ConstellationUnderConstruction.Points.Push((star.SourceId, star.ThreeDPosition));
            }
        }
    }

    private bool HasBegunUploading = false;
    public void Update(GraphicsDevice graphicsDevice, StarCamera starCamera, KeyboardState keyboardState, Guid? constellationId, bool isBuildingConstellation, Guid? exoplanetJobId)
    {
        if (isBuildingConstellation && exoplanetJobId != null && ConstellationUnderConstruction is null)
        {
            ConstellationUnderConstruction = new Constelation { ExoplanetJobId = exoplanetJobId.Value.ToString() };
        }

        basicEffect.View = starCamera.GetViewMatrix();
        basicEffect.Projection = starCamera.GetPerspectiveMatrix(graphicsDevice);

        if (keyboardState.IsKeyDown(Keys.LeftControl) && keyboardState.IsKeyDown(Keys.Z) && ConstellationUnderConstruction != null)
        {
            var current = ConstellationUnderConstruction ?? ConstellationUnderAnalysys;
            var points = current?.Points ?? (IEnumerable<(string, Vector3)>)Array.Empty<(string, Vector3)>();
            if (points.Any())
                current!.Points = new Stack<(string, Vector3)>(points.SkipLast(1));
        }

        if (
            keyboardState.IsKeyDown(Keys.LeftControl) && 
            keyboardState.IsKeyDown(Keys.LeftShift) && 
            keyboardState.IsKeyDown(Keys.S) && 
            ConstellationUnderConstruction != null && 
            exoplanetJobId != null &&
            !HasBegunUploading
        )
        {
            Task.Run(async () =>
            {
                HasBegunUploading = true;
                using var httpClient = new HttpClient();
                var rawString = JsonConvert.SerializeObject(new
                {
                    name = $"New Constellation: {Guid.NewGuid()}",
                    points = ConstellationUnderConstruction.Points.Select(p => new
                    {
                        sourceId = p.Item1,
                        x = p.Item2.X,
                        y = p.Item2.Y,
                        Z = p.Item2.Z
                    }).ToArray()
            })!;
            var content = new StringContent(rawString, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));
            _ = await httpClient.PostAsync(string.Format(url, exoplanetJobId!.Value), content);
            HasBegunUploading=false;
        });
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
