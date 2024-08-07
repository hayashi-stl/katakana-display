using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Unit : MultiMeshInstance
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        string text = "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤいユえヨラリルレロワヰうヱヲン";
        GD.Print(text.Length);
        Multimesh.InstanceCount = 51;
        for (int i = 0; i < text.Length; ++i) {
            ulong field = SegmentMap.Map[$"{text[i]}"];
            Multimesh.SetInstanceCustomData(i, new Color(field % (1 << 24), field >> 24, 0.0f, 0.0f));

            Multimesh.SetInstanceTransform(i, new Transform(Basis.Identity, new Vector3((i / 5) * -40, 0, (i % 5) * 24)));
        }
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
