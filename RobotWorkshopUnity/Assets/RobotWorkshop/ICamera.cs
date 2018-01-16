using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public interface ICamera
{
    IList<Orient> GetTiles(Rect area);
}

class LiveCamera : ICamera
{
    public IList<Orient> GetTiles(Rect place_area)
    {
        return Motive.GetTiles(place_area);
    }
}

class VirtualCamera : ICamera
{
    Queue<Orient[]> _sequence;

    public VirtualCamera()
    {
        var t = new[]
        {
           new Orient(0.1f,0.045f,0.3f,90.0f),
           new Orient(0.2f,0.045f,0.1f,30.0f),
           new Orient(0.4f,0.045f,0.3f,45.0f)
        };

        _sequence = new Queue<Orient[]>(new[]
        {
           new[] {t[0],t[1],t[2]},
           new[] {t[1],t[2]},
           new[] {t[2]},
           new Orient[0]
        });
    }

    public IList<Orient> GetTiles(Rect area)
    {
        return _sequence.Dequeue();
    }
}

