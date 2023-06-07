using System;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using static Planet;

[CustomEditor(typeof(Planet))]
class planetEditor : Editor {
    Planet _target;
        
    void OnEnable()
    {
        _target = (Planet)target; 
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if(GUILayout.Button("Generate")){
            _target.Generate();
        }
        if(GUILayout.Button("Randomize")){
            _target.Randomize();
        }
        if(GUILayout.Button("Update Colors")){
            _target.UpdateColors();
        }
        if(GUILayout.Button("Clear")){
            _target.clear();
        }
    }
}

