﻿using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
public class StackingTeamC : IStackable
{
    public string Message { get; set; }
    public IEnumerable<Orient> Display { get { return _placeTiles; } }

    //readonly float _gap = 0.005f;
    //readonly Vector3 _pickPoint = new Vector3(0.2f, 0, 0.4f);
   // readonly Vector3 _placePoint = new Vector3(1.2f, 0, 0.4f);
   // readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
    readonly ICamera _camera;
    List<Orient> _placeTiles = new List<Orient>();

    public StackingTeamC(Mode mode)
    {
        _camera = mode == Mode.Virtual ? new TeamCVirtualCamera() as ICamera : new LiveCamera() as ICamera;
    }

    bool CheckCamera(IList<Orient> tiles)
    {
        if (tiles == null)
        {
            Message = "Camera error.";
            return false;
        }
        if (tiles.Count == 0)
        {
            Message = "No tiles left.";
            return false;
        }
        return true;
    }

    public PickAndPlaceData GetNextTargets()
    {
        float m = 0.02f;

        if (_placeTiles.Count == 0)
        {
            // definig place area and scaning
            var scanRect = new Rect(1.4f * 0.25f + m,   0 + m,  1.4f * 0.75f - m * 2,   0.8f - m * 2); //placing space(1.05m)
            var scanTiles = _camera.GetTiles(scanRect);

            var _center = new Vector3(0.7f,0f,0.4f);
            if (!CheckCamera(scanTiles)) return null;
            if (scanTiles.Count > 1)
            {
                Message = "Place a single tile on the next row and press 'start loop'.";
                return null;
            }
            _placeTiles = CreateRow(scanTiles.First(), _center);
        }

        // definig pick area and scaning
        var pickRect = new Rect(0 + m, 0 + m, 1.4f * 0.25f - m * 2, 0.8f - m * 2); //picking space (0.35m)
        var pickTiles = _camera.GetTiles(pickRect);
        if (!CheckCamera(pickTiles)) return null;
        var pick = pickTiles.First();
        var place = _placeTiles.First();
        _placeTiles.RemoveAt(0);
        Message = $"{_placeTiles.Count} tiles remaining in the row";

        return new PickAndPlaceData { Pick = pick, Place = place };  //,Retract=true  // to fulfil brick in real time. but makes error in simulation.
    }

    List<Orient> CreateRow(Orient orient, Vector3 center) //orient is the scanned brick's center
    {   //float m = 0.02f;
        //scan Rect = (1.4f * 0.25f + m ,   0 + m,    1.4f * 0.75f - m * 2,   0.8f - m * 2);
        // pickRect = (0 + m            ,   0 + m,    1.4f * 0.25f - m * 2,   0.8f - m * 2); =vector(1.4 , 0, 0.8)
        // _pickPoint =(0.2f, 0, 0.4f);
        //_placePoint = new Vector3(1.2f, 0, 0.4f);
        // _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
        //_center = new Vector3(0.7f,0f,0.4f);

        var allBlocks = new List<Orient>();
        var radius = Vector3.Distance(orient.Center, center); //radius point oriented - center of orient 
        var tileNumberf = (2 * Mathf.PI * radius) / (0.18f * 1.2f); //Number of Tile
             int tileNumber = (int)Mathf.Floor(tileNumberf);

        for (int i = 1; i < tileNumber; i++)
        {
            var angle = 360 / (float)tileNumber;
            var newTile = orient.RotateAround(center, i*angle); //newTile is the birck's center of new one
            allBlocks.Add(newTile);
        }

        // even number changes rule.
        if (scanTiles.Count > 1 )
        {
            tileNumber = (int)Mathf.Floor((2 * Mathf.PI * radius) / (0.06f * 1.2f));
            for (int i = 1; i < tileNumber; i++)
            {
                var angle = 360 / (float)tileNumber;
                var newTile = orient.RotateSecond(center, i * angle); //newTile is the birck's center of new one
                allBlocks.Add(newTile);
            }
        }
        //

            return allBlocks;
    }


    //for the second rule 
    public Orient RotateSecond(Vector3 origin, float angle)
    {
        var quat = Quaternion.Euler(0, angle, 0);
        var center = origin + quat * (Center - origin);
        var rotation = quat * Rotation;
        return new Orient(center, rotation);
    }


}

class TeamCVirtualCamera : ICamera
{
    Queue<Orient[]> _sequence;
    public TeamCVirtualCamera()
    {
        var t = new[] //new array is about orient
        {

           new Orient(0.8f,0.045f,0.6f,120f),
           new Orient(0.8f,0.045f*2,0.6f,30f),
           new Orient(0.8f,0.045f*3,0.6f,60f),
        };
        _sequence = new Queue<Orient[]>(new[]
        {
           new[] {t[0]},
           new[] {t[1]}, 
           new[] {t[1]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
            new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
           new[] {t[2]},
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

        //Number of tiles. number of implementation.  
    }


    public IList<Orient> GetTiles(Rect area)
    {
        return _sequence.Dequeue();
    }
}


