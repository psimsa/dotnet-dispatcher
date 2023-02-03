using DotnetDispatcher.Core;

namespace ConsoleTester.cqrs;

public record MyQuery2(string Value) : IQuery<MyQuery2Response>;