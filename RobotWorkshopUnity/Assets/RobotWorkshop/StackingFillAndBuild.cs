using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class StackingFillAndBuild : IStackable
{
    public string Message { get; private set; }
    public IEnumerable<Orient> Display { get{ return _display_blocks; } }

    int _bottom_layer_blocks = 12;
    int _max_total_blocks = 34;
    int target_pairs = 3;
    int _stop_after_blocks = 0;
    int _pick_area_width = 400;
    int _padding = 120;

    IList<Orient> _pick_blocks = new List<Orient>();
    IList<Orient> _placed_blocks = new List<Orient>();
    IList<Orient> _display_blocks = new List<Orient>();
    IList<Orient> _placed_bottom_layer = new List<Orient>();
    readonly Rect _pick_area;
    readonly Rect _place_area;
    readonly ICamera _camera;
    readonly Vector3 _tileSize = new Vector3(0.18f, 0.045f, 0.06f);
    bool _instantiate_block = false;
    bool _use_camera = true;
    bool _stop_loop = false;
    Block[] _block_arr;
    List<int[]> _block_pairs = new List<int[]>();
    List<Orient> _arch_orients = new List<Orient>();
    int _current_block_layer = 1;
    bool _complete = false;


    public StackingFillAndBuild(Mode mode)
    {
        float m = 0.02f;
        _pick_area = new Rect(0 + m, 0 + m, _pick_area_width * 0.001f - m, 0.8f - m);
        _place_area = new Rect(_pick_area_width * 0.001f + m, 0 + m, 1.0f - m, 0.8f - m);
        _camera = mode == Mode.Virtual ? new TeamAVirtualCamera() as ICamera : new LiveCamera() as ICamera;
    }

    bool CheckCamera(string area, IList<Orient> camera)
    {
        if (camera == null)
        {
            Message = "Camera error.";
            Debug.Log($"{area} = null");
            return false;
        }

        if (camera.Count == 0)
        {
            Message = $"No {area} blocks left.";
            return false;
        }

        return true;
    }

    public PickAndPlaceData GetNextTargets()
    {
        IList<Orient> place_top = _camera.GetTiles(_place_area);
        IList<Orient> pick_top = _camera.GetTiles(_pick_area);

        if (!CheckCamera("top", pick_top)) return null;
        if (!CheckCamera("place", place_top)) return null;

        Debug.Log($"{pick_top.Count} pick blocks, {place_top.Count} placed blocks detected");
        _pick_blocks = pick_top;

        // Debug.Log(string.Format("_placed_blocks.Count = {0}", _placed_blocks.Count));
        Orient pick = pick_top.Last();
        Orient place;

        if (_stop_loop)
        {
            _display_blocks = _placed_blocks.ToList();
            _stop_loop = false;
            _use_camera = true;
            return null;
        }

        if (_placed_blocks.Count < _bottom_layer_blocks)
        {
            _placed_bottom_layer = _placed_blocks;
            if (_use_camera == true)
            {
                Debug.Log("Place camera used");
                _placed_blocks = place_top;
                _use_camera = false;
            }
            Message = $"Placing block {_placed_blocks.Count + 1} out of {_bottom_layer_blocks} on bottom layer";
            place = FarthestLocation(_placed_blocks);
        }
        else if (_placed_blocks.Count < _max_total_blocks && _complete == false)
        {
            Message = $"Placing arch blocks {_placed_blocks.Count + 1} out of {_max_total_blocks} max";
            place = BuildArches(_placed_bottom_layer, place_top, _current_block_layer);
        }
        else
        {
            Message = "Complete!";
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

        // set _use_camera to true to use the camera every loop, otherwise leave false to run once only
        if (_placed_blocks.Count == _stop_after_blocks)
        {
            _stop_loop = true;
        }
        
        return new PickAndPlaceData { Pick = pick, Place = place, Retract = true };
    }

    // Position new block as far as possible from existing blocks
    Orient FarthestLocation(IList<Orient> existing_blocks)
    {
        int table_x = 1400;         // length of table in mm
        int table_z = 800;          // width of table in mm
        int padding_x_min = _pick_area_width + _padding;    // extra padding to avoid pile of blocks
        int padding_x_max = _padding;    // padding     
        int padding_z_min = _padding;    // padding
        int padding_z_max = _padding;    // padding
        float block_dist_min;       // minimum distance between each point on table to nearest block
        float largest_dist = 0;     // largest min distance out of all points on table
        float new_block_x = 0;      // x coordinate of new block
        float new_block_z = 0;      // z coordinate of new block
        float new_block_y = _tileSize.y + 0.002f; // y (height) of new block of bottom layer on table + tolerance

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

        // position new block and add to list
        Vector3 position = new Vector3(new_block_x, new_block_y, new_block_z);
        // select which angle to use for new block (average or previous)
        float angle = angle_prev;
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Orient new_block = new Orient(position, rotation);

        Debug.Log(string.Format("Block {0} location: {1:0.000}, {2:0.000}, {3:0.000}, {4:0}", existing_blocks.Count + 1, new_block_x, new_block_y, new_block_z, angle));
        return new_block;
    }

    class Block
    {
        int index;
        int arr_size;
        float x, z;
        public float angle;
        float[] dist_between;
        float[] vector_angle;
        public float[,] angle_diff;
        public Vector3 Center;
        public Orient Ori;
        public Block(int this_index, Orient ori, int block_count)
        {
            Ori = ori;
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
            float min_dist = 0.22f;
            int valid_count = 0;
            List<int> valid_index = new List<int>();
            for (int i = 0; i < arr_size; i++)
            {
                if (i != index)
                {
                    if ((dist_between[i] < dist_limit) && (dist_between[i] > min_dist))
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
        int num_pairs = 0;

        // store distances and angles between all blocks, gets block pairs, runs only once
        if (_instantiate_block == false) {
            _block_arr = new Block[bot_blocks.Count];

            for (int i = 0; i < bot_blocks.Count; i++)
            {
                _block_arr[i] = new Block(i, bot_blocks[i], bot_blocks.Count);
            }

            for (int i = 0; i < bot_blocks.Count; i++)
            {
                for (int j = 0; j < bot_blocks.Count; j++)
                {
                    _block_arr[i].block_compare(_block_arr[j], j);
                }
            }
            // Nested loops below could be made tidier!
            for (int t = 0; t <= 6; t++)
            {
                float dist_limit = 0.42f - (0.02f * t);
                float rota_limit = 15f + (5f * t);
                for (int i = 0; i < bot_blocks.Count; i++)
                {
                    var new_pair = _block_arr[i].get_block_pair(dist_limit, rota_limit);
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
            
            for (int i = 0; i < _block_pairs.Count; i++)
            {
                Orient[] arch = ArchPositions(_block_pairs[i][0], _block_pairs[i][1]);
                for (int j = 0; j < arch.Length; j++)
                {
                    _arch_orients.Add(arch[j]);
                }
                // _arch_orients.Concat(ArchPositions(_block_pairs[i][0], _block_pairs[i][1]));
            }
        }

        Vector3 position;
        Quaternion rotation;
        Orient new_block;

        if (_arch_orients.Count > 0)
        {
            int blocks_in_layer = 0;
            for (int i = 0; i < _arch_orients.Count; i++)
            {
                if (_arch_orients[i].Center.y < ((_current_block_layer + 1) * _tileSize.y) + 0.01f)
                {
                    blocks_in_layer++;
                }
            }
            if (blocks_in_layer == 0) 
            {
                _current_block_layer++;
                Debug.Log($"Moving to layer {_current_block_layer}");
            }
            for (int i = 0; i < _arch_orients.Count; i++)
            {
                if (_arch_orients[i].Center.y < ((_current_block_layer + 1) * _tileSize.y) + 0.01f)
                {
                    position = _arch_orients[i].Center;
                    rotation = _arch_orients[i].Rotation;
                    new_block = new Orient(position, rotation);
                    _arch_orients.RemoveAt(i);
                    Debug.Log(string.Format("Block {0} location: {1:0.000}, {2:0.000}, {3:0.000}, {4:0}", _placed_blocks.Count + 1, position.x, position.y, position.z, rotation.eulerAngles.y));
                    
                    return new_block;
                }
            }
        }
        else if (_arch_orients.Count == 0)
        {
            // ends program on next loop
            _complete = true;
            Debug.Log("Arches Completed!");
        }
        // places block in corner if no valid position found
        position = new Vector3(_tileSize.x/2, _tileSize.y, _tileSize.z/2);
        rotation = Quaternion.Euler(0, 0, 0);
        new_block = new Orient(position, rotation);
        return new_block;
    }

    //function for finding the arch positions and rotations
    public Orient[] ArchPositions(int ABlock, int BBlock)
    {
        Block A = _block_arr[ABlock];
        Block B = _block_arr[BBlock];

        Vector3 AB = B.Center - A.Center;
        float ABdist = AB.magnitude;
        Debug.Log($"dist = {ABdist}");

        Vector3 pos;
        float rot;
        float angle_diff;

        int num;
        Orient[] arch;
        float[] placements;
        if (ABdist >= 0.22f && ABdist < 0.25f)
        {
            num = 5;
            placements = new[] { 0.01f, 0.05f, 0.5f, 0.95f, 0.99f };
            // ideally (distance - 200)/10 , (distance - 200)/2
        }
        else if (ABdist >= 0.25f && ABdist < 0.30f)
        {
            num = 5;
            placements = new[] { 0.02f, 0.1f, 0.5f, 0.9f, 0.98f };
        }
        else if (ABdist >= 0.30f && ABdist < 0.35f)
        {
            num = 5;
            placements = new[] { 0.05f, 0.2f, 0.5f, 0.8f, 0.95f };
        }
        else if (ABdist >= 0.35f && ABdist < 0.45f)
        {
            num = 7;
            placements = new[] { 0.03f, 0.11f, 0.27f, 0.5f, 0.73f, 0.89f, 0.97f };
        }
        else
        {
            Debug.Log("arch not valid");
            return null;
        }

        arch = new Orient[num];

        for (int i = 0; i < num; i++)
        {
            
            int a_layer = i + 1;
            pos = A.Center + placements[i] * AB;
            angle_diff = (B.angle - A.angle + 720) % 180;
            if (angle_diff > 90f)
            {
                angle_diff = 180f - angle_diff;
            }
            if ( Mathf.Abs((A.angle + angle_diff - B.angle) % 180) > Mathf.Abs((B.angle + angle_diff - A.angle) % 180) )
            {
                angle_diff = -angle_diff;
            }
            rot = A.angle + angle_diff * (i + 1) / num;
            if (a_layer > (num + 1)/2 )
            {
                a_layer = num - a_layer + 1;
            }
            pos.y = _tileSize.y * (a_layer + 1) + 0.002f;
            arch[i] = new Orient(pos.x, pos.y, pos.z, rot);
            Debug.Log($"i = {i}, x = {pos.x}, y = {pos.y}, z = {pos.z}, angle = {rot}");
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
                // new Orient(1.0f, 0.045f, 0.3f, 0),
                new Orient(1.2f, 0.045f, 0.6f, 90.0f),
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