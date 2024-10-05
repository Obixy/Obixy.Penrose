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
                SourceId: 2947050466531873024  , // SIRIUS
                RightAscension: 101.28662552099249f    ,
                Declination: -16.720932526023173f,
                Parallax: 374.48958852876103f,
                BPRP: -0.27842712f,
                GMag: 8.524133f
            ),
            new Star(
                SourceId: 3322763588417036032  , // BETELGEUSE
                RightAscension: 88.8572254952028f      ,
                Declination: 7.227819034090208f,
                Parallax: 0.5363052504330691f,
                BPRP: 1.1323872f,
                GMag: 13.313011f
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

    private const int TextureSize = 64;
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

    public static Texture2D CreateStarTexture(GraphicsDevice graphicsDevice, int size, Color color, double magnitude)
    {
        Texture2D texture = new Texture2D(graphicsDevice, size, size);
        Color[] colorData = new Color[size * size];

        float radius = size / 2f;
        float centerX = radius;
        float centerY = radius;

        // Adjust brightness based on magnitude (lower magnitude = brighter star)
        var brightness = Math.Max(0, 1 - (magnitude / 6d)); // Assuming magnitude range of 0-6
        color = Color.Lerp(Color.Black, color, (float)brightness);

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

    private const double MagnitudeStep = 0.25f;
    private static double QuantizeMagnitude(double magnitude)
    {
        return Math.Round(magnitude / MagnitudeStep) * MagnitudeStep;
    }
}
