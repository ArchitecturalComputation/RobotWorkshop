using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingTeamBBJT : IStackable
{
    public string Message { get; private set; }
    public IEnumerable<Orient> Display { get { return _startTiles.Concat(_placeTiles); } }

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
        {
            Message = "No tiles left.";
            return false;
        }

        return true;
    }

    IList<Orient> _startTiles;

    public PickAndPlaceData GetNextTargets()
    {
        float m = 0.02f;
        if (_placeTiles.Count == 0)
        {
            // defining the placing area
            //0.75 is the are used for placing the tiles
            var scanRect = new Rect(1.4f * 0.25f + m, 0 + m, 1.4f * 0.75f - m * 2, 0.8f - m * 2);
            var scanTiles = _camera.GetTiles(scanRect);
            Debug.Log("scanTiles Count: " + scanTiles.Count);
            _startTiles = scanTiles;
            if (!CheckCamera(scanTiles)) return null;

            if(scanTiles.Count == 1 ) _placeTiles = ConstructTowersA(scanTiles);
            else _placeTiles = ConstructTowersB(scanTiles);

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

    List<Orient> ConstructTowersA(IList<Orient> scanTiles)
    {
        var towers = new List<Orient>();

        for (int j = 0; j < 10; j++)
        {
                var pos = scanTiles[0].Center;
                var rot = scanTiles[0].Rotation;
                var location = new Orient(pos, rot);
                if (j > 0) towers.Add(TowerLocation(j * 2, location));
                towers.Add(TowerLocation(j * 2 + 1, location));
            
        }

        return towers;
    }

    List<Orient> ConstructTowersB(IList<Orient> scanTiles)
    {

        // safety radius
        // radius from the tile that is the magnitude related to size
        var towers = new List<Orient>();
        
        var mid = Midpoint(scanTiles);
        float maxStep = 0.02f;
        float grip = 0;// 0.015f;
        var radius = new Vector2(_tileSize.x + grip, _tileSize.x + grip).magnitude * 0.5f;

        var centers = scanTiles.Select(t => t.Center + t.Rotation * Vector3.forward * _tileSize.z).ToList();

        var distances = MaxDistance(centers, mid, radius).ToList();

        var vectors = centers
            .Select(c => mid - c);

        //  var distances = vectors
        //     .Select(v => Mathf.Max(v.magnitude - radius * 1, 0));

        var unitVectors = vectors.Select(v => v.normalized).ToList();

        var maxDistance = distances.Max();
        int layers = Mathf.CeilToInt(maxDistance / maxStep);

        var stepDistances = distances.Select(d => d / layers).ToList();

        for (int j = 0; j < layers; j++)
        {
            for (int i = 0; i < scanTiles.Count; i++)
            {
                var stepDistance = stepDistances[i];
                var vector = unitVectors[i] * (stepDistance * j);
                var pos = centers[i] + vector;
                var rot = scanTiles[i].Rotation;
                var location = new Orient(pos, rot);
                if (j > 0) towers.Add(TowerLocation(j * 2, location));
                towers.Add(TowerLocation(j * 2 + 1, location));
            }
        }

        return towers;
    }

    IEnumerable<float> MaxDistance(IList<Vector3> points, Vector3 center, float radius)
    {
        foreach (var point in points)
        {
            var vector = point - center;
            var angles = points.Where(o => o != point).Select(o => Vector3.Angle(vector, (o - center)));
            var angle = angles.Min() * 0.5f;

            var h = radius / Mathf.Sin(angle * (Mathf.PI / 180));
            var distance = vector.magnitude - h;
            if (distance < 0) throw new ArgumentException("Tiles are too close!");
            yield return distance;
        }
    }

    Orient TowerLocation(int index, Orient location, Vector3 midpoint)
    {
        throw new NotImplementedException("Change this method.");
    }

    Orient TowerLocation(int index, Orient location)
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

        tile.Center += location.Center; // + location.Rotation * Vector3.forward * _tileSize.z;


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
           new Orient(0.5f, 0.045f, 0.2f, 45),
           //new Orient(0.3f, 0.045f, 0.6f, 20),
           //new Orient(0.9f, 0.045f, 0.6f, 0),
           //new Orient(0.9f, 0.045f, 0.2f, 0),

           // brick for placing.
           new Orient(0.1f,0.045f,0.5f,90)
        };

        _sequence = new Queue<Orient[]>(new[]
        {

           /*new[] {t[0],t[1],t[2],t[3]},
           new[] {t[4]},
           new[] {t[4]},
           new[] {t[4]},
           new[] {t[4]},
           new[] {t[4]},
           new[] {t[4]},
           new[] {t[4]},
           new[] {t[4]},
           new[] {t[4]},
           new[] {t[4]},
           new[] {t[4]},
           new[] {t[4]},*/

           new[] {t[0]},
           new[] {t[1]},
           new[] {t[1]},
           new[] {t[1]},
           new[] {t[1]},
           new[] {t[1]},
           new[] {t[1]},
           new[] {t[1]},
           new[] {t[1]},
           new[] {t[1]},
           new[] {t[1]},
           new[] {t[1]},
           new[] {t[1]},

           new Orient[0]
        });
    }

    public IList<Orient> GetTiles(Rect area)
    {
        return _sequence.Dequeue();
    }
}
//public class Towers : MonoBehaviour
//{

//    public GameObject _tile;


//}