using Godot;
using System;
using Godot.Collections;
using System.Linq;
using System.Collections.Generic;

public partial class Display : Node
{
	[ExportCategory("Properties")]
	[Export] Vector2I resolution;

	[ExportCategory("References")]
	[Export] Texture2D[] offSprites;
	[Export] Texture2D[] onSprites;
	//[Export] OptionButton DisplayTexture;
	[Export] Label NumDisplay;
	[Export] Label TextDisplay;

	public bool displayInitialized { get; private set; }

	private bool[,] displayBuffer;
	private bool[,] displayBufferBuffer;
	private string charBuffer = "";
	private int displayedNum = 0;
	private bool unsigned = true;
	public bool shouldRender = false;

	private int texture;

	private Vector2I pixelPos;

	private List<char> charValues = new List<char> {' ', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '.', '!', '?'};

    public void DisplayInit()
    {
        displayedNum = 0;
        displayBuffer = new bool[resolution.X, resolution.Y];
        displayBufferBuffer = new bool[resolution.X, resolution.Y];
        TextDisplay.Text = "__________";
        NumDisplay.Text = "" + displayedNum;

		if (displayInitialized) return;
		for (int x = 0; x < resolution.X; x++)
		{
			for (int y = 0; y < resolution.Y; y++)
			{
				TextureRect sprite = new TextureRect();
				sprite.Texture = offSprites[texture];
				AddChild(sprite);
			}
		}

		displayInitialized = true;
	}

	public void UpdateSprites(int index)
	{
		texture = index;
		Array<Node> children = GetChildren();
		foreach (Node child in children)
		{
			(child as TextureRect).Texture = offSprites[texture];
		}
	}

	private void UpdateNumDisplay()
	{
		if (unsigned) NumDisplay.Text = "" + displayedNum;
		else NumDisplay.Text = "" + ((displayedNum & 0b01111111) + (((displayedNum & 0b10000000) != 0) ? -128 : 0));
	}

	public void PushBuffer()
	{
		Array<Node> children = GetChildren();
		int pixels = resolution.X * resolution.Y;
		for (int i = 0; i < pixels; i++)
		{
			TextureRect child = children[i] as TextureRect;
			child.Texture = displayBuffer[i % resolution.X, i / resolution.Y] ? onSprites[texture] : offSprites[texture];
		}
	}

	public void ClearBuffer()
	{
		for (int x = 0; x < resolution.X * resolution.Y; x++)
			displayBufferBuffer[x%resolution.X, x/resolution.Y] = false;
	}

	public void StorePort(byte port, byte data)
	{
		switch (port)
		{
			//Pixel X
			case 240:
				pixelPos.X = data & 0b00011111;
				break;
			//Pixel Y
			case 241:
				pixelPos.Y = data & 0b00011111;
				break;
			//Draw Pixel
			case 242:
				if (pixelPos.X >= resolution.X || pixelPos.Y >= resolution.Y) break;
				displayBufferBuffer[pixelPos.X, resolution.Y - 1 - pixelPos.Y] = true;
				break;
			//Clear Pixel
			case 243:
				if (pixelPos.X >= resolution.X || pixelPos.Y >= resolution.Y) break;
				displayBufferBuffer[pixelPos.X, resolution.Y - 1 - pixelPos.Y] = false;
				break;
			//Buffer screen
			case 245:
				System.Array.Copy(displayBufferBuffer, 0, displayBuffer, 0, resolution.X * resolution.Y);
				shouldRender = true;
				break;
			//Clear Screen Buffer
			case 246:
				ClearBuffer();
				break;
			//Write Char
			case 247:
				charBuffer += charValues[data];
				break;
			//Buffer Chars
			case 248:
				TextDisplay.Text = PadWithUnderscores(charBuffer.ToUpper());
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
            default:
                break;
		}
	}

    public static string PadWithUnderscores(string inputString)
    {
        if (inputString == null)
        {
            throw new ArgumentNullException(nameof(inputString));
        }

        int targetLength = 10;
        int padLength = targetLength - inputString.Length;
        return padLength > 0 ? inputString.PadRight(targetLength, '_') : inputString;
    }

	public byte LoadPort(byte port)
	{
		switch (port)
		{
			case 244:
				return (byte)(displayBufferBuffer[pixelPos.X, resolution.Y - 1 - pixelPos.Y] ? 1 : 0);
			case 254:
				Random rand = new Random();
				return (byte)rand.Next();
			case 255:
				return GetInputs();
            default:
                return 0;
		}
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
