using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public class DisplayEditor : Node2D
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    List<Button> _buttons;
    TextEdit _text;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _text = GetNode<TextEdit>("%Text");
        _buttons = new List<Button>();
        foreach (var child in GetNode("%Buttons").GetChildren()) {
            var button = (Button)child;
            _buttons.Add(button);
            button.Text = $"{button.GetIndex()}";
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("print")) {
            ulong field = 0;
            for (int i = 0; i < _buttons.Count; ++i)
                if (_buttons[i].Pressed)
                    field |= 1uL << i;

            StringBuilder bitStr = new StringBuilder();
            for (int i = 46 - 1; i >= 0; --i) {
                bitStr.Append($"{field >> i & 1}");
                if (i % 4 == 0 && i != 0)
                    bitStr.Append("_");
            }
            string line = $"        {{\"{_text.Text}\", 0b{bitStr}}},";
            GD.Print(line);
        }
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
