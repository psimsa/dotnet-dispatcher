using DotnetDispatcher.Core;

namespace ConsoleTester.cqrs;

public record MyCommand1(int Id) : ICommand<MyCommand1Response>;