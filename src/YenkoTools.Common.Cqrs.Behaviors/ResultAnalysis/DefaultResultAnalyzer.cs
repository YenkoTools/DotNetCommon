using YenkoTools.Common.Cqrs.Results;

namespace YenkoTools.Common.Cqrs.Behaviors.ResultAnalysis;

public class DefaultResultAnalyzer : IResultAnalyzer
{
    public ResultAnalysis AnalyzeResult(object? result)
    {
        if (result == null)
            return new ResultAnalysis(false, "null", null);

        var resultType = result.GetType();

        if (IsResultType(resultType))
            return AnalyzeResultPattern(result, resultType);

        if (HasSuccessProperty(result, resultType, out var isSuccess))
        {
            var innerResultType = isSuccess ? DetermineResultType(result) : "result_failure";
            return new ResultAnalysis(isSuccess, innerResultType, null);
        }

        var directResultType = DetermineResultType(result);
        var itemCount = directResultType == "collection" ? GetCollectionCount(result) : null;
        return new ResultAnalysis(true, directResultType, itemCount);
    }

    private static bool IsResultType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>);

    private ResultAnalysis AnalyzeResultPattern(object result, Type resultType)
    {
        var isSuccess = GetPropertyValue<bool>(result, resultType, "IsSuccess");

        if (isSuccess)
        {
            var value = GetPropertyValue<object>(result, resultType, "Value");
            var innerResultType = DetermineResultType(value);
            var itemCount = innerResultType == "collection" ? GetCollectionCount(result) : null;
            return new ResultAnalysis(true, innerResultType, itemCount);
        }

        return new ResultAnalysis(false, "result_failure", null);
    }

    private static bool HasSuccessProperty(object obj, Type type, out bool isSuccess)
    {
        isSuccess = false;

        if (!type.Name.Contains("Result"))
            return false;

        var property = type.GetProperty("IsSuccess") ?? type.GetProperty("Success") ?? type.GetProperty("Succeeded");
        if (property == null)
            return false;

        isSuccess = (bool)(property.GetValue(obj) ?? false);
        return true;
    }

    private static T? GetPropertyValue<T>(object obj, Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        if (property == null) return default;
        var value = property.GetValue(obj);
        return value is T typedValue ? typedValue : default;
    }

    private static string DetermineResultType(object? value)
    {
        if (value == null) return "null";
        var type = value.GetType();
        if (IsCollection(type)) return "collection";
        if (IsPrimitiveType(type)) return "primitive";
        return "entity";
    }

    private static bool IsCollection(Type type) =>
        typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string);

    private static bool IsPrimitiveType(Type type) =>
        type.IsPrimitive || type == typeof(string) || type == typeof(Guid) ||
        type == typeof(DateTime) || type == typeof(decimal);

    private static int? GetCollectionCount(object? result)
    {
        if (result == null) return null;
        var resultType = result.GetType();

        if (IsResultType(resultType))
        {
            var value = GetPropertyValue<object>(result, resultType, "Value");
            return CountEnumerable(value);
        }

        return CountEnumerable(result);
    }

    private static int? CountEnumerable(object? obj)
    {
        if (obj is System.Collections.IEnumerable enumerable and not string)
            return enumerable.Cast<object>().Count();
        return null;
    }
}
