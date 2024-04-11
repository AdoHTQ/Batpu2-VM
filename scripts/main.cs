using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Text.RegularExpressions;

public partial class main : Node
{
    [Export] private Button StartStopButton;
    [Export] private Button StepButton;
    [Export] private Button ResetButton;
    [Export] private Label StatusLabel;
    [Export] private Label RegisterDisplay;
    [Export] private Label MemoryDisplay;
    [Export] private Label PortDisplay;

    //[Export] private int instructionsPerTick;
    private int programMemorySize = 2048;
    private int ramSize = 256;
    private int portCount = 256;
    private int registerCount = 8;
    private int opcodeLength = 4;

    bool paused = true;
    int programCounter = 0;

    private byte[] bytecode;
    private byte[] ram;
    private byte[] registers;
    private byte[] ports;

    private bool zeroFlag = false;
    private bool negativeFlag = false;

    public override void _Ready()
    {
        ram = new byte[ramSize];
        registers = new byte[registerCount];
        ports = new byte[portCount];

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
        if (bytecode == null || paused) return;
        //GD.Print(programCounter);
        RunNextInstruction();
        UpdateVisualisers();
    }

    private void RunNextInstruction()
    {
        int index = programCounter * 2;
        short instruction = (short)(bytecode[index] << 8 | bytecode[index + 1]);
        ProcessOpcode(instruction);
        programCounter %= programMemorySize / 2;
    }

    private void ProcessOpcode(short instruction)
    {
        int opcode = instruction >> (16 - opcodeLength);
        int address = instruction & 1023;
        int immediate = instruction & 255;
        bool flag = ((instruction & 1024) >> 10) == 1;
        int dest = instruction & 3584 >> 9;
        
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
            //BIF
            case 3:
                if ((!flag && zeroFlag) || (flag && negativeFlag)) programCounter = address;
                else programCounter++;
                break;
            //ADI
            case -5:
                registers[dest] += (byte)immediate;
                if (registers[dest] == 0) zeroFlag = true;
                else zeroFlag = false;
                programCounter++;
                break;
            default: GD.Print("Invalid opcode in file."); break;
        }
    }

    private void StartStop()
    {
        paused = !paused;
        StartStopButton.Text = paused ? "-Start-" : "-Stop-";
        StatusLabel.Text = "";
    }

    private void Step()
    {
        if (paused) RunNextInstruction();
        StatusLabel.Text = "";
    }

    private void Reset()
    {
        programCounter = 0;
        ram = new byte[ramSize];
        registers = new byte[registerCount];
        ports = new byte[portCount];
        StatusLabel.Text = "";
    }

    private void LoadProgram(string[] files)
    {
        var code = FileAccess.GetFileAsString(files[0]);
        code = Regex.Replace(code, @"\t|\n|\r", "");
        bytecode = ConvertBinaryStringToByteArray(code);

        StatusLabel.Text = "Program Loaded";
    }

    private void SetRegister(int register, byte value)
    {
        if (register == 0) return;
        registers[register] = value;
    }

    private byte GetRegister(int register)
    {
        return registers[register];
    }

    private void UpdateVisualisers()
    {
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
