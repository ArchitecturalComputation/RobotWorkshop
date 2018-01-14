using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingFillAndBuild : IStackable
{
    public string Message { get; set; }

    int _tileCount = 15;

    IList<Orient> _pick_blocks = new List<Orient>();
    IList<Orient> _placed_blocks = new List<Orient>();
    readonly Rect _pick_area;
    readonly Rect _place_area;
    readonly ICamera _camera;

    public StackingFillAndBuild(ICamera camera)
    {
        float m = 0.02f;
        _pick_area = new Rect(0 + m, 0 + m, 0.4f - 2*m, 0.8f - 2*m);
        _place_area = new Rect(0.4f + m, 0 + m, 1.0f - 2*m, 0.8f - 2*m);
        _camera = camera;
    }

    public Orient[] GetNextTargets()
    {
        var pick_top = _camera.GetTiles(_pick_area);
        var place_top = _camera.GetTiles(_place_area);

        _placed_blocks = place_top;
        // Debug.Log(string.Format("_placed_blocks.Count = {0}", _placed_blocks.Count));

        if (_placed_blocks.Count < _tileCount)
        {
            _pick_blocks = pick_top;
            _placed_blocks = place_top;
        } else {
            Message = "tileCount exceeded";
            return null;
        }

        if (pick_top == null)
        {
            Message = "Camera error.";
            return null;
        }

        if (_pick_blocks.Count == 0)
        {
            Message = "No tiles left.";
            return null;
        }
        
        var pick = pick_top.Last();
        var place = FarthestLocation(place_top);

        var targets_w_placed_arr = new[] { pick, place }.Concat(_placed_blocks).ToArray();
        _placed_blocks.Add(place);
        _pick_blocks.Remove(pick);

        Message = $"Placing tile {_placed_blocks.Count} out of {_tileCount}";

        return targets_w_placed_arr;
    }

    // Position new block as far as possible from existing blocks
    Orient FarthestLocation(IList<Orient> existing_blocks)
    {
        int table_x = 1400;         // length of table in mm
        int table_z = 800;          // width of table in mm
        int padding = 100;          // minimum distance between edge of table and center of block
        int padding_x_min = 400;    // extra padding to avoid pile of blocks
        float block_dist_min;       // minimum distance between each point on table to nearest block
        float largest_dist = 0;     // largest min distance out of all points on table
        float new_block_x = 0;      
        float new_block_z = 0;
        float new_block_y = 0.045f; // y (height) of new block of bottom layer on table

        for (int x = padding_x_min; x < table_x - padding; x++)
        {
            for (int z = padding; z < table_z - padding; z++)
            {
                block_dist_min = table_x + table_z; // distance has to be less than length + width of table
                foreach (var each_block in existing_blocks)
                {
                    // convert distance to meters
                    float dx = (x * 0.001f) - each_block.Center.x;
                    float dz = (z * 0.001f) - each_block.Center.z;
                    float dist = Mathf.Sqrt(dx*dx + dz*dz);
                    // get minimum distance to any block on table
                    if (dist < block_dist_min)
                    {
                        block_dist_min = dist;
                    }
                }
                // store largest minimum distance
                if (block_dist_min > largest_dist)
                {
                    largest_dist = block_dist_min;
                    new_block_x = x * 0.001f;
                    new_block_z = z * 0.001f;
                }
            }
        }
        // get angle to point at previous block
        float prev_block_x = existing_blocks[existing_blocks.Count - 1].Center.x;
        float prev_block_z = existing_blocks[existing_blocks.Count - 1].Center.z;
        float angle = Mathf.Rad2Deg * Mathf.Atan2(new_block_z - prev_block_z, new_block_x - prev_block_x);
        // position new block and add to list
        Vector3 position = new Vector3(new_block_x, new_block_y, new_block_z);
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Orient new_block = new Orient(position, rotation);

        Debug.Log(string.Format("Block {0} location: {1:0.000}, {2:0.000}, {3:0.000}", existing_blocks.Count + 1, new_block_x, new_block_y, new_block_z));
        return new_block;
    }
}