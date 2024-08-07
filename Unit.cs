using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Unit : MultiMeshInstance
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    [Export]
    public string Text { get; set; }
    [Export]
    public int Height { get; set; }
    [Export]
    public int Width { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //string text = "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤいユえヨラリルレロワヰうヱヲン" +
        //    "゛゜ー＝ァィゥェォャュョヮッ";
        Multimesh = (MultiMesh)Multimesh.Duplicate();
        Multimesh.InstanceCount = Height * Width;
        for (int i = 0; i < Height * Width; ++i) {
            Multimesh.SetInstanceTransform(i, new Transform(Basis.Identity, new Vector3((i / Height) * -40, 0, (i % Height) * 24)));
        }

        DisplayText();
    }

    // Normalize the text for display. This means:
    // • Everything gets zenkaku'd
    // • All dakuten and handakuten are separated
    // • Characters without dakuten/handakuten get a space after them
    // • Characters without sutegana get a space after them.
    // • 3 characters per display unit (main, dakuten, sutegana)
    static string NormalizeDisplayText(string text) {
        // Zenkaku + separated (han)dakuten
        text = string.Join("", text.ToCharArray().Select(c => Util.NormalizationMap.ContainsKey($"{c}") ? Util.NormalizationMap[$"{c}"] : $"{c}"));
        // Spaces
        var result = new StringBuilder();
        int iter = 0;
        int mode = 0;
        GD.Print(text);
        while (true) {
            var str = iter < text.Length ? $"{text[iter]}" : "";
            if (Util.Sutegana.Contains(str) ? mode == 2 : Util.Dakuten.Contains(str) ? mode == 1 : mode == 0) {
                result.Append(str);
                ++iter;
            } else {
                result.Append("　");
            }
            mode = (mode + 1) % 3;
            if (iter == text.Length && mode == 0)
                break;
        }
        GD.Print(result.ToString());
        return result.ToString();
    }

    void DisplayText() {
        var displayText = NormalizeDisplayText(Text);

        for (int i = 0; i < Math.Min(displayText.Length / 3, Height * Width); ++i) {
            ulong field =
                Util.SegmentMap[$"{displayText[3 * i + 0]}"] |
                Util.SegmentMap[$"{displayText[3 * i + 1]}"] |
                Util.SegmentMap[$"{displayText[3 * i + 2]}"];
            Multimesh.SetInstanceCustomData(i, new Color(field % (1 << 24), field >> 24, 0.0f, 0.0f));
        }
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
