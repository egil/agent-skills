using System.Reflection;
using Orleans.Serialization.Invocation;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Hand-rolled fake of <see cref="IIncomingGrainCallContext"/> exposing only the surface
/// the <see cref="GrainCallCollectionFilter"/> reads: <see cref="InterfaceType"/>,
/// <see cref="InterfaceName"/>, <see cref="MethodName"/>, <see cref="TargetId"/>,
/// and <see cref="Invoke"/>. All other members throw <see cref="NotImplementedException"/>.
/// </summary>
internal sealed class FakeIncomingGrainCallContext : IIncomingGrainCallContext
{
    /// <summary>Gets the grain interface type for this call.</summary>
    public required GrainInterfaceType InterfaceType { get; init; }

    /// <summary>Gets the grain interface name for this call.</summary>
    public required string InterfaceName { get; init; }

    /// <summary>Gets the method name for this call.</summary>
    public required string MethodName { get; init; }

    /// <summary>Gets the target grain id for this call.</summary>
    public required GrainId TargetId { get; init; }

    /// <summary>Optional callback to execute when <see cref="Invoke"/> is called.</summary>
    public Func<Task>? OnInvoke { get; init; }

    /// <summary>Tracks how many times <see cref="Invoke"/> was called.</summary>
    public int InvokeCount { get; private set; }

    /// <inheritdoc />
    public Task Invoke()
    {
        InvokeCount++;
        return OnInvoke?.Invoke() ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    public IInvokable Request => throw new NotImplementedException();

    /// <inheritdoc />
    public object Grain => throw new NotImplementedException();

    /// <inheritdoc />
    public GrainId? SourceId => null;

    /// <inheritdoc />
    public MethodInfo InterfaceMethod => throw new NotImplementedException();

    /// <inheritdoc />
    public object? Result
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

#pragma warning disable CS8769 // Nullability mismatch on Response setter
    /// <inheritdoc />
    Response IGrainCallContext.Response
    {
        get => throw new NotImplementedException();
        set
        {
            _ = value;
            throw new NotImplementedException();
        }
    }
#pragma warning restore CS8769

    /// <inheritdoc />
    IGrainContext IIncomingGrainCallContext.TargetContext => throw new NotImplementedException();

    /// <inheritdoc />
    public MethodInfo ImplementationMethod => throw new NotImplementedException();
}
