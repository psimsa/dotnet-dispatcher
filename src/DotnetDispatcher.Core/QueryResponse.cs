using System;

namespace DotnetDispatcher.Core ;

public record QueryResponse<TResponse>(TResponse? Data, ResponseStatus ResponseStatus = ResponseStatus.Ok, string? Error = null);