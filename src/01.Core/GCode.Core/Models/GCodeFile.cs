namespace GCode.Core.Models;

/// <summary>
/// G코드 파일 전체 구조를 나타내는 모델
/// </summary>
public class GCodeFile
{
    /// <summary>첫 공정 이전의 헤더 라인들 (%, O-번호 등)</summary>
    public List<string> LeadingLines { get; set; } = new();
    
    /// <summary>분리된 공정 블록들</summary>
    public List<OperationBlock> Operations { get; set; } = new();
    
    /// <summary>마지막 공정 이후의 라인들 (%, M30 등)</summary>
    public List<string> TrailingLines { get; set; } = new();
    
    /// <summary>
    /// 파일 전체를 다시 문자열로 조합 (중복 및 유실 방지)
    /// </summary>
    public string Rebuild()
    {
        var result = new List<string>();
        
        // 1. 선두 라인들
        result.AddRange(LeadingLines);
        
        // 2. 각 공정 블록들
        foreach (var op in Operations)
        {
            // 헤더 (초기 설정 등)
            result.AddRange(op.HeaderLines);
            
            // WCS 블록들 (Zig-zag 적용 시 순서가 뒤집힘)
            foreach (var wcs in op.WcsBlocks)
            {
                result.AddRange(wcs.Lines);
            }
            
            // 푸터 (공정 종료 및 복귀 코드 등)
            result.AddRange(op.FooterLines);
        }
        
        // 3. 후미 라인들
        result.AddRange(TrailingLines);
        
        return string.Join("\n", result);
    }
}
