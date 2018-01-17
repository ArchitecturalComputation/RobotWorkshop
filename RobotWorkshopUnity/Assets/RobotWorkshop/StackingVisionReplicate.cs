using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingVisionReplicate : IStackable
{
    public string Message { get; private set; }
    public IEnumerable<Orient> Display { get; }

    readonly Vector3 _placePoint = new Vector3(0.9f, 0, 0.4f);
    readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
    readonly Rect _rect;
    readonly float _gap = 0.01f;

    bool _isScanning = true;
    int _tileCount = 0;
    List<Orient> _pickTiles = new List<Orient>();

    public StackingVisionReplicate()
    {
        Message = "Replicate vision stacking.";
        float m = 0.02f;
        _rect = new Rect(0 + m, 0 + m, 0.7f - m * 2, 0.8f - m * 2);
    }

    public PickAndPlaceData GetNextTargets()
    {
        if (_isScanning)
        {
            return RememberBlocks();
        }
        else
        {
            return BuildBlocks();
        }
    }

    PickAndPlaceData RememberBlocks()
    {
        var topLayer = Motive.GetTiles(_rect);

        if (topLayer == null)
        {
            Message = "Camera error.";
            return null;
        }

        if (topLayer.Count == 0)
        {
            _isScanning = false;
            Message = "Finished scanning, rebuilding.";
            return BuildBlocks();
        }

        var pick = topLayer.First();
        _pickTiles.Add(pick);
        var place = JengaLocation(_tileCount);
        _tileCount++;
        return new PickAndPlaceData { Pick = pick, Place = place };
    }

    PickAndPlaceData BuildBlocks()
    {
        if (_tileCount == 0) return null;
        var pick = JengaLocation(_tileCount - 1);
        var place = _pickTiles[_tileCount - 1];
        _tileCount--;
        return new PickAndPlaceData { Pick = pick, Place = place };
    }

    Orient JengaLocation(int index)
    {
        int count = index;
        int layer = count / 3;
        int horiz = count % 3;
        bool isEven = layer % 2 == 0;

        Vector3 position = new Vector3(0, (layer + 1) * _tileSize.y, (horiz - 1) * (_tileSize.z + _gap));
        var rotation = Quaternion.Euler(0, isEven ? 0 : -90, 0) * Quaternion.Euler(0, 180f, 0);
        return new Orient(rotation * position + _placePoint, rotation);
    }
}