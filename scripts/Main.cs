using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public partial class Main : Node
{
	[ExportCategory("Parameters")]
	[Export] private int programMemorySize = 2048;
	[Export] private int ramSize = 256;
	[Export] private int portCount = 16; //Subtracted from ram size to get size of port space
	[Export] private int registerCount = 16;
	[Export] private int opcodeLength = 4;

	[ExportCategory("References")]
	[Export] private Display display;
	[Export] private AssemblyView assemblyView;
	[Export] private Button StartStopButton;
	[Export] private Button StepButton;
	[Export] private Button ResetButton;
	[Export] private RichTextLabel FlagsDisplay;
	[Export] private RichTextLabel RegisterDisplay;
	[Export] private RichTextLabel MemoryDisplay;
	[Export] private Label PCDisplay;
	[Export] private Slider SpeedSlider;
	[Export] private SpinBox SpeedInput;
	[Export] private Array<Panel> InputDisplays;
	[Export] private Color ReleasedInput;
	[Export] private Color PressedInput;
	[Export] private CheckBox HexMode;
	[Export] private CheckBox AssemblyFollow;

	bool paused = true;
	int programCounter = 0;

	private int instructionsPerSecond;
	private int waitCounter;

	private byte[] bytecode;
	private byte[] ram;
	private byte[] registers;
	//private byte[] ports;
	private Stack<int> addressStack;

	private bool zeroFlag = false;
	private bool carryFlag = false;
	private bool pushScreen = false;
	private String[] inputs = {"Y", "T", "J", "K", "W", "D", "S", "A"};

	public override void _Ready()
	{
		Reset();

		GetWindow().FilesDropped += LoadProgram;

		display.DisplayInit();

		StartStopButton.ButtonUp += StartStop;
		StepButton.ButtonUp += Step;
		ResetButton.ButtonUp += Reset;

		assemblyView.main = this;
	}

	public override void _ExitTree()
	{
		StartStopButton.ButtonUp -= StartStop;
		StepButton.ButtonUp -= Step;
		ResetButton.ButtonUp -= Reset;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey)
		{
			InputEventKey input = @event as InputEventKey;
			int index = System.Array.IndexOf(inputs, input.AsText());
			if (index == -1) return;
			StyleBoxFlat style = InputDisplays[index].GetThemeStylebox("panel") as StyleBoxFlat;
			style.BgColor = input.Pressed ? PressedInput : ReleasedInput;
			InputDisplays[index].AddThemeStyleboxOverride("panel", style);
		}
	}

	public override void _Process(double delta)
	{
		if (paused) return;
		UpdateVisualisers();
	}

	public override void _PhysicsProcess(double delta)
	{
		instructionsPerSecond = (int)SpeedSlider.Value;
		SpeedInput.Value = instructionsPerSecond;
		int instructionsPerTick = (int)Math.Ceiling((double)instructionsPerSecond / 100);

		if (bytecode == null || paused) return;

		waitCounter--;

		if (waitCounter <= 0)
		{
			for (int i = 0; i < instructionsPerTick; i++) 
			{
				RunNextInstruction();

				foreach (int breakpoint in assemblyView.GetBreakpointedLines())
				{
					if (breakpoint == assemblyView.GetExecutingLines()[0])
					{
						StartStop();
						assemblyView.SetLineAsBreakpoint(breakpoint, false);
						break;
					}
				}
				if (paused) break;
			}
			waitCounter = (int)Math.Ceiling(100 / (double)instructionsPerSecond);
		}

		if (!display.shouldRender) return;
		display.PushBuffer();
		display.shouldRender = false;
	}

	public void SetSpeed(float value)
	{
		int integerValue = (int)value;
		SpeedSlider.Value = integerValue;
	}

	private void RunNextInstruction()
	{
		//StatusLabel.Text = "Program Counter: " + programCounter;
		int index = programCounter * 2;
		ushort instruction = (ushort)(bytecode[index] << 8 | bytecode[index + 1]);
		ProcessOpcode(instruction);
		programCounter %= programMemorySize / 2;
	}

	private enum Operand : byte
	{
		NOP,
		HLT,
		ADD,
		SUB,
		NOR,
		AND,
		XOR,
		RSH,
		LDI,
		ADI,
		JMP,
		BRH,
		CAL,
		RET,
		LOD,
		STR
	}

	private void ProcessOpcode(ushort instruction)
	{
		Operand opcode = (Operand)(instruction >> (16 - opcodeLength));
		byte regA = (byte)((instruction & 0b111100000000) >> 8);
		byte regB = (byte)((instruction & 0b11110000) >> 4);
		byte regC = (byte)(instruction & 0b1111);

		byte immediate = (byte)(instruction & 0b11111111);

		ushort address = (ushort)(instruction & 0b1111111111);

		byte cond = (byte)((instruction & 0b110000000000) >> 10);

		var offset = 0;
		byte memAddress = 0;

		switch (opcode)
		{
			case Operand.NOP: programCounter++; break;
			case Operand.HLT:
				if (!paused) StartStop();
				programCounter = 0;
				break;
			case Operand.ADD:
				carryFlag = (int)registers[regA] + (int)registers[regB] > 255;
				registers[regC] = (byte)(registers[regA] + registers[regB]);
				zeroFlag = registers[regC] == 0;
				//negativeFlag = (registers[dest] & 0b_10000000) == 0b_10000000;
				programCounter++;
				break;
			case Operand.SUB:
				carryFlag = registers[regA] >= registers[regB];
				zeroFlag = registers[regA] == registers[regB];
				registers[regC] = (byte)(registers[regA] - registers[regB]);
				//negativeFlag = (registers[dest] & 0b_10000000) == 0b_10000000;
				programCounter++;
				break;
			case Operand.NOR:
				registers[regC] = (byte)~(registers[regA] | registers[regB]);
				zeroFlag = registers[regC] == 0;
				carryFlag = false;
				programCounter++;
				break;
			case Operand.AND:
				registers[regC] = (byte)(registers[regA] & registers[regB]);
				zeroFlag = registers[regC] == 0;
				carryFlag = false;
				programCounter++;
				break;
			case Operand.XOR:
				registers[regC] = (byte)(registers[regA] ^ registers[regB]);
				zeroFlag = registers[regC] == 0;
				carryFlag = false;
				programCounter++;
				break;
			case Operand.RSH:
				registers[regC] = (byte)(registers[regA] >> 1);
				programCounter++;
				break;
			case Operand.LDI:
				registers[regA] = immediate;
				programCounter++;
				break;
			case Operand.ADI:
				carryFlag = (int)registers[regA] + (int)immediate > 255;
				registers[regA] += immediate;
				zeroFlag = registers[regA] == 0;
				//negativeFlag = (registers[dest] & 0b_10000000) == 0b_10000000;
				programCounter++;
				break;
			case Operand.JMP:
				programCounter = address;
				break;
			case Operand.BRH:
				if ((cond == 0 && zeroFlag) || (cond == 1 && !zeroFlag) || (cond == 2 && carryFlag) || (cond == 3 && !carryFlag)) programCounter = address;
				else programCounter++;
				break;
			case Operand.CAL:
				addressStack.Push(programCounter + 1);
				programCounter = address;
				break;
			case Operand.RET:
				programCounter = addressStack.Pop();
				break;
			case Operand.LOD:
				offset = (regC & 0b111) + (((regC & 0b1000) >> 3) == 1 ? -8 : 0);
				memAddress = (byte)(registers[regA] + offset);
				if (memAddress < ramSize - portCount) registers[regB] = ram[memAddress];
				else registers[regB] = display.LoadPort(memAddress);
				programCounter++;
				break;
			case Operand.STR:
				offset = (regC & 0b111) + (((regC & 0b1000) >> 3) == 1 ? -8 : 0);
				memAddress = (byte)(registers[regA] + offset);
				if (memAddress < ramSize - portCount) ram[memAddress] = registers[regB];
				else display.StorePort(memAddress, registers[regB]);
				programCounter++;
				break;
			default: GD.Print("Invalid opcode in file. This should be impossible."); break;
		}

		registers[0] = 0;
	}

	private void StartStop()
	{
		paused = !paused;
		assemblyView.follow = AssemblyFollow.ButtonPressed && !paused;
		StartStopButton.Text = paused ? "-Start-" : "-Stop-";
		StartStopButton.ButtonPressed = !paused;
		if (programCounter == 0)
			Reset();
	}

	private void Step()
	{
		if (bytecode == null) return;
		if (!paused) StartStop();
		RunNextInstruction();
		assemblyView.follow = AssemblyFollow.ButtonPressed;
		UpdateVisualisers();
	}

	private void Reset()
	{
		assemblyView.follow = AssemblyFollow.ButtonPressed;
		programCounter = 0;
		ram = new byte[ramSize];
		registers = new byte[registerCount];
		//ports = new byte[portCount];
		addressStack = new Stack<int>();
		//StatusLabel.Text = "";
		UpdateVisualisers();
		display.DisplayInit();
		if (!display.displayInitialized) return;
		display.ClearBuffer();
		display.PushBuffer();
	}

	public void LoadProgram(string[] files)
	{
		if (files[0].LastIndexOf(".mc") != -1)
		{
			Reset();
			try
			{
				var code = FileAccess.GetFileAsString(files[0]);
				code = Regex.Replace(code, @"\t|\n|\r", "");
				bytecode = ConvertBinaryStringToByteArray(code);

				//StatusLabel.Text = "Program Loaded";

				
			} catch
			{
				//StatusLabel.Text = "Failed to Load Program";
			}
		}
		else if (files[0].LastIndexOf(".as") != -1)
		{
			assemblyView.LoadAssembly(files[0]);
		}
	}

	private void UpdateVisualisers()
	{
		FlagsDisplay.Text = "[center]Flags\n Carry: [color=#969ca8]" + carryFlag + "[/color] | Zero: [color=#969ca8]" + zeroFlag;
		//string text = BitConverter.ToString(registers).Replace("-", " ");
		string text = "[center]Registers\n";
		for (int i = 0; i < registerCount; i++)
		{
			text += "" + (i < 10 ? " " : "") + "r" + i + ":[color=#969ca8]" + padString("" + registers[i], 3, "0") + "[/color]";
			if ((i - 3) % 4 != 0) text += "  ";
			else text += "\n";
		}
		RegisterDisplay.Text = text;

		//text = BitConverter.ToString(ram).Replace("-", " ");
		text = "[center]RAM (Address:Value)\n";
		for (int i = 0; i < ramSize - portCount; i++)
		{
			text += "" + padString("" + i, 3, "0") + ":[color=#969ca8]" + padString("" + ram[i], 3, "0") + "[/color]";
			if ((i - 3) % 4 != 0) text += "  ";
			else text += "\n";
		}
		MemoryDisplay.Text = text;
		//text = BitConverter.ToString(ports).Replace("-", " ");
		//PortDisplay.Text = text;
		if (assemblyView.initialized) 
		{
			assemblyView.programCounter = programCounter;
			assemblyView.MoveCursor();
		}

		PCDisplay.Text = "   Program Counter: " + programCounter;
	}

	private string padString(string input, int length, string fill)
	{
		string output = "";
		for (int i = 0; i < length - input.Length; i++)
		{
			output += fill;
		}
		output += input;
		return output;
	}

	private byte[] ConvertBinaryStringToByteArray(string binaryString)
	{
		int numBytes = (int)Math.Ceiling((double)binaryString.Length / 8);
		byte[] byteArray = new byte[programMemorySize];

		for (int i = 0; i < programMemorySize; i++)
		{
			if (i < numBytes)
				byteArray[i] = Convert.ToByte(binaryString.Substring(i * 8, Math.Min(8, binaryString.Length - i * 8)), 2);
			else
				byteArray[i] = 0;
		}

		return byteArray;
	}
}
