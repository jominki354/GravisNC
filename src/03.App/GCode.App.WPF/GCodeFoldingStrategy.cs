using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace GCode.App.WPF;

/// <summary>
/// G-코드의 N-번호를 기준으로 접기(Folding) 영역을 계산하는 전략
/// </summary>
public class GCodeFoldingStrategy
{
    // N-번호 시작 패턴 (줄 시작 또는 공백 뒤에 N숫자)
    private static readonly Regex NNumberPattern = new Regex(@"^(\s*N(\d+))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 문서 내의 모든 폴딩 영역을 찾아 리스트로 반환
    /// </summary>
    public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
    {
        firstErrorOffset = -1;
        var newFoldings = new List<NewFolding>();

        int startOffset = -1;
        DocumentLine? previousLine = null;
        string? currentTool = null;
        double minZ = double.MaxValue;
        double maxZ = double.MinValue;
        bool hasZ = false;

        var tRegex = new Regex(@"T\s*(\d+)", RegexOptions.IgnoreCase);
        // Z 뒤에 오는 숫자 (음수, 소수점 포함) 추출
        var zRegex = new Regex(@"Z\s*(-?\d*\.?\d+)", RegexOptions.IgnoreCase);

        foreach (var line in document.Lines)
        {
            int lineOffset = line.Offset;
            int lineLength = line.Length;
            string text = document.GetText(lineOffset, lineLength);

            // N-번호 체크 (블록 시작점)
            var nMatch = NNumberPattern.Match(text);
            if (nMatch.Success)
            {
                // 이전 블록 마감
                if (startOffset != -1 && previousLine != null)
                {
                    string stats = FormatStats(currentTool, hasZ ? minZ : null, hasZ ? maxZ : null);
                    newFoldings.Add(new NewFolding(startOffset, previousLine.Offset + previousLine.Length)
                    {
                        Name = $" ... {stats}"
                    });
                }

                // 새 블록 시작
                // N-번호 라인 자체는 보이게 하고, 그 이후부터 접히도록 설정 (VS Code 스타일)
                startOffset = lineOffset + lineLength; 
                
                currentTool = null;
                minZ = double.MaxValue;
                maxZ = double.MinValue;
                hasZ = false;
            }

            // 블록 내부 정보 수집 (T, Z)
            if (startOffset != -1)
            {
                // 1. 공구 번호 (블록 내 가장 먼저 나오는 T)
                if (currentTool == null)
                {
                    var tMatch = tRegex.Match(text);
                    if (tMatch.Success) currentTool = "T" + tMatch.Groups[1].Value;
                }

                // 2. Z축 좌표 수합
                var zMatches = zRegex.Matches(text);
                foreach (Match zm in zMatches)
                {
                    if (double.TryParse(zm.Groups[1].Value, out double zv))
                    {
                        minZ = Math.Min(minZ, zv);
                        maxZ = Math.Max(maxZ, zv);
                        hasZ = true;
                    }
                }
            }
            previousLine = line;
        }

        // 마지막 블록 처리
        if (startOffset != -1 && previousLine != null)
        {
            string stats = FormatStats(currentTool, hasZ ? minZ : null, hasZ ? maxZ : null);
            newFoldings.Add(new NewFolding(startOffset, previousLine.Offset + previousLine.Length)
            {
                Name = $" ... {stats}"
            });
        }

        // 시작 위치 순으로 정렬하여 반환
        newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return newFoldings;
    }

    private string FormatStats(string? tool, double? minZ, double? maxZ)
    {
        var parts = new List<string>();
        if (tool != null) parts.Add(tool);
        if (minZ.HasValue && maxZ.HasValue)
        {
            // 가공 깊이 정보 (최저 ~ 최고)
            parts.Add($"Z {minZ.Value:F3} ~ {maxZ.Value:F3}");
        }
        
        if (parts.Count == 0) return "[ ... ]";
        return $"[ {string.Join(" | ", parts)} ]";
    }

    /// <summary>
    /// FoldingManager의 정보를 업데이트
    /// </summary>
    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        int firstErrorOffset;
        var foldings = CreateNewFoldings(document, out firstErrorOffset);
        manager.UpdateFoldings(foldings, firstErrorOffset);
    }
}
