# DotNet Dispatcher - AOT-Friendly CQRS Dispatcher

## Overview
**DotNet Dispatcher** is a lightweight, compile-time generated CQRS dispatcher for .NET. By leveraging Roslyn code generation, this framework eliminates runtime reflection, making it highly **AOT-friendly** and **performance-efficient**.

With **DotNet Dispatcher**, developers can define **commands and queries** with their corresponding handlers, and the dispatcher is automatically generated at compile time.

## Features
✅ **Zero Runtime Reflection** - Uses Roslyn to generate code at compile time.  
✅ **AOT-Friendly** - Ideal for .NET Native AOT scenarios.  
✅ **Lightweight** - No additional runtime dependencies or complexity.  
✅ **Explicit yet Automated Wiring** - Just define your handlers, and the generator does the rest.  
✅ **Fast & Efficient** - No runtime service lookups; everything is wired at compile time.  

## Installation
```sh
# Clone the repository
$ git clone https://github.com/psimsa/dotnet-dispatcher.git
$ cd dotnet-dispatcher

# Add to your .NET project
$ dotnet add package DotNetDispatcher
```

## Getting Started

### 1. Define a Command & Handler
Create a command by implementing `ICommand<TResponse>` and a corresponding handler:

```csharp
public record PlaceOrderCommand(string ProductId, int Quantity) : ICommand<OrderResponse>;

public class PlaceOrderHandler : ICommandHandler<PlaceOrderCommand, OrderResponse>
{
    public Task<OrderResponse> Handle(PlaceOrderCommand command)
    {
        return Task.FromResult(new OrderResponse(Guid.NewGuid(), "Order Placed Successfully"));
    }
}
```

### 2. Define a Query & Handler
```csharp
public record GetOrderQuery(Guid OrderId) : IQuery<OrderResponse>;

public class GetOrderHandler : IQueryHandler<GetOrderQuery, OrderResponse>
{
    public Task<OrderResponse> Handle(GetOrderQuery query)
    {
        return Task.FromResult(new OrderResponse(query.OrderId, "Order Retrieved"));
    }
}
```

### 3. Use the Dispatcher
After compilation, the **dispatcher is automatically generated** and can be injected like this:

```csharp
public class OrderService
{
    private readonly IDispatcher _dispatcher;
    
    public OrderService(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }
    
    public async Task<OrderResponse> PlaceOrderAsync(string productId, int quantity)
    {
        return await _dispatcher.Send(new PlaceOrderCommand(productId, quantity));
    }
}
```

## How It Works
- **Roslyn Source Generator** scans for commands and queries marked with `[GenerateDispatcher]`.
- The dispatcher implementation is generated **at compile time**, avoiding reflection-based dependency resolution.
- Aside from the generated dispatcher, the generator also creates an extension method for `IServiceCollection` called `RegisterCommandDispatcherAndHandlers` that ensures necessary dependencies **are automatically registered in DI**, allowing easy injection and usage. Simply call `services.RegisterCommandDispatcherAndHandlers()` during your DI setup.

## Benefits Over MediatR
| Feature           | MediatR                     | DotNet Dispatcher |
|------------------|---------------------------|-------------------|
| **Reflection-Free** | ❌ Uses runtime reflection | ✅ No runtime reflection |
| **AOT-Friendly**  | ❌ May have issues         | ✅ Fully compatible |
| **Performance**   | ⚠️ Slight overhead        | ✅ Optimized compile-time wiring |
| **Setup**         | ✅ Easy                     | ✅ Easy (via source generator) |

## Configuration & Customization
You can customize the dispatcher behavior by modifying the source generator logic or extending handler interfaces.

## Contributing
We welcome contributions! Please feel free to:
- Open issues for bugs or feature requests.
- Submit PRs to enhance functionality or improve documentation.

## License
This project is licensed under the **MIT License**.

## Author
Maintained by [psimsa](https://github.com/psimsa).

