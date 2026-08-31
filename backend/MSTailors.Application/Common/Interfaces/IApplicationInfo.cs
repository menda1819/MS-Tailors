namespace MSTailors.Application.Common.Interfaces;

/// <summary>Build and hosting metadata, supplied by the Api layer.</summary>
public interface IApplicationInfo
{
    string Name { get; }
    string Version { get; }
    string Environment { get; }
}
