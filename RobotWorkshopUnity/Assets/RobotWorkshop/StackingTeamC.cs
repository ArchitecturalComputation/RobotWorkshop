using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StackingTeamC : IStackable
{
    public string Message { get; set; }

    readonly Vector3 _pickPoint = new Vector3(0.2f, 0, 0.4f);
    readonly Vector3 _placePoint = new Vector3(1.2f, 0, 0.4f);
    readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
    readonly float _gap = 0.005f;
    readonly ICamera _camera;

    List<Orient> _placeTiles = new List<Orient>();

    public StackingTeamC(Mode mode)
    {
        _camera = mode == Mode.Virtual ? new TeamCVirtualCamera() as ICamera : new LiveCamera() as ICamera;
        //MakePickTower();
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

    public Orient[] GetNextTargets()
    {
        float m = 0.02f;

        if (_placeTiles.Count == 0)
        {
            // definig place area and scaning
            var scanRect = new Rect(1.4f * 0.25f + m, 0 + m, 1.4f * 0.75f - m * 2, 0.8f - m * 2);
            var scanTiles = _camera.GetTiles(scanRect);
            if (!CheckCamera(scanTiles)) return null; // checking if camera is working
            if (scanTiles.Count > 1)
            {
                Message = "Place a single tile on the next row and press 'start loop'.";
                return null;
            }

            _placeTiles = CreateRow(scanTiles.First(), scanRect.center);
        }

        // definig pick area and scaning
        var pickRect = new Rect(0 + m, 0 + m, 1.4f * 0.25f - m * 2, 0.8f - m * 2);
        var pickTiles = _camera.GetTiles(pickRect);
        if (!CheckCamera(pickTiles)) return null;

        var pick = pickTiles.First();
        var place = _placeTiles.First();
        _placeTiles.RemoveAt(0);

        Message = $"{_placeTiles.Count} tiles remaining in the row";
        return new[] { pick, place }
        .Concat(_placeTiles).ToArray();
    }

    List<Orient> CreateRow(Orient orient, Vector3 center)
    {
        
        throw new NotImplementedException("To be implemented by team C.");
        
     
        var radius = Math.Sqrt(Math.Pow(center.x - tiles.x, 2.0) - Math.Pow(center.y - Tiles.y, 2.0));
        var brickNum = Math.PI / Math.Atan(0.18f / 2*radius); // PI/(Atan(a/2*r) = N
        //var perimeter = 2 * Math.PI * radius;
        var roAngle = 360 / brickNum; //rotating angle between bricks.
        var newTile = orient.RotateAround(center, (float)roAngle); 
    }
}

class TeamCVirtualCamera : ICamera
{
    Queue<Orient[]> _sequence;
    public TeamCVirtualCamera()
    {
        var t = new[]
        {
           new Orient(0.1f,0.045f,0.3f,90.0f), //layer 1
           new Orient(0.2f,0.045f*2,0.1f,30.0f), //layer 2
           new Orient(0.1f,0.045f,0.5f,90)
        };

        _sequence = new Queue<Orient[]>(new[]
        {
           new[] {t[0]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new Orient[0]
        });
    }

    public IList<Orient> GetTiles(Rect area)
    {
        return _sequence.Dequeue();
    }
}
