using System.Diagnostics;
using System.Runtime.InteropServices;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.CommandHelpers;

internal class CertificateGenerator : IDisposable
{
    private const string CfsslUrlWindows =
        "https://github.com/cloudflare/cfssl/releases/download/v1.5.0/cfssl_1.5.0_windows_amd64.exe";

    private const string CfsslJsonUrlWindows =
        "https://github.com/cloudflare/cfssl/releases/download/v1.5.0/cfssljson_1.5.0_windows_amd64.exe";

    private const string CfsslUrlLinux =
        "https://github.com/cloudflare/cfssl/releases/download/v1.5.0/cfssl_1.5.0_linux_amd64";

    private const string CfsslJsonUrlLinux =
        "https://github.com/cloudflare/cfssl/releases/download/v1.5.0/cfssljson_1.5.0_linux_amd64";

    private const string CfsslUrlMacOs =
        "https://github.com/cloudflare/cfssl/releases/download/v1.5.0/cfssl_1.5.0_darwin_amd64";

    private const string CfsslJsonUrlMacOs =
        "https://github.com/cloudflare/cfssl/releases/download/v1.5.0/cfssljson_1.5.0_darwin_amd64";

    private const string CaConfig =
        @"{""signing"":{""default"":{""expiry"":""43800h""},""profiles"":{""server"":{""expiry"":""43800h"",""usages"":[""signing"",""key encipherment"",""server auth""]}}}}";

    private const string CaCsr =
        @"{""CN"":""Operator Root CA"",""key"":{""algo"":""rsa"",""size"":2048},""names"":[{""C"":""DEV"",""L"":""Kubernetes"",""O"":""Kubernetes Operator""}]}";

    private readonly TextWriter _appOut;
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private bool _initialized;
    private string? _cfssl;
    private string? _cfssljson;
    private string? _caconfig;
    private string? _cacsr;
    private string? _servercsr;

    public CertificateGenerator(TextWriter appOut)
    {
        _appOut = appOut;
    }

    private static string ShellExecutor => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "cmd.exe"
        : "/bin/sh";

    public void Dispose()
    {
        Delete(_caconfig);
        Delete(_cacsr);
        Delete(_cfssl);
        Delete(_cfssljson);
        Delete(_servercsr);
    }

    public async Task CreateCaCertificateAsync(string outputFolder)
    {
        if (!_initialized)
        {
            await PrepareExecutables();
            _initialized = true;
        }

        Directory.CreateDirectory(outputFolder);

        await _appOut.WriteLineAsync($@"Generating certificates to ""{outputFolder}"".");
        await _appOut.WriteLineAsync("Generating CA certificate.");
        await ExecuteProcess(
            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = outputFolder,
                    FileName = ShellExecutor,
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? $"/c {_cfssl} gencert -initca {_cacsr} | {_cfssljson} -bare ca -"
                        : $@"-c ""{_cfssl} gencert -initca {_cacsr} | {_cfssljson} -bare ca -""",
                },
            });

        await ListDir(outputFolder);
    }

    public async Task CreateServerCertificateAsync(
        string outputFolder,
        string name,
        string @namespace,
        string caPath,
        string caKeyPath)
    {
        if (!_initialized)
        {
            await PrepareExecutables();
            _initialized = true;
        }

        Directory.CreateDirectory(outputFolder);

        _servercsr = Path.Join(_tempDirectory, Path.GetRandomFileName());
        await using (var serverCsrStream = new StreamWriter(new FileStream(_servercsr, FileMode.CreateNew)))
        {
            await serverCsrStream.WriteLineAsync(
                ServerCsr($"{name}.{@namespace}.svc", $"*.{@namespace}.svc", "*.svc"));
        }

        await _appOut.WriteLineAsync(
            $@"Generating server certificate for ""{name}"" in namespace ""{@namespace}"".");
        await ExecuteProcess(
            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = outputFolder,
                    FileName = ShellExecutor,
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? $"/c {_cfssl} gencert -ca=file:{caPath.Replace('\\', '/')} -ca-key=file:{caKeyPath.Replace('\\', '/')} -config={_caconfig} -profile=server {_servercsr} | {_cfssljson} -bare server -"
                        : $@"-c ""{_cfssl} gencert -ca={caPath} -ca-key={caKeyPath} -config={_caconfig} -profile=server {_servercsr} | {_cfssljson} -bare server -""",
                },
            });

        await ListDir(outputFolder);
    }

    private static string ServerCsr(params string[] serverNames) =>
        $@"{{""CN"":""Operator Service"",""hosts"":[""{string.Join(@""",""", serverNames)}""],""key"":{{""algo"":""ecdsa"",""size"":256}},""names"":[{{""C"":""DEV"",""L"":""Kubernetes""}}]}}";

    private static Task ExecuteProcess(Process process)
        => Task.Run(
            () =>
            {
                process.Start();
                process.WaitForExit(2000);
            });

    private static void Delete(string? file)
    {
        if (file == null)
        {
            return;
        }

        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }

    private async Task PrepareExecutables()
    {
        Directory.CreateDirectory(_tempDirectory);
        _cfssl = Path.Join(_tempDirectory, Path.GetRandomFileName());
        _cfssljson = Path.Join(_tempDirectory, Path.GetRandomFileName());
        _caconfig = Path.Join(_tempDirectory, Path.GetRandomFileName());
        _cacsr = Path.Join(_tempDirectory, Path.GetRandomFileName());

        using (var client = new HttpClient())
        await using (var cfsslStream = new FileStream(_cfssl, FileMode.CreateNew))
        await using (var cfsslJsonStream = new FileStream(_cfssljson, FileMode.CreateNew))
        await using (var caConfigStream = new StreamWriter(new FileStream(_caconfig, FileMode.CreateNew)))
        await using (var caCsrStream = new StreamWriter(new FileStream(_cacsr, FileMode.CreateNew)))
        {
            await caConfigStream.WriteLineAsync(CaConfig);
            await caCsrStream.WriteLineAsync(CaCsr);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await _appOut.WriteLineAsync("Download cfssl / cfssljson for windows.");
                await using var cfsslDl = await client.GetStreamAsync(CfsslUrlWindows);
                await using var cfsslJsonDl = await client.GetStreamAsync(CfsslJsonUrlWindows);

                await cfsslDl.CopyToAsync(cfsslStream);
                await cfsslJsonDl.CopyToAsync(cfsslJsonStream);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await _appOut.WriteLineAsync("Download cfssl / cfssljson for linux.");
                await using var cfsslDl = await client.GetStreamAsync(CfsslUrlLinux);
                await using var cfsslJsonDl = await client.GetStreamAsync(CfsslJsonUrlLinux);

                await cfsslDl.CopyToAsync(cfsslStream);
                await cfsslJsonDl.CopyToAsync(cfsslJsonStream);
            }
            else
            {
                await _appOut.WriteLineAsync("Download cfssl / cfssljson for macos.");
                await using var cfsslDl = await client.GetStreamAsync(CfsslUrlMacOs);
                await using var cfsslJsonDl = await client.GetStreamAsync(CfsslJsonUrlMacOs);

                await cfsslDl.CopyToAsync(cfsslStream);
                await cfsslJsonDl.CopyToAsync(cfsslJsonStream);
            }
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await _appOut.WriteLineAsync("Make unix binaries executable.");
            await ExecuteProcess(
                new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = ArgumentEscaper.EscapeAndConcatenate(
                            new[] { "+x", _cfssl, _cfssljson }),
                    },
                });
        }
    }

    private async Task ListDir(string directory)
    {
        await _appOut.WriteLineAsync($"Files in {directory}:");
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            await _appOut.WriteLineAsync(file);
        }
    }
}
