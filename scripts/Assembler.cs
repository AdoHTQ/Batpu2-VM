using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;

class Assembler
{
    public static string Assemble(string assemblyFilename, string outputFilename)
    {
        string pythonPath = "python";
        if (OperatingSystem.IsLinux()) pythonPath = "python3";
        if (OperatingSystem.IsMacOS()) pythonPath = "python3";
        
        string scriptPath = @"assembler/assembler.py";

        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = pythonPath,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        start.ArgumentList.Add(scriptPath);
        start.ArgumentList.Add(assemblyFilename);
        start.ArgumentList.Add(outputFilename);

        using (Process process = Process.Start(start))
        {
            using (System.IO.StreamReader reader = process.StandardError)
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }
    }
}
