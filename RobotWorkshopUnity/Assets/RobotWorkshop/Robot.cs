using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Mathf;

public interface IStackable
{
    PickAndPlaceData GetNextTargets();
    string Message { get; }
    IEnumerable<Orient> Display { get; }
}

public class PickAndPlaceData
{
    public Orient Pick { get; set; }
    public Orient Place { get; set; }
    public bool Retract { get; set; } = false;
    public bool StopLoop { get; set; } = false;
}

public enum Mode { Virtual, Live };
public enum Team { Debug, TeamA, TeamBBJT, TeamC };

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

    Material _selectedMaterial;
    PickAndPlaceData _currentData;
    bool _looping = false;
    bool _robotAwaiting = false;
    Mode _mode;
    Team _team;
    string _robotMessage = "Press connect.";

    void Initialize()
    {
        _selectedMaterial = new Material(_material)
        {
            color = Color.red
        };

        switch (_team)
        {
            case Team.Debug: { _stackable = new StackingVisionSimple(_mode); break; }
            case Team.TeamA: { _stackable = new StackingFillAndBuild(_mode); break; }
            case Team.TeamBBJT: { _stackable = new StackingTeamBBJT(_mode); break; }
            case Team.TeamC: { _stackable = new StackingTeamC(_mode); break; }
            default:
                throw new ArgumentOutOfRangeException("Team not found.");
        }
    }

    async void StartLoop()
    {
        _robotMessage = "Robot loop started.";
        _looping = true;


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
                if (_stackable == null) Initialize();

                _currentData = _stackable.GetNextTargets();

                if (_currentData == null || _currentData.StopLoop)
                {
                    StopLoop();
                    return;
                }

                _server.SendTargets(_currentData.Retract ? 2 : 1, BestGrip(_currentData.Pick), BestGrip(_currentData.Place));
                _robotAwaiting = false;
            }
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
        string ip = _mode == Mode.Virtual ? "127.0.0.1" : "192.168.0.3";
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

    void Update()
    {
        if (_stackable?.Display != null)
        {
            foreach (var orient in _stackable.Display)
                DrawTile(orient, _material);
        }

        if (_currentData != null)
        {
            DrawTile(_currentData.Pick, _selectedMaterial);
            DrawTile(_currentData.Place, _selectedMaterial);
        }
    }

    void DrawTile(Orient orient, Material material)
    {
        Graphics.DrawMesh(_tile, orient.Center, orient.Rotation, material, 0);
    }

    void OnGUI()
    {
        GUI.skin = _skin;

        GUILayout.BeginArea(new Rect(16, 16, Screen.width, Screen.height));
        GUILayout.BeginVertical();

        _team = (Team)GUILayout.SelectionGrid((int)_team, new[] { "Debug", "Team A", "Team BBJT", "Team C" }, 4);
        _mode = (Mode)GUILayout.SelectionGrid((int)_mode, new[] { "Virtual", "Live" }, 2);

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

        GUILayout.Label($"<b>Robot:</b> {_robotMessage}");
        GUILayout.Label($"<b>Stacking:</b> {_stackable?.Message}");
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}