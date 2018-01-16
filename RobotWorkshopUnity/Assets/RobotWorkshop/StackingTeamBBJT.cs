using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingTeamBBJT : IStackable
{
    public string Message { get; set; }

    readonly Vector3 _pickPoint = new Vector3(0.2f, 0, 0.4f);
    readonly Vector3 _placePoint = new Vector3(1.2f, 0, 0.4f);
    readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
    readonly float _gap = 0.005f;
    readonly Rect _rect;
    readonly ICamera _camera;

    List<Orient> _placeTiles = new List<Orient>();
    int _tileCount = 0;

    public StackingTeamBBJT(Mode mode)
    {
        Message = "Our vision stackning.";
        _camera = mode == Mode.Virtual ? new TeamBBJTVirtualCamera() as ICamera : new LiveCamera() as ICamera;
    }

    bool CheckCamera(IList<Orient> tiles)
    {
        if (tiles.Count == 0)
        {
            Message = "No tiles left.";
            return false;
        }

        if (tiles == null)
        {
            Message = "Camera error.";
            return false;
        }

        return true;
    }

    IList<Orient> _firstScan;

    public Orient[] GetNextTargets()
    {
        float m = 0.02f;

        if (_placeTiles.Count == 0)
        {
            var scanRect = new Rect(1.4f * 0.25f + m, 0 + m, 1.4f * 0.75f - m * 2, 0.8f - m * 2);
            var scanTiles = _camera.GetTiles(scanRect);
            if (!CheckCamera(scanTiles)) return null;

            for (int i = 1; i < 10; i++)
            {
                foreach (var tile in scanTiles)
                {
                    var nextTile = TowerLocation(i, tile);
                    _placeTiles.Add(nextTile);
                }
            }

            _firstScan = scanTiles; //remove later;
        }

        if (_tileCount >= _placeTiles.Count)
        {
            Message = "Finished building towers.";
            return null;
        }

        var pickRect = new Rect(0 + m, 0 + m, 1.4f * 0.25f - m * 2, 0.8f - m * 2);
        var pickTiles = _camera.GetTiles(pickRect);
        if (!CheckCamera(pickTiles)) return null;

        var pick = pickTiles.First();
        var place = _placeTiles[_tileCount];
        _tileCount++;

        Message = $"Placing tile {_tileCount} out of {_placeTiles.Count}";
        return new[] { pick, place }
        .Concat(_placeTiles).ToArray();
    }

    Orient TowerLocation(int index, Orient location)
    {
        int count = index;
        int layer = count / 2;
        int row = count % 2;
        bool isEven = layer % 2 == 0;

        Vector3 position = new Vector3(0, (layer) * _tileSize.y, (row * 2 - 1) * _tileSize.z);
        var rotation = Quaternion.Euler(0, isEven ? 0 : -90, 0);
        var tile = new Orient(rotation * position, rotation);

        tile.Center = location.Rotation * tile.Center;
        tile.Rotation = location.Rotation * tile.Rotation;

        tile.Center += location.Center + location.Rotation * Vector3.forward * _tileSize.z;
        return tile;

        // Vector3 _midpoint = midpoint(startTiles);

        //var rotation = Quaternion.Euler(0, evenLayer ? 0 : -90, 0);
        //rotation = rotation * startRotation;

        //Vector3 toMiddle = towardsMiddle(_midpoint, startPosition);

        //Vector3 position = new Vector3(toMiddle.x * layer, (layer + 1) * _tileSize.y, (row - 0.5f) * (_tileSize.z * 2) + (toMiddle.z * layer));
        //position = (startRotation * position) + startPosition;

        //if (!evenLayer)
        //{
        //    Vector3 move = new Vector3(-0.12f * row + 0.06f, 0, -0.12f * row + 0.06f);
        //    position = startRotation * move + position;
        //}

        //return new Orient(position, rotation);
    }



    Vector3 midpoint(List<Orient> startTiles)
    {
        Vector3 sum = Vector3.zero;

        foreach (var tile in startTiles)
        {
            sum += tile.Center;
        }

        return sum / startTiles.Count;
    }

    Vector3 towardsMiddle(Vector3 midpoint, Vector3 position)
    {

        var vector = midpoint - position;
        var distance = Mathf.Min(vector.magnitude, 0.015f);

        return position + vector.normalized * distance;
    }
}

class TeamBBJTVirtualCamera : ICamera
{
    Queue<Orient[]> _sequence;

    public TeamBBJTVirtualCamera()
    {
        var t = new[]
        {
           new Orient(0.6f,0.045f,0.4f,45),
           new Orient(0.9f,0.045f,0.2f,20),
           new Orient(0.45f,0.045f,0.5f,45),
           new Orient(0.1f,0.045f,0.5f,90)
        };

        _sequence = new Queue<Orient[]>(new[]
        {
           new[] {t[0],t[1],t[2]},
           new[] {t[3]},
           new[] {t[3]},
           new[] {t[3]},
           new[] {t[3]},
           new[] {t[3]},
           new[] {t[3]},
           new[] {t[3]},
           new[] {t[3]},
           new[] {t[3]},
           new[] {t[3]},
           new[] {t[3]},
           new[] {t[3]},
           new Orient[0]
        });
    }

    public IList<Orient> GetTiles(Rect area)
    {
        return _sequence.Dequeue();
    }
}