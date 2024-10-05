using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace GameApp.Shared.GameLogic;

public class ThreeDDebugGrid
{
    private BasicEffect? _basicEffect;
    private readonly VertexPositionColor[] _axisVertices;

    public ThreeDDebugGrid() {
        _axisVertices =
            GetAxisLine(Vector3.Zero, Vector3.UnitX * AxisLength, PositiveX)
            .Concat(GetAxisLine(Vector3.Zero, Vector3.UnitX * -AxisLength, NegativeX))
            .Concat(GetAxisNotches(Vector3.UnitX, Vector3.UnitY, AxisLength, PositiveX, NegativeX))
            .Concat(GetAxisLine(Vector3.Zero, Vector3.UnitY * AxisLength, PositiveY))
            .Concat(GetAxisLine(Vector3.Zero, Vector3.UnitY * -AxisLength, NegativeY))
            .Concat(GetAxisNotches(Vector3.UnitY, Vector3.UnitZ, AxisLength, PositiveY, NegativeY))
            .Concat(GetAxisLine(Vector3.Zero, Vector3.UnitZ * AxisLength, PositiveZ))
            .Concat(GetAxisLine(Vector3.Zero, Vector3.UnitZ * -AxisLength, NegativeZ))
            .Concat(GetAxisNotches(Vector3.UnitZ, Vector3.UnitX, AxisLength, PositiveZ, NegativeZ))
            .ToArray();
    }

    public void LoadContent(GraphicsDevice graphicsDevice, StarCamera starCamera)
    {
        _basicEffect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            World = Matrix.Identity,
            View = starCamera.GetViewMatrix(),
            Projection = starCamera.GetPerspectiveMatrix(graphicsDevice)
        };
    }

    public void Update(StarCamera starCamera)
    {
        _basicEffect.View = starCamera.GetViewMatrix();
    }

    public void Draw(GraphicsDevice graphicsDevice)
    {
        _basicEffect.CurrentTechnique.Passes[0].Apply();
        graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _axisVertices, 0, _axisVertices.Length / 2);
    }

    private const int AxisLength = 10;
    private const float NotchSize = 0.1f;

    private static readonly Color PositiveX = Color.Red;
    private static readonly Color NegativeX = Color.Pink;
    private static readonly Color PositiveY = Color.Green;
    private static readonly Color NegativeY = Color.LightGreen;
    private static readonly Color PositiveZ = Color.Blue;
    private static readonly Color NegativeZ = Color.LightBlue;

    private static IEnumerable<VertexPositionColor> GetAxisLine(Vector3 start, Vector3 end, Color color)
    {
        yield return new VertexPositionColor(start, color);
        yield return new VertexPositionColor(end, color);
    }

    private static IEnumerable<VertexPositionColor> GetAxisNotches(
        Vector3 axisDirection,
        Vector3 notchDirection,
        int length,
        Color positiveColor,
        Color negativeColor
    )
    {
        for (int i = 1; i <= length; i++)
        {
            Vector3 pos = axisDirection * i;
            Vector3 neg = axisDirection * -i;

            yield return new VertexPositionColor(pos, positiveColor);
            yield return new VertexPositionColor(pos + notchDirection * NotchSize, positiveColor);
            yield return new VertexPositionColor(neg, negativeColor);
            yield return new VertexPositionColor(neg + notchDirection * NotchSize, negativeColor);
        }
    }
}
