using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GameApp.Shared.GameLogic;

public class ThreeDDebugGrid
{
    private BasicEffect? _sphereEffect;
    private VertexPositionColor[] _sphereVertices;
    private int _sphereVertexCount;

    public ThreeDDebugGrid() { }

    public void LoadContent(GraphicsDevice graphicsDevice, StarCamera starCamera)
    {
        Matrix rotation = Matrix.CreateRotationY(0f);

        _sphereEffect = new BasicEffect(graphicsDevice);
        _sphereEffect.VertexColorEnabled = true;
        _sphereEffect.Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(60), graphicsDevice.Viewport.AspectRatio, 0.01f, 100f);
        _sphereEffect.View = Matrix.CreateLookAt(Vector3.Zero, Vector3.Transform(Vector3.Forward, rotation), Vector3.Up);

        CreateWireframeSphere(10, 20, 20);
    }

    public void Update(StarCamera starCamera, GameTime gameTime)
    {
        _sphereEffect.View = starCamera.GetViewMatrix();
    }

    public void Draw(GraphicsDevice graphicsDevice)
    {
        graphicsDevice.RasterizerState = new RasterizerState { FillMode = FillMode.Solid };

        foreach (var pass in _sphereEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _sphereVertices, 0, _sphereVertexCount / 2);
        }
    }

    private void CreateWireframeSphere(float radius, int latitudes, int longitudes)
    {
        _sphereVertexCount = (latitudes + 1) * (longitudes + 1) * 4;
        _sphereVertices = new VertexPositionColor[_sphereVertexCount];

        int index = 0;

        for (int lat = 0; lat <= latitudes; lat++)
        {
            float theta = lat * MathHelper.Pi / latitudes;
            float sinTheta = (float)Math.Sin(theta);
            float cosTheta = (float)Math.Cos(theta);

            for (int lon = 0; lon <= longitudes; lon++)
            {
                float phi = lon * 2 * MathHelper.Pi / longitudes;
                float sinPhi = (float)Math.Sin(phi);
                float cosPhi = (float)Math.Cos(phi);

                float x = radius * sinTheta * cosPhi;
                float y = radius * cosTheta;
                float z = radius * sinTheta * sinPhi;

                Vector3 position = new Vector3(x, y, z);

                if (lon < longitudes)
                {
                    _sphereVertices[index++] = new VertexPositionColor(position, Color.White);
                    _sphereVertices[index++] = new VertexPositionColor(
                        new Vector3(
                            radius * sinTheta * (float)Math.Cos((lon + 1) * 2 * MathHelper.Pi / longitudes),
                            y,
                            radius * sinTheta * (float)Math.Sin((lon + 1) * 2 * MathHelper.Pi / longitudes)
                        ),
                        Color.White
                    );
                }

                if (lat < latitudes)
                {
                    _sphereVertices[index++] = new VertexPositionColor(position, Color.White);
                    _sphereVertices[index++] = new VertexPositionColor(
                        new Vector3(
                            radius * (float)Math.Sin((lat + 1) * MathHelper.Pi / latitudes) * cosPhi,
                            radius * (float)Math.Cos((lat + 1) * MathHelper.Pi / latitudes),
                            radius * (float)Math.Sin((lat + 1) * MathHelper.Pi / latitudes) * sinPhi
                        ),
                        Color.White
                    );
                }
            }
        }
    }
}
