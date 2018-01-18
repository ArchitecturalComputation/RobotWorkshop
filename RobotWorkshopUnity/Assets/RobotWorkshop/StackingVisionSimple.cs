using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingVisionSimple : IStackable
{
    public string Message { get; private set; }
    public IEnumerable<Orient> Display { get { return _placedTiles; } }

    readonly Rect _rect;
    readonly ICamera _camera;

    List<Orient> _placedTiles = new List<Orient>();

    public StackingVisionSimple(Mode mode)
    {
        Message = "Simple vision stacking.";
        float m = 0.02f;
        _rect = new Rect(0 + m, 0 + m, 0.7f - m * 2, 0.8f - m * 2);
        _camera = mode == Mode.Virtual ? new VirtualCamera() as ICamera : new LiveCamera() as ICamera;
    }

    public PickAndPlaceData GetNextTargets()
    {
        var topLayer = _camera.GetTiles(_rect);

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
        _placedTiles.Add(place);

        return new PickAndPlaceData { Pick = pick, Place = place };
    }
}