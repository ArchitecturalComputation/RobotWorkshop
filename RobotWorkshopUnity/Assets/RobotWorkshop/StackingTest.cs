using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingTest : IStackable
{
    public string Message { get; set; }

    readonly Vector3 _pickPoint = new Vector3(0.2f, 0, 0.4f);
    // readonly Vector3 _placePoint = new Vector3(1.2f, 0, 0.4f);
    readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
    int _tileCount;
    readonly float _gap = 0.005f;

    List<Orient> _pickTiles = new List<Orient>();
    List<Orient> _placeTiles = new List<Orient>();

    List<Orient> _placeBlocks = new List<Orient>();
    int _initNum;

    public StackingTest(int _num_blocks)
    {
        _tileCount = _num_blocks;
        
    }

    public List<Orient> InitBlocks()
    {
        // Input position of starting blocks
        Orient init1 = new Orient(new Vector3(0.4f, 0.045f, 0.5f), Quaternion.Euler(0, 0, 0));
        Orient init2 = new Orient(new Vector3(0.8f, 0.045f, 0.3f), Quaternion.Euler(0, 90, 0));

        _placeBlocks.Add(init1);
        _placeBlocks.Add(init2);

        _initNum = _placeBlocks.Count;

        MakePickTower(_tileCount - _initNum);
        return _placeBlocks;
    }

    public Orient[] GetNextTargets()
    {
        // int _num_blocks_placed = _initNum + _tileCount - _pickTiles.Count;

        if (_pickTiles.Count == 0)
        {
            Message = "No tiles left.";
            return null;
        }

        Orient pick = _pickTiles.Last();

        Orient place = FarthestLocation(_placeBlocks);
        
        _pickTiles.RemoveAt(_pickTiles.Count - 1);
        _placeTiles.Add(pick);

        Message = $"Placing tile {_placeBlocks.Count} out of {_tileCount}";

        return new[] { pick, place };
    }

    // Position new block as far as possible from existing blocks
    Orient FarthestLocation(List<Orient> existing_blocks)
    {
        int table_x = 1400;         // length of table in mm
        int table_z = 800;          // width of table in mm
        int padding = 100;          // minimum distance between edge of table and center of block
        int padding_x_min = 300;    // extra padding to avoid pile of blocks
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
        float prev_block_x = existing_blocks[existing_blocks.Count - 1].Center.x;
        float prev_block_z = existing_blocks[existing_blocks.Count - 1].Center.z;
        float angle = Mathf.Rad2Deg * Mathf.Atan2(new_block_z - prev_block_z, new_block_x - prev_block_x);

        Vector3 position = new Vector3(new_block_x, new_block_y, new_block_z);
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Orient new_block = new Orient(position, rotation);
        _placeBlocks.Add(new_block);

        Debug.Log(string.Format("Block {0} location: {1:0.000}, {2:0.000}, {3:0.000}", existing_blocks.Count, new_block_x, new_block_y, new_block_z));
        return new_block;
    }

    void MakePickTower(int total_blocks)
    {
        for (int i = 0; i < total_blocks; i++)
        {
            Orient next = JengaLocation(i);
            _pickTiles.Add(new Orient(_pickPoint + next.Center, next.Rotation));
        }
    }

    Orient JengaLocation(int index)
    {
        int count = index;
        int layer = count / 3;
        int row = count % 3;
        bool isEven = layer % 2 == 0;

        Vector3 position = new Vector3(0, (layer + 1) * _tileSize.y, (row - 1) * _tileSize.z);
        Quaternion rotation = Quaternion.Euler(0, isEven ? 0 : -90, 0);
        return new Orient(rotation * position, rotation);
    }

}