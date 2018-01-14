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
    readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
    readonly float _tile_gap = 0.01f;
    int _current_pos;
    int _current_layer;
    int _tower_length;
    int _tower_height;
    List<Orient> _pick_tower = new List<Orient>();
    IList<Orient> _placed_blocks = new List<Orient>();

    public VirtualCamera()
    {
        var init_blocks = new[]
        {
           new Orient(0.4f, 0.045f, 0.5f, 0),
           new Orient(0.8f, 0.045f, 0.3f, 90.0f),
        };
        // create virtual pick tower
        _pick_tower = CreatePickTower();
        // create virtual initial placed blocks
        foreach (var block in init_blocks)
        {
            _placed_blocks.Add(block);
        }
    }

    public IList<Orient> GetTiles(Rect area)
    {
        // set border between pick and place area
        float pick_place_border_x = 0.4f;

        if (area.x < pick_place_border_x)
        {
            // in pick area
            var _pick_top_layer = _pick_tower.GetRange(((_current_layer -1) * _tower_length) -1, _current_pos);
            // Debug.Log(string.Format("number of blocks in top pick layer = {0}", _current_pos));
            _pick_tower.RemoveAt(_pick_tower.Count - 1);
            _current_pos -= 1;
            if (_current_pos == 0)
            {
                _current_layer -= 1;
                _current_pos = _tower_length;
                if (_current_layer < 0)
                {
                    Debug.Log("No blocks left");
                    return null;
                }
            }
            return _pick_top_layer;
        } else {
            // in place area
            return _placed_blocks;
        }
    }

    List<Orient> CreatePickTower()
    {
        // make blocks in virtual pick tower, stacked as length x height
        _tower_length = 10;
        _tower_height = 5;
        for (int i = 0; i < _tower_height; i++)
        {
            for (int j = 0; j < _tower_length; j++)
            {
                float z = 0.05f + j * (_tileSize.z + _tile_gap);
                float y = (i+1) * _tileSize.y;
                Orient pick_block = new Orient(0.1f, y, z, 0);
                _pick_tower.Add(pick_block);
            }
        }
        _current_pos = _tower_length;
        _current_layer = _tower_height;
        return _pick_tower;
    }
}
