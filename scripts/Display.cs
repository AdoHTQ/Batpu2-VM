using Godot;
using System;
using Godot.Collections;

public partial class Display : Node
{
    [ExportCategory("Properties")]
    [Export] Vector2I resolution;

    [ExportCategory("References")]
    [Export] Texture2D[] offSprites;
    [Export] Texture2D[] onSprites;
    [Export] OptionButton DisplayTexture;
    [Export] Label NumDisplay;
    [Export] Label TextDisplay;

    private bool displayInitialized;

    private bool[] buffer;

    public void DisplayInit()
    {
        if (displayInitialized) return;
        for (int x = 0; x < resolution.X; x++)
        {
            for (int y = 0; y < resolution.Y; y++)
            {
                TextureRect sprite = new TextureRect();
                sprite.Texture = offSprites[DisplayTexture.Selected];
                AddChild(sprite);
            }
        }
        buffer = new bool[resolution.X * resolution.Y];

        displayInitialized = true;
    }

    public void UpdateSprites(int index)
    {
        Array<Node> children = GetChildren();
        foreach (Node child in children)
        {
            (child as TextureRect).Texture = offSprites[index];
        }
    }

    private void SwapBuffers()
    {
        Array<Node> children = GetChildren();
        int pixels = resolution.X * resolution.Y;
        for (int i = 0; i < pixels; i++)
        {
            TextureRect child = children[i] as TextureRect;
            child.Texture = buffer[i] ? onSprites[DisplayTexture.Selected] : offSprites[DisplayTexture.Selected];
        }
        System.Array.Fill(buffer, false);
    }

    //Data parameter will be either memory or ports depending on the mode
    public void UpdateDisplay(byte[] ports)
    {
        
    }
}
