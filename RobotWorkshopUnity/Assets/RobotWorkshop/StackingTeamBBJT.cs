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
    
    // _placeTiles =  all the values/Vectors where the tiles will be place according to the script
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

    IList<Orient> _firstScan;

    public Orient[] GetNextTargets()
    {
        float m = 0.02f;
        // if the the list that should have the Orient values to place the tiles is empty (.count==0)
        if (_placeTiles.Count == 0)
        {
            // defining the placing area
            //0.75 is the are used for placing the tiles
            var scanRect = new Rect(1.4f * 0.25f + m, 0 + m, 1.4f * 0.75f - m * 2, 0.8f - m * 2);
            var scanTiles = _camera.GetTiles(scanRect); // get values fro the camere
            if (!CheckCamera(scanTiles)) return null; // everything is fine with the camera

            var _midpoint = midpoint(scanTiles);

            // for all the 15 tiles in each tower
            for (int i = 1; i < 15; i++)
            { 
                // consider the ones (tile) the camera scanned int the placing area (scanTiles)
                foreach (var tile in scanTiles)
                {
                    // build the 'nextTile' in relation to the previous (tile)          
                    var nextTile = TowerLocation(i, tile,_midpoint);
                    // add the Orients for the 'next Tiles' in the _placeTiles List
                    _placeTiles.Add(nextTile);
                }
            }
            _firstScan = scanTiles; //remove later; (why?)
        }

        if (_tileCount >= _placeTiles.Count)
        {
            Message = "Finished building towers.";
            return null;
        }
        // the 0.25 is the are used to pick the tiles
        var pickRect = new Rect(0 + m, 0 + m, 1.4f * 0.25f - m * 2, 0.8f - m * 2);
        var pickTiles = _camera.GetTiles(pickRect);
        if (!CheckCamera(pickTiles)) return null;

        // picking and placing 
        var pick = pickTiles.First(); // take the first one (check pickTiles method)
        var place = _placeTiles[_tileCount]; //the orient to place 'place' = placetiles [index]
        _tileCount++; //increment the count (move to the next index) and start again

        // message while constructing
        Message = $"Placing tile {_tileCount} out of {_placeTiles.Count}";
        return new[] { pick, place } // 
        .Concat(_placeTiles).ToArray(); // just to display all the values
    }

    // the logic to build the towers
    // to create, we use index: and location:
    Orient TowerLocation(int index, Orient location, Vector3 midpoint)
    {
        int count = index;
        int layer = count / 2;
        int row = count % 2;
        bool isEven = layer % 2 == 0;

        // this logic creates the tower tile per tile
        //1. create the tower logic in the origin (estabilishes the relationship between tiles)
        //a. position: moves in Z> row 0> / row 
        //b. rotating 90deg for even layers
        //c. 'create' tile : rotate the position so the center moves / and then rotates the tile
        Vector3 position = new Vector3(0, (layer) * _tileSize.y, (row * 2 - 1) * _tileSize.z);//a.
        var rotation = Quaternion.Euler(0, isEven ? 0 : -90, 0); //b.
        var tile = new Orient(rotation*position, rotation); //c.
       
        //2. relocate the tile to its ACTUAL location
        tile.Center = location.Rotation * tile.Center; // translate the center to the place
        tile.Rotation = location.Rotation * tile.Rotation; // rotate the tiles (same as before c.)


        Orient tilt = new Orient(0, 0, 0, 8f * layer);
        tile = tile.Transform(tilt);

        // translating the whole tower
        tile.Center += location.Center + location.Rotation * Vector3.forward * _tileSize.z;

        // going towards the midle:
        var toMiddle = towardsMiddle(midpoint, tile.Center) * layer;
        tile.Center.z += toMiddle.z;
        tile.Center.x += toMiddle.x;

     
        return tile;

    }

    Vector3 midpoint(IList<Orient> startTiles)
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
        // array to simulate what the camera is seeing.
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
            // WHERE TO Check camera (temporary)
            // 1 = placing area 2+= only picking area
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