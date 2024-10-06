using GameApp.Shared.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;

namespace SharedGameLogic.GameLogic;

public class ClickDetectionGrid
{
    public HashSet<(Rectangle, Star)> ScreenPositionDictionary { get; set; } = [];

    public void Update(MouseState current, MouseState previous, ConstelationManager constelationManager)
    {
        if (current.LeftButton == ButtonState.Pressed && previous.LeftButton == ButtonState.Released)
        {
            var click = current.Position;

            var containers = ScreenPositionDictionary.Where(kvp => kvp.Item1.Contains(click.X, click.Y));

            var largestStar = containers.Select(kvp => kvp.Item2).OrderBy(s => s.AbsoluteMagnitude).FirstOrDefault();
            largestStar?.OnClick(constelationManager);
        }
    }

    public void UpdatePosition(Star star, Rectangle? drawRectangle)
    {
        ScreenPositionDictionary.RemoveWhere((kvp) => kvp.Item2 == star);

        if (drawRectangle != null)
        {
            ScreenPositionDictionary.Add((drawRectangle.Value, star));
        }
    }
}
