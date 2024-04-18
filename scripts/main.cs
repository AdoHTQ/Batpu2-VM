using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public partial class main : Node
{
    [ExportCategory("Parameters")]
    [Export] private int programMemorySize = 2048;
    [Export] private int ramSize = 256;
    [Export] private int portCount = 256;
    [Export] private int registerCount = 8;
    [Export] private int opcodeLength = 4;

    [ExportCategory("References")]
    [Export] private Display display;
    [Export] private Button StartStopButton;
    [Export] private Button StepButton;
    [Export] private Button ResetButton;
    [Export] private Label StatusLabel;
    [Export] private Label FlagsDisplay;
    [Export] private Label RegisterDisplay;
    [Export] private Label MemoryDisplay;
    [Export] private Label PortDisplay;
    [Export] private Slider SpeedSlider;
    [Export] private SpinBox SpeedInput;

    bool paused = true;
    int programCounter = 0;

    private int instructionsPerSecond;
    private int waitCounter;

    private byte[] bytecode;
    private byte[] ram;
    private byte[] registers;
    private byte[] ports;
    private Stack<int> addressStack;

    private bool zeroFlag = false;
    private bool carryFlag = false;

    public override void _Ready()
    {
        Reset();

        GetWindow().FilesDropped += LoadProgram;

        StartStopButton.ButtonUp += StartStop;
        StepButton.ButtonUp += Step;
        ResetButton.ButtonUp += Reset;
    }

    public override void _ExitTree()
    {
        StartStopButton.ButtonUp -= StartStop;
        StepButton.ButtonUp -= Step;
        ResetButton.ButtonUp -= Reset;
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
            for (int i = 0; i < instructionsPerTick; i++) RunNextInstruction();
            waitCounter = (int)Math.Ceiling(100 / (double)instructionsPerSecond);
        }
        
    }

    public void SetSpeed(float value)
    {
        int integerValue = (int)value;
        SpeedSlider.Value = integerValue;
    }

    private void RunNextInstruction()
    {
        StatusLabel.Text = "Program Counter: " + programCounter;
        int index = programCounter * 2;
        ushort instruction = (ushort)(bytecode[index] << 8 | bytecode[index + 1]);
        ProcessOpcode(instruction);
        programCounter %= programMemorySize / 2;
        UpdateVisualisers();
        display.UpdateDisplay(ports, ram);
    }

    private void ProcessOpcode(ushort instruction)
    {
        
        byte opcode = (byte)(instruction >> (16 - opcodeLength));
        ushort address = (ushort)(instruction & 1023);
        byte immediate = (byte)(instruction & 255);
        byte cond = (byte)((instruction & 0b110000000000) >> 10);
        byte dest = (byte)((instruction & 3584) >> 9);
        int offset = (byte)(instruction & 0b11111);
        bool sign = (instruction & 0b100000) >> 5 == 1;
        offset += sign ? -32 : 0;

        byte port = (byte)(instruction & 255);

        byte regA = (byte)((instruction & 448) >> 6);
        byte operation = (byte)((instruction & 56) >> 3);
        byte regB = (byte)(instruction & 7);

        switch (opcode)
        {
            //NOP
            case 0: programCounter++; break;
            //HLT
            case 1:
                if (!paused) StartStop(); 
                break;
            //JMP
            case 2:
                programCounter = address;
                break;
            //BCH
            case 3:
                if ((cond == 0 && zeroFlag) || (cond == 1 && !zeroFlag) || (cond == 2 && !zeroFlag && carryFlag) || (cond == 3 && !carryFlag)) programCounter = address;
                else programCounter++;
                break;
            //CAL
            case 4:
                addressStack.Push(programCounter + 1);
                programCounter = address;
                break;
            //RET
            case 5:
                programCounter = addressStack.Pop();
                break;
            //PST
            case 6:
                registers[dest]=ports[port];
                programCounter++;
                break;
            //PLD
            case 7:
                ports[port] = registers[dest];
                programCounter++;
                break;
            //MLD
            case 8:
                registers[dest] = ram[registers[regA] + offset];
                programCounter++;
                break;
            //MST
            case 9:
                ram[registers[regA] + offset] = registers[dest];
                programCounter++;
                break;
            //LDI
            case 10:
                registers[dest] = immediate;
                programCounter++;
                break;
            //ADI
            case 11:
                carryFlag = (int)registers[dest] + (int)immediate > 255;
                //GD.Print(carryFlag);
                registers[dest] += immediate;
                zeroFlag = registers[dest] == 0;
                //negativeFlag = (registers[dest] & 0b_10000000) == 0b_10000000;
                programCounter++;
                break;
/*            //CMI
            case 12:
                negativeFlag = immediate > registers[dest];
                registers[0] = (byte)(registers[dest] - immediate);
                zeroFlag = registers[0] == 0;
                programCounter++;
                break;*/
            //ADD
            case 12:
                carryFlag = (int)registers[regA] + (int)registers[regB] > 255;
                registers[dest] = (byte)(registers[regA] + registers[regB]);
                zeroFlag = registers[dest] == 0;
                //negativeFlag = (registers[dest] & 0b_10000000) == 0b_10000000;
                programCounter++;
                break;
            //SUB
            case 13:
                carryFlag = registers[regA] - registers[regB] < 0;
                registers[dest] = (byte)(registers[regA] - registers[regB]);
                zeroFlag = registers[dest] == 0;
                //negativeFlag = (registers[dest] & 0b_10000000) == 0b_10000000;
                programCounter++;
                break;
            //BIT
            case 14:
                switch (operation)
                {
                    case 0: registers[dest] = (byte)(registers[regA] | registers[regB]); break;
                    case 1: registers[dest] = (byte)(registers[regA] & registers[regB]); break;
                    case 2: registers[dest] = (byte)(registers[regA] ^ registers[regB]); break;
                    case 3: registers[dest] = (byte)~(registers[regA] & ~registers[regB]); break;
                    case 4: registers[dest] = (byte)~(registers[regA] | registers[regB]); break;
                    case 5: registers[dest] = (byte)~(registers[regA] & registers[regB]); break;
                    case 6: registers[dest] = (byte)~(registers[regA] ^ registers[regB]); break;
                    case 7: registers[dest] = (byte)(registers[regA] & ~registers[regB]); break;
                }
                zeroFlag = registers[dest] == 0;
                carryFlag = false;
                //negativeFlag = (registers[dest] & 0b_10000000) == 0b_10000000;
                programCounter++;
                break;
            //RSH
            case 15:
                registers[dest] = (byte)(registers[regA] >> 1);
                programCounter++;
                break;
            default: GD.Print("Invalid opcode in file. This should be impossible."); break;
        }

        registers[0] = 0;
    }

    private void StartStop()
    {
        paused = !paused;
        StartStopButton.Text = paused ? "-Start-" : "-Stop-";
        StatusLabel.Text = "";
    }

    private void Step()
    {
        if (!paused) StartStop();
        if (paused && bytecode != null) RunNextInstruction();
    }

    private void Reset()
    {
        programCounter = 0;
        ram = new byte[ramSize];
        registers = new byte[registerCount];
        ports = new byte[portCount];
        addressStack = new Stack<int>();
        StatusLabel.Text = "";
        UpdateVisualisers();
    }

    private void LoadProgram(string[] files)
    {
        Reset();
        try
        {
            var code = FileAccess.GetFileAsString(files[0]);
            code = Regex.Replace(code, @"\t|\n|\r", "");
            bytecode = ConvertBinaryStringToByteArray(code);

            StatusLabel.Text = "Program Loaded";

            display.DisplayInit();
        } catch
        {
            StatusLabel.Text = "Failed to Load Program";
        }
    }

    private void UpdateVisualisers()
    {
        FlagsDisplay.Text = "Flags | C:" + (carryFlag ? 1 : 0) + " Z:" + (zeroFlag ? 1 : 0);
        string text = BitConverter.ToString(registers).Replace("-", " ");
        RegisterDisplay.Text = text;
        text = BitConverter.ToString(ram).Replace("-", " ");
        MemoryDisplay.Text = text;
        text = BitConverter.ToString(ports).Replace("-", " ");
        PortDisplay.Text = text;
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
