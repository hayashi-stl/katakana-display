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
    [Export]
    public bool Editable { get; set; }

    List<string> _editTextAbove;
    List<string> _editTextBelow;
    int CursorPos => _editTextAbove.Count;
    int EditTextLength => _editTextAbove.Count + _editTextBelow.Count;
    float _cursorTime;
    const float CursorPeriod = 1.0f;


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

        _editTextAbove = NormalizeDisplayText(Text);
        _editTextBelow = new List<string>();
        DisplayText();
    }

    // Normalize the text for display. This means:
    // • Everything gets zenkaku'd
    // • All dakuten and handakuten are separated
    // • Characters without dakuten/handakuten get a space after them
    // • Characters without sutegana get a space after them.
    // • 3 characters per display unit (main, dakuten, sutegana)
    static List<string> NormalizeDisplayText(string text) {
        // Zenkaku + separated (han)dakuten
        text = string.Join("", Util.Chars(text).Select(c => Util.NormalizationMap.ContainsKey(c) ? Util.NormalizationMap[c] : c));
        // Spaces
        var result = new List<string>();
        int iter = 0;
        int mode = 0;
        
        var chars = Util.Chars(text);
        while (true) {
            var str = iter < chars.Count ? chars[iter] : "";
            if (Util.Sutegana.Contains(str) ? mode == 2 : Util.Dakuten.Contains(str) ? mode == 1 : mode == 0) {
                result.Add(str);
                ++iter;
            } else {
                result.Add("　");
            }
            mode = (mode + 1) % 3;
            if (iter == chars.Count && mode == 0)
                break;
        }
        return result;
    }

    void DisplayText() {
        var editText = _editTextAbove.Concat(_editTextBelow.Reverse<string>()).ToList();
        for (int i = 0; i < Height * Width; ++i) {
            ulong field = 3 * i >= editText.Count ? 0uL :
                Util.SegmentMap[$"{editText[3 * i + 0]}"] |
                Util.SegmentMap[$"{editText[3 * i + 1]}"] |
                Util.SegmentMap[$"{editText[3 * i + 2]}"];
            
            if (Editable && 3 * i == _editTextAbove.Count && _cursorTime < 0.5)
                field ^= Util.CursorBitfield;
            Multimesh.SetInstanceCustomData(i, new Color(field % (1 << 24), field >> 24, 0.0f, 0.0f));
        }
    }

    static readonly Dictionary<KeyList, (int X, int Y)> DirectionDistances = new Dictionary<KeyList, (int, int)>() {
        { KeyList.Up, (0, -1) },
        { KeyList.Down, (0, 1) },
        { KeyList.Left, (1, 0) },
        { KeyList.Right, (-1, 0) },
    };

    void MoveCursorForward() {
        _editTextAbove.Add(_editTextBelow[_editTextBelow.Count - 1]);
        _editTextBelow.RemoveAt(_editTextBelow.Count - 1);
        _cursorTime = 0;
    }

    void MoveCursorBack() {
        _editTextBelow.Add(_editTextAbove[_editTextAbove.Count - 1]);
        _editTextAbove.RemoveAt(_editTextAbove.Count - 1);
        _cursorTime = 0;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Editable) return;
        if (!(@event is InputEventKey)) return;

        var keyEvent = (InputEventKey)@event;
        if (!@event.IsPressed()) return;

        if (DirectionDistances.ContainsKey((KeyList)keyEvent.Scancode)) {
            var (X, Y) = DirectionDistances[(KeyList)keyEvent.Scancode];
            int distance = Mathf.Clamp(X * 3 * Height + Y * 3, -CursorPos, EditTextLength - CursorPos);
            if (distance > 0)
                for (int i = 0; i < distance; ++i)
                    MoveCursorForward();
            if (distance < 0)
                for (int i = 0; i < -distance; ++i)
                    MoveCursorBack();
            return;
        }
    }

    public override void _Process(float delta)
    {
        if (!Editable) return;

        _cursorTime = (_cursorTime + delta) % CursorPeriod;
        DisplayText();
    }
}
