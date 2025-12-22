using GCode.Core.Models;

namespace GCode.Core.Services;

/// <summary>
/// WCS 순서 최적화 서비스
/// </summary>
public class WcsOptimizer
{
    private readonly OperationParser _parser = new();

    /// <summary>
    /// WCS Zig-zag 최적화 적용
    /// 짝수 번째 공정의 WCS 블록 순서를 역순으로 재정렬
    /// </summary>
    public string OptimizeZigZag(string gcode)
    {
        var file = _parser.Parse(gcode);
        
        if (file.Operations.Count == 0)
            return gcode;

        // 짝수 번째 공정의 WCS 블록 역순 정렬
        // i=0(N1:홀수), i=1(N2:짝수) ...
        for (int i = 0; i < file.Operations.Count; i++)
        {
            var op = file.Operations[i];
            if ((i + 1) % 2 == 0) // 짝수 번째 공정
            {
                if (op.WcsBlocks.Count > 1)
                {
                    op.WcsBlocks.Reverse();
                }
            }
        }

        return file.Rebuild();
    }

    /// <summary>
    /// 최적화 미리보기 정보 생성
    /// </summary>
    public OptimizationPreview GetPreview(string gcode)
    {
        var file = _parser.Parse(gcode);
        var preview = new OptimizationPreview
        {
            TotalOperations = file.Operations.Count
        };

        for (int i = 0; i < file.Operations.Count; i++)
        {
            var op = file.Operations[i];
            preview.Operations.Add(new OperationInfo
            {
                Index = op.Index > 0 ? op.Index : (i + 1),
                Name = op.Name,
                ToolNumber = op.ToolNumber,
                WcsCount = op.WcsBlocks.Count,
                WillReverse = ((i + 1) % 2 == 0) && op.WcsBlocks.Count > 1
            });
        }

        return preview;
    }
}

/// <summary>
/// 최적화 미리보기 정보
/// </summary>
public class OptimizationPreview
{
    public int TotalOperations { get; set; }
    public List<OperationInfo> Operations { get; set; } = new();
}

/// <summary>
/// 공정 정보
/// </summary>
public class OperationInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ToolNumber { get; set; } = string.Empty;
    public int WcsCount { get; set; }
    public bool WillReverse { get; set; }
}
