

# DotNet Dispatcher Repository Overview

## Purpose
**DotNet Dispatcher** is a lightweight, compile-time generated CQRS dispatcher for .NET that eliminates runtime reflection, making it highly AOT-friendly and performance-efficient. It leverages Roslyn code generation to automatically generate dispatchers at compile time, providing a zero-reflection, high-performance alternative to traditional CQRS implementations like MediatR.

## General Setup
The repository is organized with the following key components:

- **src/DotnetDispatcher**: Contains the core dispatcher interfaces and base classes
- **src/DotnetDispatcher.Generator**: Contains the Roslyn source generator that creates the dispatcher implementations
- **test**: Contains unit tests for the dispatcher functionality
- **build**: Contains build scripts and configuration using Nuke build system
- **.github/workflows**: Contains GitHub Actions workflows for CI/CD

## Repository Structure
```
dotnet-dispatcher/
├── .github/                  # GitHub Actions workflows
│   └── workflows/           # CI/CD pipelines
│       ├── Build_main_and_publish_to_nuget.yml
│       ├── Continuous_build.yml
│       └── Manual_publish_to_Github_Nuget.yml
├── .nuke/                    # Nuke build system configuration
├── build/                    # Build scripts and configuration
├── src/                      # Source code
│   ├── DotnetDispatcher/     # Core dispatcher library
│   └── DotnetDispatcher.Generator/ # Roslyn source generator
├── test/                     # Test projects
│   ├── DotnetDispatcher.Tests/
│   └── DotnetDispatcher.Tests.Domain/
├── README.md                 # Project documentation
└── .editorconfig             # Code style configuration
```

## CI/CD Pipelines
The repository includes three main GitHub Actions workflows:

1. **Continuous_build.yml**: Runs on every push, performing:
   - Clean build
   - Compilation
   - Unit testing
   - Packaging

2. **Build_main_and_publish_to_nuget.yml**: Triggers on version tags (v*), performing:
   - Clean build
   - Compilation
   - Packaging
   - Publishing to GitHub NuGet
   - Artifact publishing

3. **Manual_publish_to_Github_Nuget.yml**: Manual trigger for:
   - Packaging
   - Publishing to GitHub NuGet
   - Artifact publishing

## Code Quality
- Uses Nuke build system for consistent build processes
- Includes .editorconfig for C# code style enforcement
- Follows Roslyn analyzer rules for code quality
- Comprehensive unit test coverage

