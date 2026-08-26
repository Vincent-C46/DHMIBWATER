using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Transactions;

/// <summary>Warning 등급 실패를 대화상자 없이 삭제한다. Error 등급은 손대지 않고 그대로 Revit 기본 처리(대화상자)로 넘긴다.</summary>
internal sealed class SuppressWarningFailuresPreprocessor : IFailuresPreprocessor
{
    public List<string> Messages { get; } = new();

    public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
    {
        foreach (var failure in accessor.GetFailureMessages())
        {
            if (failure.GetSeverity() != FailureSeverity.Warning) continue;
            Messages.Add(failure.GetDescriptionText());
            accessor.DeleteWarning(failure);
        }
        return FailureProcessingResult.Continue;
    }
}
