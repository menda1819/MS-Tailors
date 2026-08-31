using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MongoDB.Bson;
using MongoDB.Driver;
using MSTailors.Infrastructure.Persistence;

namespace MSTailors.Tests.Fixtures;

/// <summary>
/// Starts a real mongod for the test run and shuts it down afterwards.
/// </summary>
/// <remarks>
/// Repository code is mostly filter and update expressions that the driver
/// translates to BSON. A mocked IMongoCollection would assert that we built the
/// object we intended to build, not that MongoDB agrees with it — which is the
/// only interesting question. So these tests run against a real server.
///
/// The binary lives in the gitignored tools/mongodb directory. Set
/// MSTAILORS_MONGOD_PATH to point somewhere else.
/// </remarks>
public sealed class MongoServerFixture : IAsyncLifetime
{
    private Process? _process;
    private string _dataDirectory = string.Empty;
    private string _logPath = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;

    public IMongoClient Client { get; private set; } = null!;

    /// <summary>Set when mongod could not be found, so tests can skip with a useful message.</summary>
    public string? SkipReason { get; private set; }

    public async Task InitializeAsync()
    {
        var mongodPath = ResolveMongodPath();

        if (mongodPath is null)
        {
            SkipReason =
                "mongod was not found. Expected it at <repo>/tools/mongodb/bin/mongod, or set " +
                "MSTAILORS_MONGOD_PATH. See README.md > Running the tests.";
            return;
        }

        var port = FindFreePort();
        _dataDirectory = Path.Combine(Path.GetTempPath(), $"mstailors-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dataDirectory);

        _logPath = Path.Combine(_dataDirectory, "mongod.log");

        // mongod logs to a file rather than to stdout. Redirecting its output to
        // a pipe we never drain would deadlock the server the moment the 64KB
        // pipe buffer filled, which looks exactly like a hung test run.
        _process = Process.Start(new ProcessStartInfo
        {
            FileName = mongodPath,
            Arguments = string.Join(' ',
                $"--dbpath \"{_dataDirectory}\"",
                $"--logpath \"{_logPath}\"",
                $"--port {port}",
                "--bind_ip 127.0.0.1",
                "--noauth",
                "--quiet",
                // Smallest cache the engine accepts, so several test runs can
                // coexist without competing for memory.
                "--wiredTigerCacheSizeGB 0.25"),
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException($"Could not start mongod at {mongodPath}");

        ConnectionString = $"mongodb://127.0.0.1:{port}/?directConnection=true";

        var settings = MongoClientSettings.FromConnectionString(ConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
        Client = new MongoClient(settings);

        await WaitUntilReadyAsync();
    }

    private async Task WaitUntilReadyAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(45);
        Exception? last = null;

        while (DateTime.UtcNow < deadline)
        {
            if (_process is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"mongod exited with code {_process.ExitCode} during startup. {ReadLogTail()}");
            }

            try
            {
                await Client.GetDatabase("admin")
                    .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(250);
            }
        }

        throw new TimeoutException(
            $"mongod did not become ready within 45 seconds. Last error: {last?.Message}. {ReadLogTail()}");
    }

    private string ReadLogTail()
    {
        if (string.IsNullOrEmpty(_logPath) || !File.Exists(_logPath))
        {
            return string.Empty;
        }

        try
        {
            var lines = File.ReadLines(_logPath).TakeLast(20);
            return "mongod log tail: " + string.Join(Environment.NewLine, lines);
        }
        catch (IOException)
        {
            return string.Empty;
        }
    }

    /// <summary>A context over a database unique to the calling test class.</summary>
    public IMongoContext CreateContext(string databaseName) => new MongoContext(Client, databaseName);

    public async Task DropDatabaseAsync(string databaseName) => await Client.DropDatabaseAsync(databaseName);

    private static string? ResolveMongodPath()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("MSTAILORS_MONGOD_PATH");
        if (!string.IsNullOrWhiteSpace(fromEnvironment) && File.Exists(fromEnvironment))
        {
            return fromEnvironment;
        }

        // Walk up from the test assembly until the repo root is found.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tools", "mongodb", "bin", "mongod");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async Task DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
            catch (InvalidOperationException)
            {
                // Already gone — nothing to clean up.
            }
        }

        _process?.Dispose();

        if (!string.IsNullOrEmpty(_dataDirectory) && Directory.Exists(_dataDirectory))
        {
            try
            {
                Directory.Delete(_dataDirectory, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temp directory is not worth failing a test run over.
            }
        }
    }
}

/// <summary>Shares one mongod across every test class in the collection.</summary>
[CollectionDefinition(Name)]
public sealed class MongoCollection : ICollectionFixture<MongoServerFixture>
{
    public const string Name = "mongo";
}
