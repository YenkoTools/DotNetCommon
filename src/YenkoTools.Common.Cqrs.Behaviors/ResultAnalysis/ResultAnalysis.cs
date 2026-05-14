namespace YenkoTools.Common.Cqrs.Behaviors.ResultAnalysis;

public record ResultAnalysis(bool IsSuccess, string ResultType, int? ItemCount);
