namespace GCode.Core.Models;

/// <summary>
/// G코드 공정 블록을 나타내는 모델
/// </summary>
public class OperationBlock
{
    /// <summary>공정 순번 (1, 2, 3...)</summary>
    public int Index { get; set; }
    
    /// <summary>공정 이름 ("HOLES", "CONTOUR" 등)</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>공구 번호 ("T12", "T17" 등)</summary>
    public string ToolNumber { get; set; } = string.Empty;
    
    /// <summary>공구 설명 ("3.4 DRILL", "5. FINISH ENDMILL" 등)</summary>
    public string ToolDescription { get; set; } = string.Empty;
    
    /// <summary>시작 라인 번호 (1-indexed)</summary>
    public int StartLine { get; set; }
    
    /// <summary>종료 라인 번호 (1-indexed)</summary>
    public int EndLine { get; set; }
    
    /// <summary>공정 시작부터 첫 WCS 이전까지의 헤더 라인들</summary>
    public List<string> HeaderLines { get; set; } = new();
    
    /// <summary>마지막 WCS 이후부터 공정 종료까지의 푸터 라인들</summary>
    public List<string> FooterLines { get; set; } = new();
    
    /// <summary>원본 코드 라인들 (파싱용)</summary>
    public List<string> RawLines { get; set; } = new();
    
    /// <summary>WCS 블록 목록</summary>
    public List<WcsBlock> WcsBlocks { get; set; } = new();
    
    public override string ToString() => $"Op{Index}: {Name} ({ToolNumber})";
}
