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

    public bool displayInitialized { get; private set; }

    private bool[,] displayBuffer;
    private string charBuffer;
    private int displayedNum = 0;
    private bool unsigned = true;

    private Vector2I pixelPos;

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
        displayBuffer = new bool[resolution.X, resolution.Y];
        TextDisplay.Text = "Output";
        NumDisplay.Text = "" + displayedNum;

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

    private void UpdateNumDisplay()
    {
        if (unsigned) NumDisplay.Text = "" + displayedNum;
        else NumDisplay.Text = "" + (displayedNum & 0b01111111) + (((displayedNum & 0b10000000) != 0) ? -128 : 0);
    }

    public void PushBuffer()
    {
        Array<Node> children = GetChildren();
        int pixels = resolution.X * resolution.Y;
        for (int i = 0; i < pixels; i++)
        {
            TextureRect child = children[i] as TextureRect;
            child.Texture = displayBuffer[i % resolution.X, i / resolution.X] ? onSprites[DisplayTexture.Selected] : offSprites[DisplayTexture.Selected];
        }
    }

    public void ClearBuffer()
    {
        for (int x = 0; x < resolution.X; x++)
        {
            for (int y = 0; y < resolution.Y; y++)
            {
                displayBuffer[x, y] = false;
            }
        }
    }

    public void StorePort(byte port, byte data)
    {
        switch (port)
        {
            //Pixel X
            case 240:
                pixelPos.X = data;
                break;
            //Pixel Y
            case 241:
                pixelPos.Y = data;
                break;
            //Draw Pixel
            case 242:
                if (pixelPos.X >= resolution.X || pixelPos.Y >= resolution.Y) break;
                displayBuffer[pixelPos.X, pixelPos.Y] = true;
                break;
            //Clear Pixel
            case 243:
                if (pixelPos.X >= resolution.X || pixelPos.Y >= resolution.Y) break;
                displayBuffer[pixelPos.X, pixelPos.Y] = false;
                break;
            //Buffer screen
            case 245:
                PushBuffer();
                break;
            //Clear Screen Buffer
            case 246:
                ClearBuffer();
                break;
            //Write Char
            case 247:
                charBuffer += System.Text.Encoding.ASCII.GetString(new byte[] { data });
                break;
            //Buffer Chars
            case 248:
                TextDisplay.Text = charBuffer;
                break;
            //Clear Chars Buffer
            case 249:
                charBuffer = "";
                break;
            //Show Number
            case 250:
                displayedNum = data;
                UpdateNumDisplay();
                break;
            //Clear Number
            case 251:
                NumDisplay.Text = "";
                break;
            //Signed Mode
            case 252:
                unsigned = false;
                UpdateNumDisplay();
                break;
            //Unsigned Mode
            case 253:
                unsigned = true;
                UpdateNumDisplay();
                break;
        }
    }

    public byte LoadPort(byte port)
    {
        switch (port)
        {
            case 244:
                return (byte)(displayBuffer[pixelPos.X,pixelPos.Y] ? 1 : 0);
            case 254:
                Random rand = new Random();
                return (byte)rand.Next();
            case 255:
                return GetInputs();
        }
        return 0;
    }

    public byte GetInputs()
    {
        bool up = Input.IsActionPressed("up");
        bool down = Input.IsActionPressed("down");
        bool left = Input.IsActionPressed("left");
        bool right = Input.IsActionPressed("right");
        bool start = Input.IsActionPressed("start");
        bool select = Input.IsActionPressed("select");
        bool a = Input.IsActionPressed("a");
        bool b = Input.IsActionPressed("b");
        bool[] inputs = new bool[] {start, select, a, b, up, right, down, left};
        return ToByte(inputs);
    }

    public static byte ToByte(bool[] data)
    {
        if (data.Length != 8)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "data length must be 8");
        }

        byte result = 0;
        for (int i = 0; i < data.Length; i++)
        {
            result |= (byte)(data[i] ? 1 << (7 - i) : 0);
        }
        return result;
    }
}
