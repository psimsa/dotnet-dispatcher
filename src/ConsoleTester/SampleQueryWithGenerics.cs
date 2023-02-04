using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotnetDispatcher.Core;

namespace ConsoleTester;

public record SampleQueryWithGenerics : IQuery<Result<SampleQueryWithGenerics>>;

public record SampleQueryWithGenericsResponse(string Value);

public record Result<T>(T Data);