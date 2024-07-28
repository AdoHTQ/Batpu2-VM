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
		string scriptPath = @"assembler/main.py";
		string[] scriptArgs = {assemblyFilename, outputFilename};

		ProcessStartInfo start = new ProcessStartInfo
		{
			FileName = pythonPath,
			Arguments = $"{scriptPath} {string.Join(" ", scriptArgs)}",
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

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
