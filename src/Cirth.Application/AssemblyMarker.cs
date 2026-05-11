using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Cirth.Application.Tests")]

namespace Cirth.Application;

/// <summary>
/// Tipo marcador usado para descoberta de assembly por MediatR, FluentValidation, etc.
/// Não adicione lógica aqui.
/// </summary>
public sealed class AssemblyMarker;
