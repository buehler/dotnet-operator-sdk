using System.Diagnostics;

namespace KubeOps.Operator.Commands.CommandHelpers;

internal class CertificateGenerator : IDisposable
{
    /* Suggested URLs for downloading the executables into the docker image
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
    */

    private const string CaConfig =
        @"{""signing"":{""default"":{""expiry"":""43800h""},""profiles"":{""server"":{""expiry"":""43800h"",""usages"":[""signing"",""key encipherment"",""server auth""]}}}}";

    private const string CaCsr =
        @"{""CN"":""Operator Root CA"",""key"":{""algo"":""rsa"",""size"":2048},""names"":[{""C"":""DEV"",""L"":""Kubernetes"",""O"":""Kubernetes Operator""}]}";

    private readonly TextWriter _appOut;
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private bool _initialized;
    private string? _caconfig;
    private string? _cacsr;
    private string? _servercsr;
    private string _cfssl = "cfssl";
    private string _cfssljson = "cfssljson";

    public CertificateGenerator(TextWriter appOut)
    {
        _appOut = appOut;
    }

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
            var cfsslFolder = Environment.GetEnvironmentVariable("CFSSL_EXECUTABLES_PATH") ?? "/operator";
            _cfssl = $"{cfsslFolder}/{_cfssl}";
            _cfssljson = $"{cfsslFolder}/{_cfssljson}";
            await PrepareExecutables();
            _initialized = true;
        }

        Directory.CreateDirectory(outputFolder);

        await _appOut.WriteLineAsync($@"Generating certificates to ""{outputFolder}"".");
        await _appOut.WriteLineAsync("Generating CA certificate.");

        await GenCertAsync($"-initca {_cacsr}", outputFolder);

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
        var arguments =
            $"-ca=file:{caPath.Replace('\\', '/')} -ca-key=file:{caKeyPath.Replace('\\', '/')} -config={_caconfig} -profile=server {_servercsr}";
        await GenCertAsync(arguments, outputFolder);
        await ListDir(outputFolder);
    }

    private static string ServerCsr(params string[] serverNames) =>
        $@"{{""CN"":""Operator Service"",""hosts"":[""{string.Join(@""",""", serverNames)}""],""key"":{{""algo"":""ecdsa"",""size"":256}},""names"":[{{""C"":""DEV"",""L"":""Kubernetes""}}]}}";

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

    private async Task GenCertAsync(
        string arguments,
        string outputFolder,
        CancellationToken cancellationToken = default)
    {
        var genCertPsi = new ProcessStartInfo
        {
            WorkingDirectory = outputFolder,
            FileName = _cfssl,
            Arguments = $"gencert {arguments}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        var writeJsonPsi = new ProcessStartInfo
        {
            WorkingDirectory = outputFolder,
            FileName = _cfssljson,
            Arguments = "-bare ca -",
            RedirectStandardInput = true,
            UseShellExecute = false,
        };
        using var genCert = new Process { StartInfo = genCertPsi };
        using var writeJson = new Process { StartInfo = writeJsonPsi };

        genCert.Start();
        await genCert.WaitForExitAsync(cancellationToken);

        writeJson.Start();
        await writeJson.StandardInput.WriteAsync(await genCert.StandardOutput.ReadToEndAsync());
        await writeJson.StandardInput.FlushAsync();
        writeJson.StandardInput.Close();
        await writeJson.WaitForExitAsync(cancellationToken);
    }

    private async Task PrepareExecutables()
    {
        Directory.CreateDirectory(_tempDirectory);
        _caconfig = Path.Join(_tempDirectory, Path.GetRandomFileName());
        _cacsr = Path.Join(_tempDirectory, Path.GetRandomFileName());

        await using var caConfigStream = new StreamWriter(new FileStream(_caconfig, FileMode.CreateNew));
        await using var caCsrStream = new StreamWriter(new FileStream(_cacsr, FileMode.CreateNew));
        await caConfigStream.WriteLineAsync(CaConfig);
        await caCsrStream.WriteLineAsync(CaCsr);
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
