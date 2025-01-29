namespace DotnetDispatcher.Tests.Domain;

public record Result<T>(bool IsSuccess, T? Data);

public record Result(bool IsSuccess);
