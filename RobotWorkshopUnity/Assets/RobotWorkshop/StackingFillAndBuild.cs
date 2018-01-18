using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class StackingFillAndBuild : IStackable
{
    public string Message { get; private set; }
    public IEnumerable<Orient> Display { get{ return _display_blocks; } }

    int _bottom_layer_blocks = 13;
    int _max_total_blocks = 20;
    int _current_block_layer = 1;

    IList<Orient> _pick_blocks = new List<Orient>();
    IList<Orient> _placed_blocks = new List<Orient>();
    IList<Orient> _display_blocks = new List<Orient>();
    IList<Orient> _placed_bottom_layer = new List<Orient>();
    readonly Rect _pick_area;
    readonly Rect _place_area;
    readonly ICamera _camera;
    bool _instantiate_block = false;
    bool _camera_used = false;
    Block[] block_arr;
    List<int[]> _block_pairs = new List<int[]>();

    public StackingFillAndBuild(Mode mode)
    {

        float m = 0.02f;
        _pick_area = new Rect(0 + m, 0 + m, 0.4f - m, 0.8f - m);
        _place_area = new Rect(0.4f + m, 0 + m, 1.0f - m, 0.8f - m);
        _camera = mode == Mode.Virtual ? new TeamAVirtualCamera() as ICamera : new LiveCamera() as ICamera;
    }

    public PickAndPlaceData GetNextTargets()
    {
        IList<Orient> pick_top = _camera.GetTiles(_pick_area);
        IList<Orient> place_top = _camera.GetTiles(_place_area);
        Debug.Log($"{pick_top.Count} pick blocks, {place_top.Count} placed blocks detected");
        _pick_blocks = pick_top;

        if (pick_top == null)
        {
            Message = "Camera error.";
            Debug.Log("pick_top = null");
            return null;
        }

        if (_pick_blocks.Count == 0)
        {
            Message = "No pick blocks left.";
            return null;
        }

        // Debug.Log(string.Format("_placed_blocks.Count = {0}", _placed_blocks.Count));
        Orient pick = pick_top.Last();
        Orient place;

        if (_placed_blocks.Count < _bottom_layer_blocks)
        {
            _placed_bottom_layer = place_top;
            if (_camera_used == false)
            {
                Debug.Log("Place camera used");
                _placed_blocks = place_top;
                _camera_used = true;
            }
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

        if (_placed_blocks.Contains(place))
        {
            Message = "Block already exists, clash detected";
            return null;
        }
        // display_blocks for Unity
        _display_blocks = _placed_blocks.ToList();
        _placed_blocks.Add(place);
        _pick_blocks.Remove(pick);

        // set _camera_used to false to use the camera every loop, otherwise leave true to run once only
        _camera_used = false;
        return new PickAndPlaceData { Pick = pick, Place = place, Retract = true };
    }

    class Block
    {
        int index;
        int arr_size;
        float x, z;
        public float angle;
        float[] dist_between;
        float[] vector_angle;
        float[,] angle_diff;
        public Vector3 Center;
        public Block(int this_index, Orient ori, int block_count)
        {
            Center = ori.Center;
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
        public List<int[]> get_block_pair(float dist_limit, float angle_limit)
        {
            int valid_count = 0;
            List<int> valid_index = new List<int>();
            for (int i = 0; i < arr_size; i++)
            {
                if (i != index)
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
            }

            if (valid_count > 0)
            {
                Debug.Log($"Block {index} - There are {valid_count} pairs of block pairs within dist {dist_limit}, angle {angle_limit}");

                List<int[]> valid_block_pair = new List<int[]>();
                for (int i = 0; i < valid_index.Count; i++)
                {
                    int[] i_pair = {index, valid_index[i]};
                    valid_block_pair.Add(i_pair);
                    Debug.Log($"new pair {index}, {valid_index[i]}");
                }
                return valid_block_pair;
            }
            return null;
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
        float new_block_x = 0;
        float new_block_z = 0;
        float new_block_y = 0.045f * current_block_layer + 0.002f;
        float angle = 0;
        int num_pairs = 0;
        int target_pairs = 4;

        // store distances and angles between all blocks, gets block pairs, runs only once
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
            // Nested loops below could be made tidier!
            for (int t = 0; t <= 5; t++)
            {
                float dist_limit = 0.4f - (0.00f * t);
                float rota_limit = 10f + (5f * t);
                for (int i = 0; i < bot_blocks.Count; i++)
                {
                    var new_pair = block_arr[i].get_block_pair(dist_limit, rota_limit);
                    if (new_pair != null)
                    {
                        for (int j = 0; j < new_pair.Count; j++)
                        {
                            if (_block_pairs.Count == 0)
                            {
                                _block_pairs.Add(new_pair[j]);
                                num_pairs++;
                                Debug.Log($"Add first pair {new_pair[j][0]}, {new_pair[j][1]}");
                            }
                            else
                            {
                                bool in_block_pairs = false;
                                // adds new pair if neither block is already in _block_pair
                                for (int k = 0; k < _block_pairs.Count; k++)
                                {
                                    if ( (_block_pairs[k][0] == new_pair[j][0]) || (_block_pairs[k][0] == new_pair[j][1]) 
                                    || (_block_pairs[k][1] == new_pair[j][0]) || (_block_pairs[k][1] == new_pair[j][1]) )
                                    {
                                        in_block_pairs = true;
                                    }
                                }
                                if (!in_block_pairs)
                                {
                                    _block_pairs.Add(new_pair[j]);
                                    num_pairs++;
                                    Debug.Log($"Add {num_pairs} pair {new_pair[j][0]}, {new_pair[j][1]}");                                        
                                }
                            }
                            if (num_pairs >= target_pairs) 
                            {
                                Debug.Log($"Found {target_pairs} pairs!");
                                goto found_arches;
                            }
                        }

                    }
                }

            } found_arches: ;
            // writes all pairs in list to console
            _block_pairs.ForEach(i => Debug.Log($"in _block_pair: {i[0]}, {i[1]}"));

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
        float new_block_y = 0.045f + 0.002f; // y (height) of new block of bottom layer on table + tolerance

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

     //function for finding the arch positions and rotations
    public Orient[] ArchPositions(int ABlock, int BBlock)
    {
        Block A = block_arr[ABlock];
        Block B = block_arr[BBlock];

        Vector3 AB = A.Center - B.Center;
        float ABdist = AB.magnitude;

        int num;
        Orient[] arch;
        float[] placements;

        if (ABdist > 350 && ABdist < 400)
        {
            num = 5;
            placements = new[] { 0.05f, 0.2f, 0.5f, 0.8f, 0.95f };
        }
        else if (ABdist > 400 && ABdist < 450)
        {
            num = 7;
            placements = new[] { 0.03f, 0.11f, 0.27f, 0.5f, 0.73f, 0.89f, 0.97f };
        }
        else
        {
            return null;
        }

        arch = new Orient[num];

        for (int i = 0; i < num; i++)
        {
            Vector3 pos;
            float rot;
            pos = A.Center + placements[i] * AB;
            rot = ((num - i) / (num + 1)) * A.angle + ((i + 1) / (num + 1)) * B.angle;
            arch[i] = new Orient(pos.x, pos.y, pos.z, rot);
        }

        return arch;
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