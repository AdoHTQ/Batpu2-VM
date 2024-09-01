using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;

class Assembler
{
    public static string Assemble(string assemblyFilename, string outputFilename)
    {
        string exePath = "python";
        if (OperatingSystem.IsLinux()) exePath = "python3";
        if (OperatingSystem.IsMacOS()) exePath = "python3";

        if (assemblyFilename.GetExtension() == "ap")
        {
            if (OperatingSystem.IsLinux())
            {
                switch (RuntimeInformation.OSArchitecture)
                {
                    case Architecture.X86: exePath = @"assembler-plus\i686-unknown-linux-gnu\assembler-plus"; break;
                    case Architecture.X64: exePath = @"assembler-plus\x86_64-unknown-linux-gnu\assembler-plus"; break;
                    case Architecture.Arm | Architecture.Arm64: exePath = @"assembler-plus\aarch64-unknown-linux-gnu\assembler-plus"; break;
                    default: exePath = @"assembler-lpus\i686-unknown-linux-gnu\assembler-plus"; break;
                }
            }
            else if (OperatingSystem.IsWindows())
            {
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86: exePath = @"assembler-plus\i686-pc-windows-gnu\assembler-plus.exe"; break;
                    case Architecture.X64: exePath = @"assembler-plus\x86_64-pc-windows-gnu\assembler-plus.exe"; break;
                    default: exePath = @"assembler-plus\i686-pc-windows-gnu\assembler-plus.exe"; break;
                }
            }
            else
            {
                exePath = @"assembler-plus\i686-unknown-linux-gnu\assembler-plus";
            }
        }

        string scriptPath = @"assembler/assembler.py";

        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = exePath,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (assemblyFilename.GetExtension() != "ap")
        {
            start.ArgumentList.Add(scriptPath);
        }
        start.ArgumentList.Add(assemblyFilename);
        start.ArgumentList.Add(outputFilename);
        if (assemblyFilename.GetExtension() == "ap")
        {
            start.ArgumentList.Add(assemblyFilename.GetBaseName() + ".meta");
        }

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
