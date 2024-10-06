using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GameApp.Shared.GameLogic;

// Load stars from database
public class StarSource
{
    private List<Star> stars;
    // Quantized magnitude -> Star texture
    private Dictionary<double, Texture2D> _textureCache;

    public StarSource()
    {
        stars = [
            new Star(
                SourceId: 160886283751041408   ,
                RightAscension: 74.24843702624405f  ,
                Declination: 33.16601472318802f,
                Parallax: 7.249065034494049f,
                BPRP: 1.4451852f,
                GMag: 2.1975422f
            ),
            new Star(
                SourceId: 418551920284673408   ,
                RightAscension: 10.12724197930297f     ,
                Declination: 56.53718879378639f,
                Parallax: 14.090976249252234f,
                BPRP: 1.1434835f,
                GMag: 1.9425238f
            ),
            new Star(
                SourceId: 1222646935698492160  ,
                RightAscension: 233.67254376294218f    ,
                Declination: 26.714295174747274f,
                Parallax: 42.24080076261629f,
                BPRP: 0.529583f,
                GMag: 2.2693703f
            ),
            new Star(
                SourceId: 1279798794197267072  ,
                RightAscension: 221.24648617076684f    ,
                Declination: 27.074315757773466f,
                Parallax: 13.82667260913517f,
                BPRP: 1.1840065f,
                GMag: 2.1833525f
            ),
            new Star(
                SourceId: 2947050466531873024  , // SIRIUS // S3VV000636
                RightAscension: 101.28715515137f    ,
                Declination: -16.71611595154f,
                Parallax: 379.2200f,
                BPRP: -1.530862f,
                GMag: -1.46f
            ),
            new Star(
                SourceId: 3322763588417036032  , // BETELGEUSE
                RightAscension: 88.8572254952028f      ,
                Declination: 7.227819034090208f,
                Parallax: 0.5363052504330691f,
                BPRP: 1.6358423f,
                GMag: 0.50f
            ),
            new Star(
                SourceId: 4038055447778237312  ,
                RightAscension: 274.4060904518451f     ,
                Declination: -36.76242931758906f,
                Parallax: 23.885852068095137f,
                BPRP: 1.8272674f,
                GMag: 2.1164951f
            ),
            new Star(
                SourceId: 4302054339959905920  ,
                RightAscension: 296.56498571046956f    ,
                Declination: 10.613252703828636f,
                Parallax: 5.589928355256642f,
                BPRP: 1.4782449f,
                GMag: 2.223692f
            ),
            new Star(
                SourceId: 4357027756659697664  ,
                RightAscension: 243.58621066034064f    ,
                Declination: -3.694967708333353f,
                Parallax: 20.411292350652413f,
                BPRP: 1.5633104f,
                GMag: 2.0164251f
            ),
            new Star(
                SourceId: 4993479684438433792  ,
                RightAscension: 6.57215550145885f      ,
                Declination: -42.30782032569077f,
                Parallax: 39.91825815729799f,
                BPRP: 1.2694821f,
                GMag: 2.0899775f
            ),
            new Star(
                SourceId: 5111187420714898304  ,
                RightAscension: 59.507642022011794f    ,
                Declination: -13.509012610930261f,
                Parallax: 17.00162879163835f,
                BPRP: 1.689486f,
                GMag: 2.2707934f
            ),
            new Star(
                SourceId: 5589311357728452608  ,
                RightAscension: 109.28559423278779f    ,
                Declination: -37.097444485279574f,
                Parallax: 5.762063161817684f,
                BPRP: 1.5425804f,
                GMag: 2.0832374f
            )
        ];

        _textureCache = new Dictionary<double, Texture2D>();
    }

    private const int TextureSize = 16;
    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        foreach (var star in stars)
        {
            var quantizedMagnitude = QuantizeMagnitude(star.AbsoluteMagnitude);

            if (!_textureCache.TryGetValue(quantizedMagnitude, out Texture2D? texture))
            {
                texture = CreateStarTexture(graphicsDevice, TextureSize, Color.White, quantizedMagnitude);
                _textureCache[quantizedMagnitude] = texture;
            }

            star.Texture = texture;
        }
    }

    public static Texture2D CreateStarTexture(GraphicsDevice graphicsDevice, int textureSize, Color color, double magnitude)
    {
        // Calculate the scaled texture size based on magnitude
        var scaledSize = (int)CalculateScaledSize(magnitude) * textureSize;

        // Create a new texture with the scaled size
        Texture2D texture = new Texture2D(graphicsDevice, scaledSize, scaledSize);

        // Create an array to hold the color data for each pixel
        Color[] colorData = new Color[scaledSize * scaledSize];

        // Calculate the center of the texture
        Vector2 center = new Vector2(scaledSize / 2f);

        // Fill the texture with the star pattern
        for (int y = 0; y < scaledSize; y++)
        {
            for (int x = 0; x < scaledSize; x++)
            {
                Vector2 position = new Vector2(x, y);
                float distance = Vector2.Distance(position, center);
                float radius = scaledSize / 2f;

                // Create a circular gradient
                float alpha = MathHelper.Clamp(1f - (distance / radius), 0f, 1f);
                alpha = (float)Math.Pow(alpha, 2); // Square the alpha for a smoother falloff

                colorData[y * scaledSize + x] = color * alpha;
            }
        }

        // Set the texture data
        texture.SetData(colorData);

        return texture;
    }

    // Constants for scaling
    private const float MinScale = 0.1f;  // Minimum scale for dim stars
    private const float MaxScale = 3.0f;  // Maximum scale for bright stars
    private const float MinMagnitude = -10f;  // Brighter stars (e.g., supergiants)
    private const float MaxMagnitude = 15f;   // Dimmer stars (e.g., faint dwarfs)

    public static float CalculateScaledSize(double absoluteMagnitude)
    {
        // Clamp the magnitude within a reasonable range
        float clampedMagnitude = MathHelper.Clamp((float)absoluteMagnitude, MinMagnitude, MaxMagnitude);

        // Map magnitude to a scale using linear interpolation
        float scale = MathHelper.Lerp(MaxScale, MinScale,
            (clampedMagnitude - MinMagnitude) / (MaxMagnitude - MinMagnitude));

        return scale;
    }

    public IEnumerable<Star> GetStars()
    {
        return stars;
    }

    private const double MagnitudeStep = 0.25f;
    private static double QuantizeMagnitude(double magnitude)
    {
        return Math.Round(magnitude / MagnitudeStep) * MagnitudeStep;
    }
}
