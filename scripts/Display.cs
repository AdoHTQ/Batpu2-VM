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
    [Export] OptionButton DisplayMode;
    [Export] Label IndexLabel;

    private bool displayInitialized;

    private bool previousClock;

    private bool[] buffer;
    private int bufferIndex;

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
        bufferIndex = 0;
        System.Array.Fill(buffer, false);
    }

    //Data parameter will be either memory or ports depending on the mode
    public void UpdateDisplay(byte[] ports, byte[] ram)
    {
        byte clock_port = ports[3];
        byte data_port = ports[2];

        bool clock = (clock_port >> 7) == 1;
        if (clock == previousClock) return;

        bool clearDisplay = ((clock_port >> 6) & 1) == 1;
        bool swapBuffers = ((clock_port >> 5) & 1) == 1;

        if (DisplayMode.Selected == 0)
        {
            if (clearDisplay)
            {
                System.Array.Fill(buffer, false);
                SwapBuffers();
                
            } else if (swapBuffers)
            {
                SwapBuffers();
            } else
            {
                for (int i = 0; i < 8; i++)
                {
                    buffer[bufferIndex] = ((data_port >> i) & 1) == 1;
                    bufferIndex++;
                }
            }
        }
        IndexLabel.Text = "Buffer Index: " + bufferIndex;

        previousClock = clock;
    }
}
