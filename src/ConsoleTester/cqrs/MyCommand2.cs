using DotnetDispatcher.Core;

namespace ConsoleTester.cqrs;

public record MyCommand2(string Value) : ICommand;