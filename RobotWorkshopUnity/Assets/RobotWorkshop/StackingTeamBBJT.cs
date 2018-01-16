using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingTeamBBJT : IStackable
{
    public string Message { get; set; }

    readonly Vector3 _pickPoint = new Vector3(0.2f, 0, 0.4f);
    readonly Vector3 _placePoint = new Vector3(1.2f, 0, 0.4f);
    readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
    readonly int _tileCount = 24;
    readonly float _gap = 0.005f;
    readonly Rect _rect;

    List<Orient> _pickTiles = new List<Orient>();
    List<Orient> _placeTiles = new List<Orient>();

    public StackingTeamBBJT()
    {
        /* NOT USED ATM
        Message = "Our vision stackning";
        float m = 0.02f;
        _rect = new Rect(0 + m, 0 + m, 0.7f - m * 2, 0.8f - m * 2);*/

        MakePickTower();
    }

    public Orient[] GetNextTargets()
    {
        //var topLayer = Motive.GetTiles(_rect);
        var _startTiles = simulateCamera();

        if (_pickTiles.Count == 0)
        {
            Message = "No tiles left.";
            return null;
        }

        if (_startTiles == null)
        {
            Message = "Camera error.";
            return null;
        }

        var pick = _pickTiles.Last();

        var place = NewLocation(_tileCount - _pickTiles.Count, _startTiles);

        place.Center += _placePoint;
        _pickTiles.RemoveAt(_pickTiles.Count - 1);
        _placeTiles.Add(pick);

        Message = $"Placing tile {_placeTiles.Count} out of {_tileCount}";

        return new[] { pick, place };
    }

    Orient NewLocation(int index, List<Orient> startTiles)
    {
        int towers = startTiles.Count;
        int count = index;
        int layer = count / 2 / towers;

        int towerIndex = count % towers;
        bool evenLayer = layer % 2 == 0;

        int row = ((count % (towers*2)) - towerIndex)/ 2;

        var rotation = Quaternion.Euler(0, evenLayer ? 0 : -90, 0);
        rotation = rotation * startTiles[towerIndex].Rotation;

        Vector3 position = new Vector3(0, (layer + 1) * _tileSize.y, (row - 0.5f) * (_tileSize.z * 2) + (0.015f * layer));
        position = startTiles[towerIndex].Rotation * position;
        position = position + startTiles[towerIndex].Center;

        if (!evenLayer)
        {
            if (row == 0)
            {
                Vector3 move = new Vector3(0.06f, 0, 0.06f);
                position = startTiles[towerIndex].Rotation * move + position;
            }
            else
            {
                Vector3 move = new Vector3(-0.06f, 0, -0.06f);
                position = startTiles[towerIndex].Rotation * move + position;
            }
        }

        return new Orient(position, rotation);
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

        Vector3 position = new Vector3(0, (layer + 1) * _tileSize.y, (row - 1) * _tileSize.z + _gap);
        var rotation = Quaternion.Euler(0, isEven ? 0 : -90, 0);
        return new Orient(rotation * position, rotation);
    }

    List<Orient> simulateCamera()
    {
        var _tempList = new List<Orient>();

        Vector3 tempStart1 = new Vector3(0, 0, 0);
        var rotStart1 = Quaternion.Euler(0, 0, 0);
        _tempList.Add(new Orient(tempStart1, rotStart1));

        Vector3 tempStart2 = new Vector3(-0.4f, 0, 0);
        var rotStart2 = Quaternion.Euler(0, 45, 0);
        _tempList.Add(new Orient(tempStart2, rotStart2));

        Vector3 tempStart3 = new Vector3(-0.8f, 0, 0);
        var rotStart3 = Quaternion.Euler(0, 20, 0);
        _tempList.Add(new Orient(tempStart3, rotStart3));

        return _tempList;
    }
}
