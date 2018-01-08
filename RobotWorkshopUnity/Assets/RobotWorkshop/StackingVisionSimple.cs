using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingVisionSimple : IStackable
{
    public string Message { get; set; }
    readonly Rect _rect;

    public StackingVisionSimple()
    {
        Message = "Simple vision stacking.";
        float m = 0.02f;
        _rect = new Rect(0 + m, 0 + m, 0.7f - m * 2, 0.8f - m * 2);
    }

    public Orient[] GetNextTargets()
    {
        var topLayer = Motive.GetTiles(_rect);

        if (topLayer == null)
        {
            Message = "Camera error.";
            return null;
        }

        if (topLayer.Count == 0)
        {
            Message = "No more tiles.";
            return null;
        }

        var pick = topLayer.First();
        var place = pick;
        place.Center.x += 0.700f;

        return new[] { pick, place };
    }
}