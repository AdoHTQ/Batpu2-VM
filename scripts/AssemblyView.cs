using Godot;
using System;
using System.Collections.Generic;

public partial class AssemblyView : CodeEdit
{
    [ExportGroup("Syntax colors")]
    [Export] private Color comments;
    [Export] private Color labels;
    [Export] private Color registers;

    private List<int> codeLines;

    public int programCounter
    {
        get {return programCounter;}
        private set {
            programCounter = value;
            MoveCursor(programCounter);
        }
    }

    public override void _Ready()
    {
        CodeHighlighter highlighter = SyntaxHighlighter as CodeHighlighter;
        highlighter.AddColorRegion("//", "", comments);
        highlighter.AddColorRegion(".", " ", labels);
        for(int i = 0; i < 16; i++)
        {
            highlighter.AddKeywordColor("r" + i, registers);
        }

        SetLineAsExecuting(6, true);
    }

    public void LoadAssembly(string assembly)
    {
        Text = assembly;
    }

    public void MoveCursor(int line)
    {

    }
}
