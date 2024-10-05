using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GameApp.Shared.GameLogic;

public class Star
{
    public Star(
        string SourceId,
        Vector3 ThreeDPosition,
        Vector3 Color, // Actual RGB color, still needs to be converted to color mask
        float AbsoluteMagnitude // Im still not sure if this is Absolute or relative but ok
    )
    {
        this.SourceId = SourceId;
        this.ThreeDPosition = ThreeDPosition;
        this.Color = Color;
        this.AbsoluteMagnitude = AbsoluteMagnitude;
    }

    public string SourceId { get; }
    public Vector3 ThreeDPosition { get; }
    public Vector3 Color { get; }
    public Color ColorMask => new Color(
        Color.X / 255f,
        Color.Y / 255f,
        Color.Z / 255f,
        1f
    );

    public float AbsoluteMagnitude { get; }
    public Texture2D? Texture { get; set; }

    public void Draw(SpriteBatch spriteBatch, Vector2 screenPosition, float size)
    {
        if (Texture is null)
            throw new ArgumentNullException(nameof(Texture));

        spriteBatch.Draw(
            Texture,
            screenPosition,
            null,
            ColorMask,
            0f,
            new Vector2(8 / 2, 8 / 2),//new Vector2(Texture.Width / 2, Texture.Height / 2),
            size / 8,//size / Texture.Width,
            SpriteEffects.None,
            0f
        );
    }
}

// Load stars from database
public class StarSource
{
    private List<Star> stars;
    // Quantized magnitude -> Star texture
    private Dictionary<float, Texture2D> _textureCache;

    public StarSource()
    {
        stars = [
           new Star("418551920284673408",
                new Vector3(7.648648470719667f, 1.3661841513924735f, 11.755311234543765f),
                new Vector3(255f, 216f, 184f),
                3.7021639489054473f),
            new Star("4357027756659697664",
                new Vector3(-9.06110402874351f, -18.242450569113956f, -1.315398860223795f),
                new Vector3(255f, 198f, 152f),
                3.380211241711606f),
            new Star("5589311357728452608",
                new Vector3(-1.5179151295580655f, 4.33798136937098f, -3.475517460606429f),
                new Vector3(255f, 199f, 153f),
                4.478849326165926f),
            new Star("4993479684438433792",
                new Vector3(29.32712033687681f, 3.3788212655240293f, -26.869517200852084f),
                new Vector3(255f, 210f, 174f),
                2.7975994179830387f),
            new Star("4038055447778237312",
                new Vector3(1.470087330052893f, -19.078976588391928f, -14.29564479904072f),
                new Vector3(255f, 188f, 133f),
                3.243671468248319f),
            new Star("1279798794197267072",
                new Vector3(-9.256776047172817f, -8.116970462065536f, 6.293151964351856f),
                new Vector3(255f, 214f, 181f),
                3.718620468821298f),
            new Star("160886283751041408",
                new Vector3(1.6472904190515996f, 5.840241416856564f, 3.9657227230770116f),
                new Vector3(255f, 203f, 160f),
                4.279443911906433f),
            new Star("4302054339959905920",
                new Vector3(2.4571200371711006f, -4.914254111997705f, 1.0295457103923173f),
                new Vector3(255f, 201f, 158f),
                4.505171182897414f),
            new Star("1222646935698492160",
                new Vector3(-22.352401792151028f, -30.398565763496322f, 18.98900812563558f),
                new Vector3(255f, 250f, 244f),
                2.7485426387250787f),
            new Star("5111187420714898304",
                new Vector3(8.38834265485218f, 14.244924283509336f, -3.9715518490902237f),
                new Vector3(255f, 193f, 142f),
                3.5390518907565762f),
            new Star("2947050466531873024", // SIRIUS
                new Vector3(-70.19502737134754f, 351.7189554192832f, -107.74455984080053f),
                new Vector3(179f, 204f, 255f),
                -1.46f),
            new Star("3322763588417036032", // BETELGEUSE
                new Vector3(0.01f, 0.5f, -0.06f),
                new Vector3(255f, 216f, 185f),
                .5f
            ),
        ];

        _textureCache = new Dictionary<float, Texture2D>();
    }

    private const int TextureSize = 64;
    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        foreach (var star in stars)
        {
            float quantizedMagnitude = QuantizeMagnitude(star.AbsoluteMagnitude);

            if (!_textureCache.TryGetValue(quantizedMagnitude, out Texture2D? texture))
            {
                texture = CreateStarTexture(graphicsDevice, TextureSize, Color.White, quantizedMagnitude);
                _textureCache[quantizedMagnitude] = texture;
            }

            star.Texture = texture;
        }
    }

    public static Texture2D CreateStarTexture(GraphicsDevice graphicsDevice, int size, Color color, float magnitude)
    {
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colorData = new Color[size * size];

        float radius = size / 2f;
        float centerX = radius;
        float centerY = radius;

        // Adjust brightness based on magnitude (lower magnitude = brighter star)
        float brightness = Math.Max(0, 1 - (magnitude / 6f)); // Assuming magnitude range of 0-6
        color = Color.Lerp(Color.Black, color, brightness);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (distance < radius)
                {
                    float alpha = 1 - (distance / radius);
                    alpha = (float)Math.Pow(alpha, 0.5); // Soften the edge
                    colorData[y * size + x] = Color.Lerp(Color.Transparent, color, alpha);
                }
                else
                {
                    colorData[y * size + x] = Color.Transparent;
                }
            }
        }

        texture.SetData(colorData);
        return texture;
    }

    public IEnumerable<Star> GetStars()
    {
        return stars;
    }

    private const float MagnitudeStep = 0.25f;
    private static float QuantizeMagnitude(float magnitude)
    {
        return (float)Math.Round(magnitude / MagnitudeStep) * MagnitudeStep;
    }
}
