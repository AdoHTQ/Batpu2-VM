using Godot;
using System;

public partial class AssemblyView : CodeEdit
{
    [ExportGroup("Syntax colors")]
    [Export] private Color comments;
    [Export] private Color labels;
    [Export] private Color registers;

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
}
