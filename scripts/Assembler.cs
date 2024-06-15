using Godot;
using System;
using System.Collections.Generic;
using System.IO;


class Program
{
    static void assemble(string assembly_filename, string output_filename)
    {
        string[] assembly_lines = File.ReadAllLines(assembly_filename);
        StreamWriter machine_code_file = new StreamWriter(output_filename);

        List<string> lines = new List<string>();
        foreach (string line in assembly_lines)
        {
            lines.Add(line.Trim());
        }

        // Remove comments and blanklines
        lines = new List<string>(lines.ConvertAll(line => line.Split('/')[0]));
        lines.RemoveAll(line => string.IsNullOrWhiteSpace(line));

        // Populate symbol table
        Dictionary<string, int> symbols = new Dictionary<string, int>();

        string[] registers = { "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15" };
        for (int index = 0; index < registers.Length; index++)
        {
            symbols.Add(registers[index], index);
        }

        string[] opcodes = { "nop", "hlt", "add", "sub", "nor", "and", "xor", "rsh", "ldi", "adi", "jmp", "brh", "cal", "ret", "lod", "str" };
        for (int index = 0; index < opcodes.Length; index++)
        {
            symbols.Add(opcodes[index], index);
        }

        string[] conditions = { "eq", "ne", "ge", "lt" };
        string[] conditions2 = { "=", "!=", ">=", "<" };
        string[] conditions3 = { "z", "nz", "c", "nc" };
        string[] conditions4 = { "zero", "notzero", "carry", "notcarry" };
        for (int index = 0; index < conditions.Length; index++)
        {
            symbols.Add(conditions[index], index);
        }
        for (int index = 0; index < conditions2.Length; index++)
        {
            symbols.Add(conditions2[index], index);
        }
        for (int index = 0; index < conditions3.Length; index++)
        {
            symbols.Add(conditions3[index], index);
        }
        for (int index = 0; index < conditions4.Length; index++)
        {
            symbols.Add(conditions4[index], index);
        }

        string[] ports = { "pixel_x", "pixel_y", "draw_pixel", "clear_pixel", "load_pixel", "buffer_screen", "clear_screen_buffer",
                           "write_char", "buffer_chars", "clear_chars_buffer", "show_number", "clear_number", "signed_mode", "unsigned_mode", "rng", "controller_input" };
        for (int index = 0; index < ports.Length; index++)
        {
            symbols.Add(ports[index], index + 240);
        }

        // Add definitions to symbol table
        // expects all definitions to be above assembly
        bool is_definition(string word)
        {
            return word == "define";
        }

        bool is_label(string word)
        {
            return word[0] == '.';
        }

        int offset = 0;
        for (int index = 0; index < lines.Count; index++)
        {
            string[] words = lines[index].ToLower().Split();
            if (is_definition(words[0]))
            {
                symbols.Add(words[1], int.Parse(words[2]));
                offset += 1;
            }
            else if (is_label(words[0]))
            {
                symbols.Add(words[0], index - offset);
            }
        }

        // Generate machine code
        int resolve(string word)
        {
            if (int.TryParse(word, out int result))
            {
                return result;
            }
            if (!symbols.ContainsKey(word))
            {
                Console.WriteLine($"Could not resolve {word}");
                System.Environment.Exit(1);
            }
            return symbols[word];
        }

        for (int i = offset; i < lines.Count; i++)
        {
            string[] words = lines[i].ToLower().Split();

            // Remove label, we have it in symbols now
            if (is_label(words[0]))
            {
                words = new ArraySegment<string>(words, 1, words.Length - 1).ToArray();
            }

            // Pseudo-instructions
            if (words[0] == "cmp")
            {
                words = new[] { "sub", words[1], words[2], registers[0] }; // sub A B r0
            }
            else if (words[0] == "mov")
            {
                words = new[] { "add", words[1], registers[0], words[2] }; // add A r0 dest
            }
            else if (words[0] == "lsh")
            {
                words = new[] { "add", words[1], words[1], words[2] }; // add A A dest
            }
            else if (words[0] == "inc")
            {
                words = new[] { "adi", words[1], "1" }; // adi dest 1
            }
            else if (words[0] == "dec")
            {
                words = new[] { "adi", words[1], "-1" }; // adi dest -1
            }
            else if (words[0] == "not")
            {
                words = new[] { "nor", words[1], registers[0], words[2] }; // nor A r0 dest
            }

            // Begin machine code translation
            string opcode = words[0];
            int machine_code = symbols[opcode] << 12;
            int[] operands = new int[words.Length - 1];
            for (int j = 0; j < operands.Length; j++)
            {
                operands[j] = resolve(words[j + 1]);
            }

            // Number of operands check
            if ((opcode == "nop" || opcode == "hlt" || opcode == "ret") && words.Length != 2)
            {
                Console.WriteLine($"Incorrect number of operands for {opcode} on line {i + 1}");
                System.Environment.Exit(1);
            }

            if ((opcode == "jmp" || opcode == "cal") && words.Length != 3)
            {
                Console.WriteLine($"Incorrect number of operands for {opcode} on line {i + 1}");
                System.Environment.Exit(1);
            }

            if ((opcode == "rsh" || opcode == "ldi" || opcode == "adi" || opcode == "brh") && words.Length != 4)
            {
                Console.WriteLine($"Incorrect number of operands for {opcode} on line {i + 1}");
                System.Environment.Exit(1);
            }

            if ((opcode == "add" || opcode == "sub" || opcode == "nor" || opcode == "and" || opcode == "xor" || opcode == "lod" || opcode == "str") && words.Length != 5)
            {
                Console.WriteLine($"Incorrect number of operands for {opcode} on line {i + 1}");
                System.Environment.Exit(1);
            }

            // Reg A
            if (opcode == "add" || opcode == "sub" || opcode == "nor" || opcode == "and" || opcode == "xor" || opcode == "rsh" || opcode == "ldi" || opcode == "adi" || opcode == "lod" || opcode == "str")
            {
                if (operands[0] != (operands[0] % (1 << 4)))
                {
                    Console.WriteLine($"Invalid reg A for {opcode} on line {i + 1}");
                    System.Environment.Exit(1);
                }
                machine_code |= (operands[0] << 8);
            }

            // Reg B
            if (opcode == "add" || opcode == "sub" || opcode == "nor" || opcode == "and" || opcode == "xor" || opcode == "lod" || opcode == "str")
            {
                if (operands[1] != (operands[1] % (1 << 4)))
                {
                    Console.WriteLine($"Invalid reg B for {opcode} on line {i + 1}");
                    System.Environment.Exit(1);
                }
                machine_code |= (operands[1] << 4);
            }

            // Reg C
            if (opcode == "add" || opcode == "sub" || opcode == "nor" || opcode == "and" || opcode == "xor" || opcode == "rsh")
            {
                if (operands[operands.Length - 1] != (operands[operands.Length - 1] % (1 << 4)))
                {
                    Console.WriteLine($"Invalid reg C for {opcode} on line {i + 1}");
                    System.Environment.Exit(1);
                }
                machine_code |= operands[operands.Length - 1];
            }

            // Immediate
            if (opcode == "ldi" || opcode == "adi")
            {
                if (operands[1] < -128 || operands[1] > 255) // 2s comp [-128, 127] or uint [0, 255]
                {
                    Console.WriteLine($"Invalid immediate for {opcode} on line {i + 1}");
                    System.Environment.Exit(1);
                }
                machine_code |= operands[1] & ((1 << 8) - 1);
            }

            // Instruction memory address
            if (opcode == "jmp" || opcode == "brh" || opcode == "cal")
            {
                if (operands[operands.Length - 1] != (operands[operands.Length - 1] % (1 << 10)))
                {
                    Console.WriteLine($"Invalid instruction memory address for {opcode} on line {i + 1}");
                    System.Environment.Exit(1);
                }
                machine_code |= operands[operands.Length - 1];
            }

            // Condition
            if (opcode == "brh")
            {
                if (operands[0] != (operands[0] % (1 << 2)))
                {
                    Console.WriteLine($"Invalid condition for {opcode} on line {i + 1}");
                    System.Environment.Exit(1);
                }
                machine_code |= (operands[0] << 10);
            }

            // Offset
            if (opcode == "lod" || opcode == "str")
            {
                if (operands[2] < -8 || operands[2] > 7) // 2s comp [-8, 7]
                {
                    Console.WriteLine($"Invalid offset for {opcode} on line {i + 1}");
                    System.Environment.Exit(1);
                }
                machine_code |= operands[2] & ((1 << 4) - 1);
            }

            string as_string = Convert.ToString(machine_code, 2).PadLeft(16, '0');
            machine_code_file.WriteLine(as_string);
        }

        machine_code_file.Close();
    }

    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: assembler <assembly_file> <output_file>");
            return;
        }

        assemble(args[0], args[1]);
    }
}