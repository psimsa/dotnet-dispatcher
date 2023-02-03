using DotnetDispatcher.Core;

namespace ConsoleTester.cqrs;

public record MyQuery1(int Id) : IQuery<MyQuery1Response>;