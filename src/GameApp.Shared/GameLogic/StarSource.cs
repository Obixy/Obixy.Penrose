using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameApp.Shared.GameLogic;

// Load stars from database
public class StarSource
{
    private IEnumerable<Star> stars = [];
    // Quantized magnitude -> Star texture
    private Dictionary<double, Texture2D> _textureCache = new Dictionary<double, Texture2D>();
    private readonly HttpClient httpClient = new();
    const string url = "https://localhost:7013/exoplanets/{0}/stars";
    private Task? _startsQueryTask;
    public bool IsLoading = false;
    public bool HasLoadedTextures = false;

    public Task EnsureStartsQueryTask(Guid exoplanetId)
    {
        return _startsQueryTask ??= Task.Run(async () =>
        {
            IsLoading = true;
            using var httpClient = new HttpClient();
            using var streamReader = new StreamReader(await httpClient.GetStreamAsync(string.Format(url, exoplanetId)));

            var stars = new HashSet<Dictionary<string, string>>();

            while (!streamReader.EndOfStream)
            {
                var line = await streamReader.ReadLineAsync();

                var json = line![6..];
                var starsData = JsonSerializer.Deserialize<ICollection<Dictionary<string, string>>>(json)!;
                Console.WriteLine($"{starsData.Count}");

                stars.Add(starsData.SelectMany(starData => starData).ToDictionary());
            }

            this.stars = stars.Select(star => new Star(
                float.Parse(star["sourceId"]),
                0,
                0,
                0,
                0,
                0
            ));

            IsLoading = false;
        });
    }

    private const int TextureSize = 16;
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

    public void Update(GraphicsDevice graphicsDevice, Guid jobId)
    {
        EnsureStartsQueryTask(jobId);

        if (!IsLoading && stars.Any())
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

            HasLoadedTextures = true;
        }
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
