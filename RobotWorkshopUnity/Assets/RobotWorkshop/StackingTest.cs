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
    // Orient[] _existing_blocks;
    // List<Orient> _initBlocks = new List<Orient>();
    int _initNum;

    public StackingTest(int _num_blocks)
    {
        _tileCount = _num_blocks;

        MakePickTower();
    }

    public List<Orient> InitBlocks()
    {
        // Input position of starting blocks
        Orient init1 = new Orient(new Vector3(0.4f, 0.045f, 0.5f), Quaternion.Euler(0, 0, 0));
        Orient init2 = new Orient(new Vector3(0.8f, 0.045f, 0.3f), Quaternion.Euler(0, 90, 0));

        _placeBlocks.Add(init1);
        _placeBlocks.Add(init2);

        _initNum = _placeBlocks.Count;
        return _placeBlocks;
    }

    public Orient[] GetNextTargets()
    {
        int _num_blocks_placed = _initNum + _tileCount - _pickTiles.Count;

        if (_pickTiles.Count == 0)
        {
            Message = "No tiles left.";
            return null;
        }

        Orient pick = _pickTiles.Last();

        Orient place = FarthestLocation(_num_blocks_placed + 1, _placeBlocks);
        
        _pickTiles.RemoveAt(_pickTiles.Count - 1);
        _placeTiles.Add(pick);

        Message = $"Placing tile {_placeTiles.Count} out of {_tileCount}";

        return new[] { pick, place };
    }

    Orient FarthestLocation(int index, List<Orient> existing_blocks)
    {
        Vector3 position = new Vector3(0, 0, 0);
        Quaternion rotation = Quaternion.Euler(0, 0, 0);
        return new Orient(position, rotation);
    }

    void MakePickTower()
    {
        for (int i = 0; i < _tileCount; i++)
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