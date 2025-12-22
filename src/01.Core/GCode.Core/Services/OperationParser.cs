using System.Text.RegularExpressions;
using GCode.Core.Models;

namespace GCode.Core.Services;

/// <summary>
/// G코드를 파싱하여 공정 블록으로 분리하는 서비스 (개선된 버전)
/// </summary>
public class OperationParser
{
    // 공정 구분자 (N번호 우선, 아니면 OPERATION 주석)
    private static readonly Regex OpStartPattern = 
        new(@"^(\s*N(\d+))|(\(\s*OPERATION\s*(\d+))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly Regex ToolPattern = 
        new(@"\(\s*TOOL\s*(\d+)\s*:\s*(.+?)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // WCS 패턴 (G54~G59, G54.1 Pn) - 잼 코드를 위해 \b 제거
    private static readonly Regex WcsPattern = 
        new(@"(?<![A-Z0-9])G(5[4-9]|54\.1)(?:\s*P(\d+))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // 푸터 감지용 - 잼 코드를 위해 \b 제거 (M01, M5, M9, G28 등)
    private static readonly Regex FooterRegex = 
        new(@"(?<![A-Z0-9])(M0?[01259]|G28|G30|M30|M99)(?![0-9])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ToolNumberPattern = 
        new(@"(?<![A-Z0-9])T(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// G코드 텍스트를 파싱하여 GCodeFile 객체 생성 (안정화 버전)
    /// </summary>
    public GCodeFile Parse(string gcode)
    {
        var gcodeFile = new GCodeFile();
        var lines = gcode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        OperationBlock? currentBlock = null;
        bool firstOpFound = false;

        // 공정 분리용 패턴: N번호로 시작하거나 M6(공구 교환)이 포함된 경우
        // (OPERATION) 주석으로 분리하면 WCS마다 주석을 넣는 CAM에서 공정이 너무 잘게 쪼개짐
        var splitPattern = new Regex(@"^(\s*N(\d+))|(\bM0?6\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            var match = splitPattern.Match(line);

            if (match.Success)
            {
                // 이전 공정 마무리
                if (currentBlock != null)
                {
                    ParseWcsBlocks(currentBlock);
                    gcodeFile.Operations.Add(currentBlock);
                }

                // 새 공정 시작
                int index = 0;
                if (match.Groups[2].Success) int.TryParse(match.Groups[2].Value, out index);

                currentBlock = new OperationBlock
                {
                    Index = index,
                    StartLine = i + 1
                };
                firstOpFound = true;
            }

            if (!firstOpFound)
            {
                // 첫 공정(N1 등)이 나오기 전까지는 파일 헤더로 취급
                gcodeFile.LeadingLines.Add(line);
            }
            else
            {
                // 모든 라인을 RawLines에 추가 (분리 트리거가 된 라인 포함!)
                currentBlock!.RawLines.Add(line);
                
                // 공구 정보는 수집만 해둠 (UI 표시용)
                var toolMatch = ToolPattern.Match(line);
                if (toolMatch.Success)
                {
                    currentBlock.ToolNumber = $"T{toolMatch.Groups[1].Value}";
                    currentBlock.ToolDescription = toolMatch.Groups[2].Value.Trim();
                }
                else if (string.IsNullOrEmpty(currentBlock.ToolNumber))
                {
                    var tNumMatch = ToolNumberPattern.Match(line);
                    if (tNumMatch.Success) currentBlock.ToolNumber = $"T{tNumMatch.Groups[1].Value}";
                }

                // 공정 이름 업데이트 (주석에서 추출)
                if (line.Contains("OPERATION", StringComparison.OrdinalIgnoreCase))
                {
                    var nameMatch = Regex.Match(line, @":\s*(.+?)\s*\)");
                    if (nameMatch.Success) currentBlock.Name = nameMatch.Groups[1].Value;
                }
            }
        }

        // 마지막 공정 마무리
        if (currentBlock != null)
        {
            ParseWcsBlocks(currentBlock);
            gcodeFile.Operations.Add(currentBlock);
        }

        return gcodeFile;
    }

    /// <summary>
    /// 공정 내 라인들을 Header, WcsBlocks, Footer로 상세 분류
    /// </summary>
    private void ParseWcsBlocks(OperationBlock block)
    {
        block.HeaderLines.Clear();
        block.WcsBlocks.Clear();
        block.FooterLines.Clear();

        WcsBlock? currentWcs = null;
        bool wcsStarted = false;
        bool inFooter = false;

        foreach (var line in block.RawLines)
        {
            var wcsMatch = WcsPattern.Match(line);
            var isFooterTrigger = FooterRegex.IsMatch(line);

            // WCS 시작 조건
            if (wcsMatch.Success && !isFooterTrigger)
            {
                // 이전 WCS 블록 저장
                if (currentWcs != null)
                {
                    block.WcsBlocks.Add(currentWcs);
                }

                wcsStarted = true;
                inFooter = false;

                string wcsName = $"G{wcsMatch.Groups[1].Value}";
                if (wcsMatch.Groups[2].Success) wcsName += $" P{wcsMatch.Groups[2].Value}";

                currentWcs = new WcsBlock { Wcs = wcsName };
            }
            // 푸터 시작 조건 (WCS가 이미 시작된 이후에만 푸터로 간주)
            else if (isFooterTrigger && wcsStarted)
            {
                if (currentWcs != null)
                {
                    block.WcsBlocks.Add(currentWcs);
                    currentWcs = null;
                }
                inFooter = true;
            }

            // 라인 분류 작업
            if (!wcsStarted)
            {
                block.HeaderLines.Add(line);
            }
            else if (inFooter)
            {
                block.FooterLines.Add(line);
            }
            else if (currentWcs != null)
            {
                currentWcs.Lines.Add(line);
            }
            else
            {
                // 예외 상황 방어: WCS가 끝났는데 푸터 감지가 안 된 경우 등
                block.FooterLines.Add(line);
            }
        }

        // 마지막 WCS 블록 저장
        if (currentWcs != null)
        {
            block.WcsBlocks.Add(currentWcs);
        }
    }
}
