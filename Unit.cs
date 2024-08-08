using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Tool]
public class Unit : Spatial
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    string _text;
    [Export]
    public string Text {
        get => _text;
        set {
            _text = value;
            if (_mesh != null)
                EditorUpdateText();
        }
    }
    [Export]
    public int Height { get; set; }
    [Export]
    public int Width { get; set; }
    [Export]
    public bool Editable { get; set; }
    [Export]
    public bool Cursorless { get; set; }
    [Export]
    public Color Color = new Color(0.5f, 0.5f, 1.0f);

    List<string> _editTextAbove;
    List<string> _editTextBelow;
    int? _pendingPos = null;
    int CursorPos => _editTextAbove.Count;
    int EditTextLength => _editTextAbove.Count + _editTextBelow.Count;
    float _cursorTime;
    bool _latin = false;
    MultiMeshInstance _mesh;

    const float CursorPeriod = 1.0f;

    const int Span = 3;
    public const int SingleSegmentStart = 0x2460;
    public const int NumSegments = 46;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //string text = "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤいユえヨラリルレロワヰうヱヲン" +
        //    "゛゜ー＝ァィゥェォャュョヮッ";
        _mesh = GetNode<MultiMeshInstance>("%Mesh");
        if (GetTree().EditedSceneRoot != this)
            _mesh.Multimesh = (MultiMesh)_mesh.Multimesh.Duplicate();
        EditorUpdateText();
    }

    public void EditorUpdateText()
    {
        _mesh.Multimesh.InstanceCount = Height * Width;
        for (int i = 0; i < Height * Width; ++i) {
            _mesh.Multimesh.SetInstanceTransform(i, new Transform(Basis.Identity, new Vector3((i / Height) * -40, 0, (i % Height) * 24)));
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
        text = Util.Normalize(text);
        // Spaces
        var result = new List<string>();
        int iter = 0;
        int mode = 0;
        
        var chars = Util.Chars(text);
        while (true) {
            if (iter == chars.Count && mode == 0)
                break;
            var str = iter < chars.Count ? chars[iter] : "";
            if (Util.Sutegana.Contains(str) ? mode == 2 : Util.Dakuten.Contains(str) ? mode == 1 : mode == 0) {
                result.Add(str);
                ++iter;
            } else {
                result.Add("　");
            }
            mode = (mode + 1) % Span;
        }
        return result;
    }

    void DisplayText() {
        var editText = _editTextAbove.Concat(_editTextBelow.Reverse<string>()).ToList();
        for (int i = 0; i < Height * Width; ++i) {
            ulong field = Span * i >= editText.Count ? 0uL :
                editText[Span * i + 0][0] >= SingleSegmentStart && editText[Span * i + 0][0] < SingleSegmentStart + NumSegments ?
                    1uL << (editText[Span * i + 0][0] - SingleSegmentStart) :
                Util.SegmentMap[$"{editText[Span * i + 0]}"] |
                Util.SegmentMap[$"{editText[Span * i + 1]}"] |
                Util.SegmentMap[$"{editText[Span * i + 2]}"];
            ulong color = (ulong)(Color.r8 | Color.g8 << 8 | Color.b8 << 16);
            
            if (!Engine.EditorHint && Editable && !Cursorless && i == CursorPos / Span && _cursorTime < 0.5)
                field ^= _latin ? Util.LatinCursorBitfield : Util.CursorBitfield;
            if (_pendingPos != null && i >= _pendingPos / Span && i < CursorPos / Span)
                field |= Util.PendingBitfield;
            _mesh.Multimesh.SetInstanceCustomData(i, new Color(field % (1 << 24), field >> 24, color, 0.0f));
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

    (string Transformed, string Leftover) DoCharConversion() {
        int pending = _pendingPos.Value;

        string toLookup = "";
        for (int i = pending; i < CursorPos; i += 3)
            toLookup += _editTextAbove[i];
        _editTextAbove.RemoveRange(pending, CursorPos - pending);

        return Util.ConversionTrie.Lookup(toLookup);
    }

    // Returns true if special
    bool AddChar(string letter) {
        bool special = false;
        if (Util.Sutegana.Contains(letter)) {
            special = true;
            if (_editTextAbove.Last() == "　") {
                _editTextAbove[_editTextAbove.Count - 1] = letter;
                return true;
            }
        } else if (Util.Dakuten.Contains(letter)) {
            special = true;
            if (_editTextAbove[_editTextAbove.Count - 2] == "　" && _editTextAbove.Last() == "　") {
                _editTextAbove[_editTextAbove.Count - 2] = letter;
                return true;
            }
        }

        var normalized = NormalizeDisplayText(letter);
        _editTextAbove.AddRange(normalized);
        return special;
    }

    public override void _Input(InputEvent @event) {
        if (!(GetTree().CurrentScene is Trailer))
            HandleInput(@event);
    }

    public void HandleInput(InputEvent @event)
    {
        if (Engine.EditorHint || !Editable) return;
        if (!(@event is InputEventKey)) return;

        var keyEvent = (InputEventKey)@event;
        if (!@event.IsPressed()) return;

        if (keyEvent.GetScancodeWithModifiers() == ((uint)KeyModifierMask.MaskCtrl | (uint)KeyList.L)) {
            _latin = !_latin;
            _pendingPos = null;
            return;
        }

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

        if ((KeyList)keyEvent.Scancode == KeyList.Enter) {
            _pendingPos = null;
            _cursorTime = 0;
            return;
        }

        string letter = char.ConvertFromUtf32((int)keyEvent.Unicode);
        letter = Util.Normalize(letter);
        if (!Util.SegmentMap.ContainsKey(letter) && ((int)keyEvent.Unicode < SingleSegmentStart || (int)keyEvent.Unicode >= SingleSegmentStart + NumSegments))
            return;

        var special = AddChar(letter);
        _cursorTime = 0;
        if (_latin || special) {
            _pendingPos = null;
            return;
        }

        if (_pendingPos == null)
            _pendingPos = CursorPos - Span;
        (string Transformed, string Leftover) = DoCharConversion();

        foreach (var c in Util.Chars(Transformed))
            AddChar(c);
        _pendingPos = CursorPos;
        foreach (var c in Util.Chars(Leftover))
            AddChar(c);
    }

    public override void _Process(float delta)
    {
        if (Engine.EditorHint || !Editable) return;

        _cursorTime = (_cursorTime + delta) % CursorPeriod;
        DisplayText();
    }
}
