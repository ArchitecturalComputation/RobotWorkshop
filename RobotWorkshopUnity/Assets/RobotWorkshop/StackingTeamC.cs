using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingTeamC : IStackable
{
    public string Message { get; set; }

    readonly Vector3 _pickPoint = new Vector3(0.2f, 0, 0.4f);
    readonly Vector3 _placePoint = new Vector3(1.2f, 0, 0.4f);
    readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
    readonly int _tileCount = 6;
    readonly float _gap = 0.005f;

    List<Orient> _pickTiles = new List<Orient>();
    List<Orient> _placeTiles = new List<Orient>();

    public StackingTeamC()
    {
        MakePickTower();
    }

    public Orient[] GetNextTargets()
    {
        if (_pickTiles.Count == 0)
        {
            Message = "No tiles left.";
            return null;
        }

        var pick = _pickTiles.Last();

        var place = RandomLocation(_tileCount - _pickTiles.Count);
        place.Center += _placePoint;

        _pickTiles.RemoveAt(_pickTiles.Count - 1);
        _placeTiles.Add(pick);

        Message = $"Placing tile {_placeTiles.Count} out of {_tileCount}";

        return new[] { pick, place };
    }


    void MakePickTower()
    {
        for (int i = 0; i < _tileCount; i++)
        {
            var next = JengaLocation(i);
            _pickTiles.Add(new Orient(_pickPoint + next.Center, next.Rotation));
        }
    }

    Orient JengaLocation(int index)
    {
        int count = index;
        int layer = count / 3;
        int row = count % 3;
        bool isEven = layer % 2 == 0;

        Vector3 position = new Vector3(0, (layer + 1) * _tileSize.y, (row - 1) * _tileSize.z);
        var rotation = Quaternion.Euler(0, isEven ? 0 : -90, 0);
        return new Orient(rotation * position, rotation);
    }

    Orient RandomLocation(int index)
    {
        int count = _placeTiles.Count;
        int layer = count / 2;
        int row = count % 2 * Random.Range(1, 2);
        bool isEven = layer % 2 == 0;

        Vector3 position = new Vector3(0, (layer + 1) * _tileSize.y, (row - 1) * _tileSize.z + _gap);
        var rotation = Quaternion.Euler(0, isEven ? 0 : -90, 0);
        return new Orient(rotation * position, rotation);
    }
}