namespace GCode.Core.Models;

/// <summary>
/// WCS(Work Coordinate System) 블록을 나타내는 모델
/// </summary>
public class WcsBlock
{
    /// <summary>WCS 코드 ("G54", "G55", "G56", "G57")</summary>
    public string Wcs { get; set; } = string.Empty;
    
    /// <summary>시작 라인 번호 (1-indexed)</summary>
    public int StartLine { get; set; }
    
    /// <summary>종료 라인 번호 (1-indexed)</summary>
    public int EndLine { get; set; }
    
    /// <summary>해당 WCS 내 코드 라인들</summary>
    public List<string> Lines { get; set; } = new();
    
    public override string ToString() => $"{Wcs} (Lines {StartLine}-{EndLine})";
}
