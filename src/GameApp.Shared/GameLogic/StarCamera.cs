using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharedGameLogic.GameLogic;
using System;

namespace GameApp.Shared.GameLogic;

public class StarCamera
{
    public Vector3 Position { get; private set; }
    public Vector3 Forward { get; private set; }
    public Vector3 Up { get; private set; }
    private Vector3 Right => Vector3.Cross(Forward, Up);

    private const float MoveSpeed = 5f;
    private const float RotationSpeed = 2f;

    // TODO: Remove movement when not debugging
    public StarCamera(Vector3 position, Vector3 forward, Vector3 up)
    {
        Position = position;
        Forward = Vector3.Normalize(forward);
        Up = Vector3.Normalize(up);
    }

    public void Update(KeyboardState keyboardState, GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (keyboardState.IsKeyDown(Keys.W))
            Position += Forward * MoveSpeed * deltaTime;
        if (keyboardState.IsKeyDown(Keys.S))
            Position -= Forward * MoveSpeed * deltaTime;
        if (keyboardState.IsKeyDown(Keys.A))
            Position -= Right * MoveSpeed * deltaTime;
        if (keyboardState.IsKeyDown(Keys.D))
            Position += Right * MoveSpeed * deltaTime;

        if (keyboardState.IsKeyDown(Keys.Left))
            Rotate(Vector3.Up, RotationSpeed * deltaTime);
        if (keyboardState.IsKeyDown(Keys.Right))
            Rotate(Vector3.Up, -RotationSpeed * deltaTime);
        if (keyboardState.IsKeyDown(Keys.Up))
            Rotate(Right, RotationSpeed * deltaTime);
        if (keyboardState.IsKeyDown(Keys.Down))
            Rotate(Right, -RotationSpeed * deltaTime);
    }

    public void Draw(GraphicsDevice graphicsDevice, SpriteFont spriteFont, SpriteBatch spriteBatch, StarSource starSource, ClickDetectionGrid clickDetectionGrid)
    {
        if(!starSource.HasLoadedTextures)
        {
            spriteBatch.DrawString(
                spriteFont, "loading...", 
                graphicsDevice.Viewport.Bounds.Center.ToVector2(), 
                Color.Blue, 
                0f, 
                Vector2.Zero, 
                new Vector2(1f, 1f), 
                SpriteEffects.None, 
                0
            );
        }

        var stars = starSource.GetStars();

        var viewMatrix = GetViewMatrix();
        var perspectiveMatrix = GetPerspectiveMatrix(graphicsDevice);

        var viewProjection =
                viewMatrix
            *   perspectiveMatrix
            ;

        foreach (var star in stars)
        {
            Vector3 viewSpacePosition = Vector3.Transform(star.ThreeDPosition, viewMatrix);

            Rectangle? drawBounds = default;

            // Only draw stars in front of the camera
            if (viewSpacePosition.Z < 0)
            {
                Vector3 projectedPosition = Vector3.Transform(star.ThreeDPosition, viewProjection);

                // Check if the star is within the view frustum
                if (Math.Abs(projectedPosition.X) <= Math.Abs(projectedPosition.Z) &&
                    Math.Abs(projectedPosition.Y) <= Math.Abs(projectedPosition.Z) &&
                    projectedPosition.Z > 0
                )
                {
                    var screenPosition = new Vector2(
                        (projectedPosition.X / projectedPosition.Z + 1) * graphicsDevice.Viewport.Width / 2,
                        (-projectedPosition.Y / projectedPosition.Z + 1) * graphicsDevice.Viewport.Height / 2
                    );

                    // Calculate size based on distance (stars further away appear smaller)
                    float size = MathHelper.Clamp(5f / -viewSpacePosition.Z, 0.1f, 5f);

                    drawBounds = star.Draw(spriteBatch, screenPosition, size);
                }
            }

            clickDetectionGrid.UpdatePosition(star, drawBounds);
        }
    }

    private void Rotate(Vector3 axis, float angle)
    {
        Matrix rotation = Matrix.CreateFromAxisAngle(axis, angle);
        Forward = Vector3.Transform(Forward, rotation);
        Up = Vector3.Transform(Up, rotation);
    }

    public Matrix GetViewMatrix()
    {
        return Matrix.CreateLookAt(Position, Position + Forward, Up);
    }

    // TODO: Check the FOV, if the stars are rendered with parsecs units, the human FOV would be wayyyy less
    public Matrix GetPerspectiveMatrix(GraphicsDevice graphicsDevice)
    {
        return Matrix.CreatePerspectiveFieldOfView(
                    MathHelper.ToRadians(60),
                    graphicsDevice.Viewport.AspectRatio,
                    0.1f,
                    100f
            );
    }

    public Matrix GetProjectionMatrix(GraphicsDevice graphicsDevice)
    {
        var viewMatrix = GetViewMatrix();
        var perspectiveMatrix = GetPerspectiveMatrix(graphicsDevice);

        var viewProjection =
                viewMatrix
            * perspectiveMatrix
            ;

        return viewProjection;
    }
}
