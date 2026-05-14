namespace YenkoTools.Common.Cqrs.Behaviors.ResultAnalysis;

public interface IResultAnalyzer
{
    ResultAnalysis AnalyzeResult(object? result);
}
