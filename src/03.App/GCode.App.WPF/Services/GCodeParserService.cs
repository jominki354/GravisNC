using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GCode.App.WPF.Services
{
    public class GCodeBlock
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Header { get; set; } = string.Empty;     // e.g., "T01 - 3.5 DRILL", "Header", "Footer"
        public string Content { get; set; } = string.Empty;    // Raw G-Code Lines
        public int StartLine { get; set; }     // For Sync
        public int EndLine { get; set; }
        public string ToolNumber { get; set; } = string.Empty; // "T14"
        public List<string> Operations { get; set; } = new List<string>(); // "( OPERATION ... )"
        public bool IsOperation { get; set; } = false; // Is this a T# block?
    }

    public class GCodeParserService
    {
        // Detects: "T14", "T01 M6", "T1", "T02" (Captured Group 1 = Number)
        private static readonly Regex ToolRegex = new Regex(@"T(\d+)", RegexOptions.Compiled);
        // Detects: "( OPERATION ... )" or similar comments
        private static readonly Regex OpCommentRegex = new Regex(@"\(\s*OPERATION.*?:?\s*(.*)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public List<GCodeBlock> Parse(string text)
        {
            var blocks = new List<GCodeBlock>();
            if (string.IsNullOrWhiteSpace(text)) return blocks;

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var currentBlock = new GCodeBlock { Header = "Start / Initialize", StartLine = 0, IsOperation = false };
            var buffer = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                // 1. Detect New Operation Block (T# + M6 usually, or just T# change)
                // Strategy: If line implies a tool change or major Op start, split.
                // For robustness: Trigger on "T#" lines.
                bool isToolChange = ToolRegex.IsMatch(trimmed); // Simple check
                
                // If we found a tool change and the buffer has content, finalize previous block
                if (isToolChange && (buffer.Count > 0 || currentBlock.IsOperation))
                {
                    // If current buffer is empty (e.g. T14 immediately follows T13), close previous
                    // If buffer has content, close previous.
                    currentBlock.Content = string.Join(Environment.NewLine, buffer);
                    currentBlock.EndLine = i - 1;
                    blocks.Add(currentBlock);

                    // Start New Block
                    currentBlock = new GCodeBlock 
                    { 
                        StartLine = i, 
                        IsOperation = true,
                        Header = $"Tool Change ({line.Trim()})" // Temporary Title
                    };
                    buffer.Clear();
                }

                buffer.Add(line);

                // 2. Metadata Extraction (Update current block info)
                var toolMatch = ToolRegex.Match(trimmed);
                if (toolMatch.Success)
                {
                    currentBlock.ToolNumber = "T" + toolMatch.Groups[1].Value;
                    if (currentBlock.Header.StartsWith("Tool Change")) 
                         currentBlock.Header = $"{currentBlock.ToolNumber}";
                }

                var opMatch = OpCommentRegex.Match(trimmed);
                if (opMatch.Success)
                {
                    string opName = opMatch.Groups[1].Value.Trim();
                    currentBlock.Operations.Add(opName);
                    // Append Op Name to Header for readability
                    if (!currentBlock.Header.Contains(opName))
                        currentBlock.Header += $" : {opName}";
                }
            }

            // Finalize Last Block
            if (buffer.Count > 0)
            {
                currentBlock.Content = string.Join(Environment.NewLine, buffer);
                currentBlock.EndLine = lines.Length - 1;
                blocks.Add(currentBlock);
            }

            return blocks;
        }

        public string Reconstruct(IEnumerable<GCodeBlock> blocks)
        {
            return string.Join(Environment.NewLine, blocks.Select(b => b.Content));
        }
    }
}
