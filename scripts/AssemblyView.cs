using Godot;
using GodotPlugins.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

public partial class AssemblyView : CodeEdit
{
	[ExportGroup("References")]
	[Export] private TextEdit lineNums;
	[Export] private Label errorDisplay;
	[Export] private Control errorToggle;
	[Export] private Label filenameDisplay;

	[ExportGroup("Syntax colors")]
	[Export] private Color commentColor;
	//[Export] private Color stringColor;
	[Export] private Color registerColor;
	[Export] private Color instructionColor;
	[Export] private Color conditionColor;

	private List<int> codeLines;

	private List<string> instructions = new List<string> {"NOP", "HLT", "ADD", "SUB", "NOR", "AND", "XOR", "RSH", "LDI", "ADI", "JMP", "BRH", "CAL", "RET", "LOD", "STR", "CMP", "MOV", "LSH", "INC", "DEC", "NOT"};
	private List<string> conditions = new List<string> {"eq", "ne", "ge", "lt", "=", "!=", ">=", "<", "z", "nz", "c", "nc", "zero", "notzero", "carry", "notcarry"};

	public bool initialized {get; private set;}

	public int programCounter;

	public bool follow;

	private string assemblyPath = "";

	public Main main;

	public override void _Process(double delta)
	{
		if (GetExecutingLines().Length > 0 && follow)
		{
			double goal = Math.Clamp(GetExecutingLines()[0], 15.0, Math.Max(15, Text.Split('\n').Length - 15)) - 15;
			ScrollVertical = ScrollVertical + (goal - ScrollVertical) * 0.1;
		}
		
		(lineNums.GetParent() as ScrollContainer).ScrollVertical = (int)(ScrollVertical * 33);
	}
	
	public void Save()
	{
		using (Godot.FileAccess file = Godot.FileAccess.Open(assemblyPath, Godot.FileAccess.ModeFlags.Write))
		{
			file.StoreString(Text);
			file.Close();
		}
		LoadAssembly(assemblyPath);
		Assembler.Assemble(assemblyPath, assemblyPath[..^3] + ".mc");
		main.LoadProgram(new string[]{assemblyPath[..^3] + ".mc"});
	}

	public override void _Ready()
	{
		//Remove line num scroll bar
		foreach (Node child in lineNums.GetChildren(true)) if (child is ScrollBar) lineNums.RemoveChild(child);

		CodeHighlighter highlighter = SyntaxHighlighter as CodeHighlighter;
		highlighter.AddColorRegion("/", "", commentColor, true);
		highlighter.AddColorRegion("#", "", commentColor, true);
		highlighter.AddColorRegion(";", "", commentColor, true);
		//highlighter.AddColorRegion("\"", "\"", stringColor, true);
		for(int i = 0; i < 16; i++)
		{
			highlighter.AddKeywordColor("r" + i, registerColor);
		}
		foreach (string instruction in instructions)
		{
			highlighter.AddKeywordColor(instruction.ToLower(), instructionColor);
			highlighter.AddKeywordColor(instruction.ToUpper(), instructionColor);
		}
		foreach (string condition in conditions)
		{
			highlighter.AddKeywordColor(condition.ToLower(), conditionColor);
			highlighter.AddKeywordColor(condition.ToUpper(), conditionColor);
		}

		codeLines = new List<int>();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton && @event.IsPressed())
		{
			InputEvent evLocal = MakeInputLocal(@event);
			if (!new Rect2(new Vector2(0, 0), Size).HasPoint((evLocal as InputEventMouseButton).Position))
			{
				if ((@event as InputEventMouseButton).ButtonIndex == MouseButton.Left) ReleaseFocus();
			}
			else if ((@event as InputEventMouseButton).ButtonIndex == MouseButton.WheelUp || (@event as InputEventMouseButton).ButtonIndex == MouseButton.WheelDown)
			{
				follow = false;
			}
			else if ((@event as InputEventMouseButton).ButtonIndex == MouseButton.Left)
			{
				follow = false;
			}
		}
		if (@event is InputEventKey && @event.IsPressed())
		{
			if ((@event as InputEventKey).KeyLabel == Key.Escape) ReleaseFocus();
		}
		if (@event.IsActionPressed("save") && assemblyPath.Length != 0) Save();
	}

	public void LoadAssembly(string assembly_filename)
	{
		assemblyPath = assembly_filename;

		filenameDisplay.Text = "Editing " + Path.GetFileName(assembly_filename);
		
		string assembly;
		try
		{
			assembly = Godot.FileAccess.GetFileAsString(assembly_filename);
		} 
		catch
		{
			GD.Print("assembly failed to load");
			return;
		}

		string text = "";
		codeLines = new List<int>();
		int lineNum = 0;
		int pc = 0;
		using (StringReader sr = new StringReader(assembly)) {
			string line;
			while ((line = sr.ReadLine()) != null) {
				line = line.Trim();

				int comment = line.IndexOfAny(new char[]{'/', ';', '#'});
				if (comment != -1) line = line[..comment];

				int labelStart = line.IndexOf('.');
				if (labelStart == 0)
				{
					int labelEnd = line.IndexOfAny(new char[]{' ', '\t'});
					if (labelEnd != -1) line = line[labelEnd..];
					else line = "";
				}

				if (instructions.Any(line.ToUpper().Contains) && !line.StripEdges().ToUpper().StartsWith("DEFINE"))
				{
					codeLines.Add(lineNum);
					text += " " + padString("" + pc, 4, "0");
					pc++;
				}
				text += "\n";
				lineNum++;
			}
		}

		Text = assembly;
		lineNums.Text = text;
		initialized = true;
		Editable = true;

		Assemble(assemblyPath, assemblyPath[..^3] + ".mc");
		main.LoadProgram(new string[]{assemblyPath[..^3] + ".mc"});
	}

	private void Assemble(string inputPath, string outputPath)
	{
		string result = Assembler.Assemble(inputPath, outputPath);
		if (result == "") return;
		errorToggle.Visible = true;
		errorDisplay.Text = result;
	}

	public void HideError()
	{
		errorToggle.Visible = false;
	}

	public void MoveCursor()
	{
		ClearExecutingLines();
		if (programCounter >= codeLines.Count) return;
		SetLineAsExecuting(codeLines[programCounter], true);
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
	private void _on_gui_input(InputEvent @event)
	{
		if (!@event.IsActionPressed("save")) return;
		AcceptEvent();
	}
}
