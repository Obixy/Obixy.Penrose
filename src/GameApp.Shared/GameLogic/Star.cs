using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using static System.Formats.Asn1.AsnWriter;

namespace GameApp.Shared.GameLogic;

public class Star
{
    private const float SPHERE_RADIUS = 10f;

    public Star(
        float SourceId,
        float RightAscension, // The RA is given in degrees on the International Celestial Reference System (ICRS) at the reference epoch J2016.0.
        float Declination, // The DEC is given in degrees on the ICRS at the reference epoch J2016.0.
        float Parallax,
        float BPRP,
        float GMag
    )
    {
        this.SourceId = SourceId.ToString();
        this.RightAscension = RightAscension;
        this.Declination = Declination;
        this.Parallax = Parallax;
        this.BPRP = BPRP;
        this.GMag = GMag;
        CalculateThreeDProjectedPosition(RightAscension, Declination, Parallax);
        CalculateTemperature(BPRP);
        CalculateColor(EstimatedTemperature);
        CalculateAbsoluteMagnitude(GMag, 1000 / Parallax);
    }

    public string SourceId { get; }
    public float RightAscension { get; }
    public float Declination { get; }
    public float Parallax { get; }
    public float BPRP { get; }
    public float GMag { get; }
    public Vector3 ThreeDPosition { get; set; } // Ignoring parallax
    public double EstimatedTemperature { get; set; }
    public Vector3 Color { get; set; }
    public Color ColorMask => new Color(
        Color.X / 255f,
        Color.Y / 255f,
        Color.Z / 255f,
        1f
    );
    public double AbsoluteMagnitude { get; set; }
    public Texture2D? Texture { get; set; }

    private Vector3? _oringinalColor;

    public void OnClick()
    {
        if (_oringinalColor == null)
        {
            _oringinalColor = Color;
            Color = new Vector3(Microsoft.Xna.Framework.Color.LightGreen.R, Microsoft.Xna.Framework.Color.LightGreen.G, Microsoft.Xna.Framework.Color.LightGreen.B);
        }
        else
        {
            Color = _oringinalColor!.Value;
            _oringinalColor = null;
        }

    }

    private void CalculateThreeDProjectedPosition(float ra, float dec, float parallax)
    {
        // Convert RA and DEC to radians
        float raRad = MathHelper.ToRadians(ra);
        float decRad = MathHelper.ToRadians(dec);

        // Convert spherical coordinates to Cartesian
        float x = SPHERE_RADIUS * (float)(Math.Cos(decRad) * Math.Cos(raRad));
        float y = SPHERE_RADIUS * (float)(Math.Cos(decRad) * Math.Sin(raRad));
        float z = SPHERE_RADIUS * (float)Math.Sin(decRad);
        ThreeDPosition = new Vector3(x, y, z);
    }

    private void CalculateColor(double estimatedTemperature)
    {
        var teff = estimatedTemperature / 100;

        double Red, Green, Blue;

        if (teff <= 66)
        {
            Red = 255;
        }
        else
        {
            Red = teff - 60;
            Red = 329.698727466 * Math.Pow(Red, -0.1332047592);
            Red = Math.Clamp(Red, 0, 255);
        }

        if (teff <= 66)
        {
            Green = teff;
            Green = 99.4708025861 * Math.Log(Green) - 161.1195681661;
        }
        else
        {
            Green = teff - 60;
            Green = 288.1221695283 * Math.Pow(Green, -0.0755148492);
        }
        Green = Math.Clamp(Green, 0, 255);

        if (teff >= 66)
        {
            Blue = 255;
        }
        else if (teff <= 19)
        {
            Blue = 0;
        }
        else
        {
            Blue = teff - 10;
            Blue = 138.5177312231 * Math.Log(Blue) - 305.0447927307;
            Blue = Math.Clamp(Blue, 0, 255);
        }

        Color = new Vector3((int)Math.Round(Red), (int)Math.Round(Green), (int)Math.Round(Blue));
    }

    private void CalculateTemperature(float BPRP)
    {
        // Approximate conversion to temperature
        double temp = 4600d * ((1d / (0.92d * BPRP + 1.7d)) + (1d / (0.92d * BPRP + 0.62d)));

        // Clamp the result to a reasonable range for stars
        EstimatedTemperature = Math.Clamp(temp, 2000, 50000);
    }

    private void CalculateAbsoluteMagnitude(double apparentMagnitude, double distancePc)
    {
        // Distance modulus formula: m - M = 5 * log10(d) - 5
        // Where m is apparent magnitude, M is absolute magnitude, and d is distance in parsecs
        AbsoluteMagnitude = apparentMagnitude - 5 * Math.Log10(distancePc) + 5;
    }

    public Rectangle Draw(SpriteBatch spriteBatch, Vector2 screenPosition, float size)
    {
        if (Texture is null)
            throw new ArgumentNullException(nameof(Texture));

        var drawRectangle = CenterRectangleAtPoint(ScaleRectangle(Texture.Bounds, Math.Max(size, 1)), screenPosition /*- new Vector2(Texture.Width / 2, Texture.Height / 2)*/);

        spriteBatch.Draw(
            Texture,
            screenPosition,
            null,
            ColorMask,
            0f,
            new Vector2(Texture.Width / 2, Texture.Height / 2),
            size,
            SpriteEffects.None,
            0f
        );

        return drawRectangle;
    }

    public static Rectangle ScaleRectangle(Rectangle rect, float scale)
    {
        // Calculate the new width and height
        int newWidth = (int)(rect.Width * scale);
        int newHeight = (int)(rect.Height * scale);

        // Adjust X and Y to keep the rectangle centered
        int newX = rect.X - (newWidth - rect.Width) / 2;
        int newY = rect.Y - (newHeight - rect.Height) / 2;

        // Return the new scaled rectangle
        return new Rectangle(newX, newY, newWidth, newHeight);
    }

    public static Rectangle CenterRectangleAtPoint(Rectangle rect, Vector2 newCenter)
    {
        // Calculate the new X and Y by centering the rectangle at the new point
        int newX = (int)(newCenter.X - rect.Width / 2);
        int newY = (int)(newCenter.Y - rect.Height / 2);

        // Return the new centered rectangle
        return new Rectangle(newX, newY, rect.Width, rect.Height);
    }
}

public class DrawParameters
{
    public Rectangle DrawBounds { get; set; }
}
