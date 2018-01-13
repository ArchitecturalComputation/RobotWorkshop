using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mathf;

public interface IStackable
{
    // InitBlocks sets initial blocks and instantiates GameObjects in Unity
    List<Orient> InitBlocks();
    Orient[] GetNextTargets();
    string Message { get; set; }
}

public class Robot : MonoBehaviour
{
    [SerializeField]
    Mesh _tile;
    [SerializeField]
    Material _material;
    [SerializeField]
    GUISkin _skin;

    Server _server;
    IStackable _stackable;

    Orient[] _targets;
    bool _looping = false;
    bool _robotAwaiting = false;
    int _robotMode = 1;
    string _robotMessage = "Press connect.";

    GameObject block_parent;
    public GameObject block;  //assign tile prefab in editor
    public GameObject[] block_arr;
    int num_blocks = 15;
    int init_num; // initial number of blocks, from length of List<Orient> _initBlockList

    void CreateBlock(Vector3 _pos, Quaternion _rota, int _arr_num)
    {
        GameObject go = Instantiate(block, _pos, _rota) as GameObject;
        go.transform.SetParent(block_parent.transform, false);
        go.gameObject.name = string.Format("Block{0}", _arr_num);
        block_arr[_arr_num] = go;
    }

    void initBlocks()
    {
        List<Orient> _initBlockList = _stackable.InitBlocks();
        init_num = _initBlockList.Count;
        block_arr = new GameObject[num_blocks + init_num];
        for (int i = 0; i < _initBlockList.Count; i++)
        {
            CreateBlock(_initBlockList[i].Center, _initBlockList[i].Rotation, i);
        }
    }

    void Start()
    {
        // Creates parent gameobject, and array of blocks
        block_parent = new GameObject("BlockParent");
        
        _stackable = new StackingTest(num_blocks); // Stacking program
    }

    async void StartLoop()
    {
        initBlocks();

        _robotMessage = "Robot loop started.";
        _looping = true;
        int block_count = init_num;

        try
        {
            while (_looping)
            {

                if (!_robotAwaiting)
                {
                    await Task.Run(() => _robotAwaiting = (_server.Read() == 1));
                }

                if (!_looping)
                    return;

                if (_robotAwaiting)
                {
                    _targets = _stackable.GetNextTargets();
            
                    if (_targets == null)
                    {
                        StopLoop();
                        return;
                    }
                 
                    _server.SendTargets(1, BestGrip(_targets[0]), BestGrip(_targets[1]));
                    _robotAwaiting = false;

                    CreateBlock(_targets[1].Center, _targets[1].Rotation, block_count);
                    block_count++;
                }
            }
        } 
        catch (Exception e) 
        {
            Debug.Log(String.Format("Error: '{0}'", e));
        }
    }

    void StopLoop()
    {
        _robotMessage = "Robot loop stopped.";
        _looping = false;
    }

    async void ConnectToRobot()
    {
        _robotMessage = "Waiting for robot to connect...";
        string ip = _robotMode == 0 ? "127.0.0.1" : "192.168.0.3";
        await Task.Run(() => _server = new Server(ip, 1025));

        if (_server.Connected)
            _robotMessage = "Robot connected.";
        else
        {
            _robotMessage = "Robot connection error.";
            _server.Dispose();
            _server = null;
        }
    }



    Orient BestGrip(Orient orient)
    {
        float robotPos = 0.7025f;
        var left = new Vector3(1, 0, 1);
        var right = new Vector3(1, 0, -1);
        var bestPos = orient.Center.x < robotPos ? left : right;

        var angle = Orient.GetAngle(orient.Rotation * Vector3.right, bestPos);
        if (Abs(angle) > 90f) orient.Rotation *= Quaternion.Euler(0, 180f, 0);

        return orient;
    }

    void OnApplicationQuit()
    {
        if (_server != null)
            _server.Dispose();
    }

    private void Update()
    {
        if (_targets != null)
            foreach (var target in _targets)
                Graphics.DrawMesh(_tile, target.Center, target.Rotation, _material, 0);
    }


    private void OnGUI()
    {
        GUI.skin = _skin;

        GUILayout.BeginArea(new Rect(16, 16, Screen.width - 16, 400));
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();

        _robotMode = GUILayout.SelectionGrid(_robotMode, new[] { "Simulation", "Live" }, 2);

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (_server == null)
        {
            if (GUILayout.Button("Connect"))
                ConnectToRobot();
        }
        else
        {
            if (_looping)
            {
                if (GUILayout.Button("Stop loop"))
                    StopLoop();
            }
            else
            {
                if (GUILayout.Button("Start loop"))
                    StartLoop();
            }
        }

        GUILayout.EndHorizontal();

       // GUILayout.BeginHorizontal();
        GUILayout.Label($"<b>Robot:</b> {_robotMessage}");
        GUILayout.Label($"<b>Stacking:</b> {_stackable.Message}");
        //GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}


