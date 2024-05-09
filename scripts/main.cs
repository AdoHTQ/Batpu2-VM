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
    [Export] private int portCount = 16;
    [Export] private int registerCount = 16;
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
    //private byte[] ports;
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
    }

    private void ProcessOpcode(ushort instruction)
    {
        byte opcode = (byte)(instruction >> (16 - opcodeLength));
        byte dest = (byte)((instruction & 0b111100000000) >> 8);
        byte regA = (byte)((instruction & 0b11110000) >> 4);
        byte regB = (byte)(instruction & 0b1111);

        byte immediate = (byte)(instruction & 0b11111111);

        ushort address = (ushort)(instruction & 0b1111111111);

        byte cond = (byte)((instruction & 0b110000000000) >> 10);

        var offset = 0;
        byte memAddress = 0;

        switch (opcode)
        {
            //NOP
            case 0: programCounter++; break;
            //HLT
            case 1:
                if (!paused) StartStop(); 
                break;
            //ADD
            case 2:
                carryFlag = (int)registers[regA] + (int)registers[regB] > 255;
                registers[dest] = (byte)(registers[regA] + registers[regB]);
                zeroFlag = registers[dest] == 0;
                //negativeFlag = (registers[dest] & 0b_10000000) == 0b_10000000;
                programCounter++;
                break;
            //SUB
            case 3:
                carryFlag = (int)registers[regA] - (int)registers[regB] < 0;
                registers[dest] = (byte)(registers[regA] - registers[regB]);
                zeroFlag = registers[dest] == 0;
                //negativeFlag = (registers[dest] & 0b_10000000) == 0b_10000000;
                programCounter++;
                break;
            //NOR
            case 4:
                Bitwise(4, dest, regA, regB);
                break;
            //AND
            case 5:
                Bitwise(1, dest, regA, regB);
                break;
            //XOR
            case 6:
                Bitwise(2, dest, regA, regB);
                break;
            //RSH
            case 7:
                registers[dest] = (byte)(registers[regA] >> 1);
                programCounter++;
                break;
            //LDI
            case 8:
                registers[dest] = immediate;
                programCounter++;
                break;
            //ADI
            case 9:
                carryFlag = (int)registers[dest] + (int)immediate > 255;
                registers[dest] += immediate;
                zeroFlag = registers[dest] == 0;
                //negativeFlag = (registers[dest] & 0b_10000000) == 0b_10000000;
                programCounter++;
                break;
            //JMP
            case 10:
                programCounter = address;
                break;
            //BRH
            case 11:
                if ((cond == 0 && zeroFlag) || (cond == 1 && !zeroFlag) || (cond == 2 && !zeroFlag && carryFlag) || (cond == 3 && !carryFlag)) programCounter = address;
                else programCounter++;
                break;
            //CAL
            case 12:
                addressStack.Push(programCounter + 1);
                programCounter = address;
                break;
            //RET
            case 13:
                programCounter = addressStack.Pop();
                break;
            //LOD
            case 14:
                offset = regB & 0b111 + (((regB & 0b1000) >> 3) == 1 ? -8 : 0);
                memAddress = (byte)(registers[regA] + offset);
                if (memAddress < ramSize - portCount) registers[dest] = ram[memAddress];
                else registers[dest] = display.LoadPort(memAddress);
                programCounter++;
                break;
            //STR
            case 15:
                offset = regB & 0b111 + (((regB & 0b1000) >> 3) == 1 ? -8 : 0);
                memAddress = (byte)(registers[regA] + offset);
                if (memAddress < ramSize - portCount) ram[memAddress] = registers[dest];
                else display.StorePort(memAddress, registers[dest]);
                programCounter++;
                break;
            default: GD.Print("Invalid opcode in file. This should be impossible."); break;
        }

        registers[0] = 0;
    }

    private void Bitwise(byte operation, byte dest, byte regA, byte regB)
    {
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
        //ports = new byte[portCount];
        addressStack = new Stack<int>();
        StatusLabel.Text = "";
        UpdateVisualisers();
        if (!display.displayInitialized) return;
        display.ClearBuffer();
        display.PushBuffer();
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
        FlagsDisplay.Text = "Flags\n Carry: " + carryFlag + " | Zero: " + zeroFlag;
        //string text = BitConverter.ToString(registers).Replace("-", " ");
        string text = "Registers\n";
        for (int i = 0; i < registerCount; i++)
        {
            text += "" + (i < 10 ? " " : "") + "r" + i + ":" + registers[i];
            if ((i - 3) % 4 != 0) text += " | ";
            else text += "\n";
        }
        RegisterDisplay.Text = text;
        text = BitConverter.ToString(ram).Replace("-", " ");
        MemoryDisplay.Text = text;
        //text = BitConverter.ToString(ports).Replace("-", " ");
        //PortDisplay.Text = text;
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
