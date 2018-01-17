using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingFillAndBuild : IStackable
{
    public string Message { get; set; }

    int _bottom_layer_blocks = 10;
    int _max_total_blocks = 20;
    int _current_block_layer = 1;

    IList<Orient> _pick_blocks = new List<Orient>();
    IList<Orient> _placed_blocks = new List<Orient>();
    IList<Orient> _placed_bottom_layer = new List<Orient>();
    // IList<Orient> _placed_top_layer = new List<Orient>();
    readonly Rect _pick_area;
    readonly Rect _place_area;
    readonly ICamera _camera;
    bool _instantiate_block = false;
    Block[] block_arr;

    public StackingFillAndBuild(Mode mode)
    {

        float m = 0.02f;
        _pick_area = new Rect(0 + m, 0 + m, 0.4f - 2 * m, 0.8f - 2 * m);
        _place_area = new Rect(0.4f + m, 0 + m, 1.0f - 2 * m, 0.8f - 2 * m);
        _camera = mode == Mode.Virtual ? new TeamAVirtualCamera() as ICamera : new LiveCamera() as ICamera;
    }

    public Orient[] GetNextTargets()
    {
        IList<Orient> pick_top = _camera.GetTiles(_pick_area);
        IList<Orient> place_top = _camera.GetTiles(_place_area);
        _pick_blocks = pick_top;

        // Debug.Log(string.Format("_placed_blocks.Count = {0}", _placed_blocks.Count));
        Orient pick = pick_top.Last();
        Orient place;

        if (_placed_blocks.Count < _bottom_layer_blocks)
        {
            _placed_bottom_layer = place_top;
            _placed_blocks = place_top;
            Message = $"Placing block {_placed_blocks.Count + 1} out of {_bottom_layer_blocks} on bottom layer";
            place = FarthestLocation(_placed_blocks);
        }
        else if (_placed_blocks.Count < _max_total_blocks)
        {
            Message = $"Placing arch blocks {_placed_blocks.Count + 1} out of {_max_total_blocks} total";
            place = BuildArches(_placed_bottom_layer, place_top, _current_block_layer);
        }
        else
        {
            Message = "Max blocks exceeded";
            Debug.Log("_place_blocks.Count >= _max_total_blocks");
            return null;
        }

        if (pick_top == null)
        {
            Message = "Camera error.";
            Debug.Log("pick_top = null");
            return null;
        }

        if (_pick_blocks.Count == 0)
        {
            Message = "No tiles left.";
            Debug.Log("_pick_blocks.Count == 0");
            return null;
        }

        Orient[] targets_w_placed_arr = new[] { pick, place }.Concat(_placed_blocks).ToArray();
        _placed_blocks.Add(place);
        _pick_blocks.Remove(pick);

        return targets_w_placed_arr;
    }

    class Block
    {
        int index;
        int arr_size;
        float x, z, angle;
        float[] dist_between;
        float[] vector_angle;
        float[,] angle_diff;
        public Block(int this_index, Orient ori, int block_count)
        {
            index = this_index;
            x = ori.Center.x;
            z = ori.Center.z;
            angle = ori.Rotation.eulerAngles.y;
            arr_size = block_count;
            dist_between = new float[arr_size];
            vector_angle = new float[arr_size];
            angle_diff = new float[arr_size, 2];
            // initialize arrays
            for (int i = 0; i < arr_size; i++)
            {
                dist_between[i] = 0;
                vector_angle[i] = 360;
                angle_diff[i, 0] = 90;
                angle_diff[i, 1] = 90;
            }
        }

        // compares all other Blocks to this Block, and returns Block pairs with dist and angle below limit
        public int[,] get_block_pair(float dist_limit, float angle_limit)
        {
            int valid_count = 0;
            List<int> valid_index = new List<int>();
            for (int i = 0; i < arr_size; i++)
            {
                if (dist_between[i] < dist_limit)
                {
                    if (angle_diff[i,0] < angle_limit && angle_diff[i,1] < angle_limit)
                    {
                        valid_count++;
                        valid_index.Add(i);
                    }
                }
            }
            Debug.Log("valid_count");

            int[,] valid_block_pair = new int[valid_count, 2];
            for (int i = 0; i < valid_index.Count; i++)
            {
                valid_block_pair[i, 0] = this.index;
                valid_block_pair[i, 1] = valid_index[i];
                Debug.Log("valid_index[i]");
            }
            return valid_block_pair;
        }

        public void block_compare(Block other_block, int other_index)
        {
            float d_x = this.x - other_block.x;
            float d_z = this.z - other_block.z;
            dist_between[other_index] = Mathf.Sqrt(d_x * d_x + d_z * d_z);
            vector_angle[other_index] = -Mathf.Rad2Deg * Mathf.Atan2(d_z, d_x);
            // Work out difference in angles between blocks, wrap around 180
            angle_diff[other_index, 0] = (this.angle - vector_angle[other_index] + 360) % 180;
            if (angle_diff[other_index, 0] > 90)
            {
                angle_diff[other_index, 0] = 180 - angle_diff[other_index, 0];
            }
            angle_diff[other_index, 1] = (other_block.angle - vector_angle[other_index] + 360) % 180;
            if (angle_diff[other_index, 1] > 90)
            {
                angle_diff[other_index, 1] = 180 - angle_diff[other_index, 1];
            }
        }
    }

    Orient BuildArches(IList<Orient> bot_blocks, IList<Orient> top_blocks, int current_block_layer)
    {
        float new_block_x = 0;                              // x coordinate of new block
        float new_block_z = 0;                              // z coordinate of new block
        float new_block_y = 0.045f * current_block_layer + 0.02f;   // y (height) of new block + tolerance
        float angle = 0;

        // store distances and angles between all blocks, runs only once
        if (_instantiate_block == false) {
            block_arr = new Block[bot_blocks.Count];

            for (int i = 0; i < bot_blocks.Count; i++)
            {
                block_arr[i] = new Block(i, bot_blocks[i], bot_blocks.Count);
            }

            for (int i = 0; i < bot_blocks.Count; i++)
            {
                for (int j = 0; j < bot_blocks.Count; j++)
                {
                    block_arr[i].block_compare(block_arr[j], j);
                }
            }

            for (int i = 0; i < bot_blocks.Count; i++)
            {
                block_arr[i].get_block_pair(0.4f, 10.0f);
            }

            _instantiate_block = true;
        }

        Vector3 position = new Vector3(new_block_x, new_block_y, new_block_z);
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Orient new_block = new Orient(position, rotation);

        Debug.Log(string.Format("Block {0} location: {1:0.000}, {2:0.000}, {3:0.000}", top_blocks.Count + 1, new_block_x, new_block_y, new_block_z));
        return new_block;
    }

    // Position new block as far as possible from existing blocks
    Orient FarthestLocation(IList<Orient> existing_blocks)
    {
        int table_x = 1400;         // length of table in mm
        int table_z = 800;          // width of table in mm
        int padding_x_min = 400;    // extra padding to avoid pile of blocks
        int padding_x_max = 100;    // padding     
        int padding_z_min = 100;    // padding
        int padding_z_max = 100;    // padding
        float block_dist_min;       // minimum distance between each point on table to nearest block
        float largest_dist = 0;     // largest min distance out of all points on table
        float new_block_x = 0;      // x coordinate of new block
        float new_block_z = 0;      // z coordinate of new block
        float new_block_y = 0.045f + 0.02f; // y (height) of new block of bottom layer on table + tolerance

        for (int x = padding_x_min; x < table_x - padding_x_max; x++)
        {
            for (int z = padding_z_min; z < table_z - padding_z_max; z++)
            {
                block_dist_min = table_x + table_z; // distance has to be less than length + width of table
                foreach (var each_block in existing_blocks)
                {
                    // convert distance to meters
                    float dx = (x * 0.001f) - each_block.Center.x;
                    float dz = (z * 0.001f) - each_block.Center.z;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
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
        // (a) get angle to point at previous block
        float prev_block_x = existing_blocks[existing_blocks.Count - 1].Center.x;
        float prev_block_z = existing_blocks[existing_blocks.Count - 1].Center.z;
        float angle_prev = -Mathf.Rad2Deg * Mathf.Atan2(new_block_z - prev_block_z, new_block_x - prev_block_x);
        // Debug.Log(string.Format("dx = {0}, dz = {1}, angle = {2}", new_block_x - prev_block_x, new_block_z - prev_block_z, angle_prev));

        // (b) get angle to average of previous blocks 
        // int prev_num = 2;
        // float sum_block_x = 0, sum_block_z = 0;
        // for (int i = 0; i < prev_num; i++)
        // {
        //     int arr_num = Mathf.Clamp(existing_blocks.Count -1 - i, 0, existing_blocks.Count -1);
        //     sum_block_x += existing_blocks[arr_num].Center.x;
        //     sum_block_z += existing_blocks[arr_num].Center.z;
        // }
        // float av_block_x = sum_block_x / prev_num;
        // float av_block_z = sum_block_z / prev_num;
        // float angle_av = -Mathf.Rad2Deg * Mathf.Atan2(new_block_z - av_block_z, new_block_x - av_block_x);
        // Debug.Log(string.Format("av x = {0}, av z = {1}, angle = {2}", av_block_x, av_block_z, angle_av));

        // position new block and add to list
        Vector3 position = new Vector3(new_block_x, new_block_y, new_block_z);
        // select which angle to use for new block (average or previous)
        float angle = angle_prev;
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Orient new_block = new Orient(position, rotation);

        Debug.Log(string.Format("Block {0} location: {1:0.000}, {2:0.000}, {3:0.000}, {4:00}", existing_blocks.Count + 1, new_block_x, new_block_y, new_block_z, angle));
        return new_block;
    }

    class TeamAVirtualCamera : ICamera
    {
        readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
        readonly float _tile_gap = 0.01f;
        int _current_pos;
        int _current_layer;
        int _tower_length;
        int _tower_height;
        List<Orient> _pick_tower = new List<Orient>();
        IList<Orient> _placed_blocks = new List<Orient>();

        public TeamAVirtualCamera()
        {
            var init_blocks = new[]
            {
                new Orient(1.0f, 0.045f, 0.3f, 0),
                // new Orient(0.8f, 0.045f, 0.3f, 90.0f),
            };
            // create virtual pick tower
            _pick_tower = CreatePickTower();
            // create virtual initial placed blocks
            foreach (var block in init_blocks)
            {
                _placed_blocks.Add(block);
                Debug.Log(string.Format("Initial block location: {0:0.000}, {1:0.000}, {2:0.000}, {3:00}", block.Center.x, block.Center.y, block.Center.z, block.Rotation.eulerAngles.y));
            }
        }

        public IList<Orient> GetTiles(Rect area)
        {
            // set border between pick and place area
            float pick_place_border_x = 0.4f;

            if (area.x < pick_place_border_x)
            {
                // in pick area
                var _pick_top_layer = _pick_tower.GetRange(((_current_layer - 1) * _tower_length) - 1, _current_pos);
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
            }
            else
            {
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
                    float y = (i + 1) * _tileSize.y;
                    Orient pick_block = new Orient(0.1f, y, z, 0);
                    _pick_tower.Add(pick_block);
                }
            }
            _current_pos = _tower_length;
            _current_layer = _tower_height;
            return _pick_tower;
        }
    }
}