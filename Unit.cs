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
    int? _pendingPos = null;
    int CursorPos => _editTextAbove.Count;
    int EditTextLength => _editTextAbove.Count + _editTextBelow.Count;
    float _cursorTime;
    const float CursorPeriod = 1.0f;

    const int Span = 3;


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
            mode = (mode + 1) % Span;
            if (iter == chars.Count && mode == 0)
                break;
        }
        return result;
    }

    void DisplayText() {
        var editText = _editTextAbove.Concat(_editTextBelow.Reverse<string>()).ToList();
        for (int i = 0; i < Height * Width; ++i) {
            ulong field = Span * i >= editText.Count ? 0uL :
                Util.SegmentMap[$"{editText[Span * i + 0]}"] |
                Util.SegmentMap[$"{editText[Span * i + 1]}"] |
                Util.SegmentMap[$"{editText[Span * i + 2]}"];
            
            if (Editable && i == CursorPos / Span && _cursorTime < 0.5)
                field ^= Util.CursorBitfield;
            if (_pendingPos != null && i >= _pendingPos / Span && i < CursorPos / Span)
                field |= Util.PendingBitfield;
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
        _editTextAbove.Add(_editTextBelow.Last());
        _editTextBelow.RemoveAt(_editTextBelow.Count - 1);
        _cursorTime = 0;
    }

    void MoveCursorBack() {
        _editTextBelow.Add(_editTextAbove.Last());
        _editTextAbove.RemoveAt(_editTextAbove.Count - 1);
        _cursorTime = 0;
    }

    void AttemptCharConversion() {

    }

    public override void _Input(InputEvent @event)
    {
        if (!Editable) return;
        if (!(@event is InputEventKey)) return;

        var keyEvent = (InputEventKey)@event;
        if (!@event.IsPressed()) return;

        if (DirectionDistances.ContainsKey((KeyList)keyEvent.Scancode)) {
            var (X, Y) = DirectionDistances[(KeyList)keyEvent.Scancode];

            int distance = Mathf.Clamp(X * Span * Height + Y * Span, -CursorPos, EditTextLength - CursorPos);
            if (distance > 0)
                for (int i = 0; i < distance; ++i)
                    MoveCursorForward();
            if (distance < 0)
                for (int i = 0; i < -distance; ++i)
                    MoveCursorBack();
            _pendingPos = null;
            return;
        }

        // Deleting text
        if ((KeyList)keyEvent.Scancode == KeyList.Backspace && CursorPos != 0) {
            if (_editTextAbove.Last() == "　" ||
                (_editTextAbove[_editTextAbove.Count - Span] == "　" && _editTextAbove[_editTextAbove.Count - 2] == "　"))
                _editTextAbove.RemoveRange(_editTextAbove.Count - Span, Span);
            else
                _editTextAbove[_editTextAbove.Count - 1] = "　";
            if (CursorPos <= _pendingPos)
                _pendingPos = null;
            return;
        }

        if ((KeyList)keyEvent.Scancode == KeyList.Delete && CursorPos != EditTextLength) {
            if (_editTextBelow[_editTextBelow.Count - Span] == "　" ||
                (_editTextBelow.Last() == "　" && _editTextBelow[_editTextBelow.Count - 2] == "　"))
                _editTextBelow.RemoveRange(_editTextBelow.Count - Span, Span);
            else
                _editTextBelow[_editTextBelow.Count - 1] = _editTextBelow[_editTextBelow.Count - 2] = "　";
            _pendingPos = null;
            return;
        }

        string letter = char.ConvertFromUtf32((int)keyEvent.Unicode);
        letter = Util.NormalizationMap.ContainsKey(letter) ? Util.NormalizationMap[letter] : letter;
        if (!Util.SegmentMap.ContainsKey(letter))
            return;

        if (Util.Sutegana.Contains(letter)) {
            if (_editTextAbove.Last() == "　") {
                _editTextAbove[_editTextAbove.Count - 1] = letter;
                _pendingPos = null;
                return;
            }
        } else if (Util.Dakuten.Contains(letter)) {
            if (_editTextAbove[_editTextAbove.Count - 2] == "　" && _editTextAbove.Last() == "　") {
                _editTextAbove[_editTextAbove.Count - 2] = letter;
                _pendingPos = null;
                return;
            }
        }

        var normalized = NormalizeDisplayText(letter);
        _editTextAbove.AddRange(normalized);
        if (Util.Lowercase.Contains(letter)) {
            if (_pendingPos == null)
                _pendingPos = CursorPos - Span;
            AttemptCharConversion();
        } else {
            _pendingPos = null;
        }
    }

    public override void _Process(float delta)
    {
        if (!Editable) return;

        _cursorTime = (_cursorTime + delta) % CursorPeriod;
        DisplayText();
    }
}
