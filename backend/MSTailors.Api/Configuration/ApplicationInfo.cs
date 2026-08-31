using System.Reflection;
using MSTailors.Application.Common.Interfaces;

namespace MSTailors.Api.Configuration;

/// <inheritdoc />
public sealed class ApplicationInfo : IApplicationInfo
{
    public ApplicationInfo(IWebHostEnvironment environment)
    {
        Environment = environment.EnvironmentName;

        var assembly = Assembly.GetEntryAssembly();

        Name = assembly?.GetName().Name ?? "MSTailors.Api";

        // InformationalVersion carries the git-describe style suffix when the
        // build sets one; AssemblyVersion is the fallback.
        Version = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                  ?? assembly?.GetName().Version?.ToString()
                  ?? "0.0.0";
    }

    public string Name { get; }
    public string Version { get; }
    public string Environment { get; }
}
