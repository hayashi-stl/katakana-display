using Godot;
using System;
using System.Linq;

public class Trailer : WorldEnvironment
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    SpatialMaterial _faderMaterial;

    const float CardMoveTime = 0.2f;
    const float CardDistance = 272;
    const float CardPanTime = 4.0f;
    const float CardInDistance = 267;
    const float CardPanDistance = CardInDistance + (CardDistance - CardInDistance) * 2;
    const float CardOutDistance = CardDistance * 2;
    const float FadeTime = 0.3f;
    const float TypeSpacing = 0.1f;

    public void DoCard(SceneTreeTween tween, string japanese, string english)
    {
        var nodeJ = GetNode<Spatial>(japanese);
        var nodeE = GetNode<Spatial>(english);
        tween           .TweenProperty(nodeJ, "visible", true, 0);
        tween.Parallel().TweenProperty(nodeE, "visible", true, 0);
        tween           .TweenProperty(nodeJ, "translation", nodeJ.Translation + Vector3.Back * CardInDistance, CardMoveTime);
        tween.Parallel().TweenProperty(nodeE, "translation", nodeE.Translation + Vector3.Back * -CardInDistance, CardMoveTime);
        tween           .TweenProperty(nodeJ, "translation", nodeJ.Translation + Vector3.Back * CardPanDistance, CardPanTime);
        tween.Parallel().TweenProperty(nodeE, "translation", nodeE.Translation + Vector3.Back * -CardPanDistance, CardPanTime);
        tween           .TweenProperty(nodeJ, "translation", nodeJ.Translation + Vector3.Back * CardOutDistance, CardMoveTime);
        tween.Parallel().TweenProperty(nodeE, "translation", nodeE.Translation + Vector3.Back * -CardOutDistance, CardMoveTime);
        tween           .TweenProperty(nodeJ, "visible", false, 0);
        tween.Parallel().TweenProperty(nodeE, "visible", false, 0);
    }

    public void FadeText(SceneTreeTween tween, string nodePath, bool @in)
    {
        var node = GetNode<Unit>(nodePath);
        if (@in)
            tween.TweenProperty(node, "visible", true, 0);
        tween.TweenProperty(_faderMaterial, "albedo_color", new Color(@in ? 0x00000000 : 0x000000ff), FadeTime);
        if (!@in)
            tween.TweenProperty(node, "visible", false, 0);
    }

    public void TypeText(SceneTreeTween tween, string nodePath, string text)
    {
        var node = GetNode<Unit>(nodePath);
        bool delay = true;
        foreach (var c in text) {
            if (c == '@') {
                delay = false;
                continue;
            }

            var ev = new InputEventKey {
                Pressed = true,
                Scancode = c == '\x08' ? (uint)KeyList.Backspace :
                    c == '\x7f' ? (int)KeyList.Delete :
                    c == '↑' ? (int)KeyList.Up :
                    c == '↓' ? (int)KeyList.Down :
                    c == '←' ? (int)KeyList.Left :
                    c == '→' ? (int)KeyList.Right :
                    0u,
                Unicode = c
            };
            if (delay)
                tween.TweenInterval(TypeSpacing);
            tween.TweenCallback(node, "HandleInput", new Godot.Collections.Array(){ ev });
            delay = true;
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _faderMaterial = (SpatialMaterial)GetNode<MeshInstance>("Camera/Fader").GetSurfaceMaterial(0);
        const string TextPath = "Text";
        const string UnitPath = "Unit";
        const string Text50Path = "Text50";

        var tween = CreateTween();
        tween.TweenInterval(1);
        DoCard(tween, "46Segment J", "46Segment E");
        FadeText(tween, TextPath, true);
        TypeText(tween, TextPath, "^^^^^^^^^naniconosouti? ^^omosiroina-.^^chottoijittemiyou.");
        FadeText(tween, TextPath, false);
        DoCard(tween, "SegmentCount J", "SegmentCount E");
        FadeText(tween, UnitPath, true);
        TypeText(tween, UnitPath, string.Join("",
            Enumerable.Range(0, Unit.NumSegments).Select(i => $"@\x08@{(char)(Unit.SingleSegmentStart + i)}^^")));
        FadeText(tween, UnitPath, false);
        DoCard(tween, "Katakana J", "Katakana E");
        FadeText(tween, Text50Path, true);
        TypeText(tween, Text50Path, "aiueokakikukekosasisusesotatitutetonaninunenohahihuhehomamimumemoya yu yorarirurerowa   won'^^^^^^^^^^");
        FadeText(tween, Text50Path, false);
        DoCard(tween, "OldKana J", "OldKana E");
        FadeText(tween, Text50Path, true);
        TypeText(tween, Text50Path, "^^^→\x7fwyi^^^↓\x7fwye^^^^^^^^^^^^^^^^^^^^");
        FadeText(tween, Text50Path, false);
        DoCard(tween, "Dakuten J", "Dakuten E");
        FadeText(tween, TextPath, true);
        TypeText(tween, TextPath, "^^^^^^   a\"i\"u\"e`o\"^^n'\"ra\"ma\"ka`\"E\"V\"E\"N\" L`A`T`I`N` L\"E\"T\"T\"E\"R\"S\"!\"^^^^^^^^^^");
        FadeText(tween, TextPath, false);
        DoCard(tween, "46Segment2 J", "46Segment2 E");
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
