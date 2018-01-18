using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingTeamBBJT : IStackable
{
    public string Message { get; private set; }
    public IEnumerable<Orient> Display { get { return _placeTiles; } }

    readonly Vector3 _pickPoint = new Vector3(0.2f, 0, 0.4f);
    readonly Vector3 _placePoint = new Vector3(1.2f, 0, 0.4f);
    readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
    readonly float _gap = 0.005f;
    readonly Rect _rect;
    readonly ICamera _camera;
    
    List<Orient> _placeTiles = new List<Orient>();
    // tileCount = INDEX;
    int _tileCount = 0;

    public StackingTeamBBJT(Mode mode)
    {
        //defing which mode we are using: virtual = simualtion ; other = real robot
        Message = "Team BBJT stacking tiles";
        _camera = mode == Mode.Virtual ? new TeamBBJTVirtualCamera() as ICamera : new LiveCamera() as ICamera;
    }

    // check camera is run before the code to verify wether the data is correct to start.
    // the checked data are: 1. tiles.Count

    bool CheckCamera(IList<Orient> tiles)
    {   
        // the camera dont see anything.
        if (tiles == null)
        {
            Message = "Camera error.";
            return false;
        }
        // there is no tiles in the camera scan
        if (tiles.Count == 0)

        return true;
    }

    IList<Orient> _firstScan;

    public PickAndPlaceData GetNextTargets()
    {
        float m = 0.02f;
        if (_placeTiles.Count == 0)
        {
            // defining the placing area
            //0.75 is the are used for placing the tiles
            var scanRect = new Rect(1.4f * 0.25f + m, 0 + m, 1.4f * 0.75f - m * 2, 0.8f - m * 2);
            var scanTiles = _camera.GetTiles(scanRect);
            if (!CheckCamera(scanTiles)) return null; 

           var _midpoint =  Midpoint(scanTiles);

            for (int i = 1; i < 15; i++)
            { 
                foreach (var tile in scanTiles)
                {  
                    var nextTile = TowerLocation(i, tile,_midpoint);
                    _placeTiles.Add(nextTile);
                }
            }
            _firstScan = scanTiles; //remove later; (why?) ¯\_(ツ)_/¯
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

        // message while constructing
        Message = $"Placing tile {_tileCount} out of {_placeTiles.Count}";
        return new PickAndPlaceData { Pick = pick, Place = place };
    }

    Orient TowerLocation(int index, Orient location, Vector3 midpoint)
    {
        int count = index;
        int layer = count / 2;
        int row = count % 2;
        bool isEven = layer % 2 == 0;

        Vector3 position = new Vector3(0, (layer) * _tileSize.y, (row * 2 - 1) * _tileSize.z);//a.
        var rotation = Quaternion.Euler(0, isEven ? 0 : -90, 0); //b.
        var tile = new Orient(rotation * position, rotation); //c.

        tile.Center = location.Rotation * tile.Center;
        tile.Rotation = location.Rotation * tile.Rotation;

        Orient tilt = new Orient(0, 0, 0, 8f * layer);
        tile = tile.Transform(tilt);

        tile.Center += location.Center + location.Rotation * Vector3.forward * _tileSize.z;

        var toMiddle = towardsMiddle(midpoint, tile.Center) * layer;
        tile.Center.z += toMiddle.z;
        tile.Center.x += toMiddle.x;

        return tile;
    }

    Vector3 Midpoint(IList<Orient> startTiles)
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
        // the difference between midpoint and position = distance to get to the center
        var vector = midpoint - position;
        float stepDistance;

       // distance = Mathf.Max(vector.magnitude -  0.5f * _tileSize.x -  0.5f * _tileSize.y)
        if (vector.magnitude < 0.70f)
        {
            stepDistance = 0.000f;
        }
        else
        {
            stepDistance = 0.025f;
        }
        var distance = Mathf.Min(vector.magnitude, stepDistance);//
        return vector.normalized* distance;
    }
}

// this virtual camera is replaced by the actual one when we use LiveRobot
//Cameras are on all the time
class TeamBBJTVirtualCamera : ICamera
{
    Queue<Orient[]> _sequence;

    public TeamBBJTVirtualCamera()
    {
        var t = new[]
        {
            //bricks place in the placing area = only scaned once.
            // discribed (x,y,z, rotation in y)
           new Orient(0.7f, 0.045f, 0.3f, 45),
           new Orient(0.5f, 0.045f, 0.6f, 20),
           new Orient(0.8f, 0.045f, 0.6f, 0),
           // brick for placing.
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