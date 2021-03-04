using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s.Models;
using KubeOps.Operator.Services;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Management.Webhooks
{
    [Command(
        "register",
        "reg",
        "r",
        Description = "Registers the currently implemented webhooks in the current selected cluster.")]
    internal class Register
    {
        private readonly OperatorSettings _settings;
        private readonly IKubernetesClient _client;
        private readonly ResourceLocator _resourceLocator;
        private readonly IServiceProvider _serviceProvider;

        public Register(
            OperatorSettings settings,
            IKubernetesClient client,
            ResourceLocator resourceLocator,
            IServiceProvider serviceProvider)
        {
            _settings = settings;
            _client = client;
            _resourceLocator = resourceLocator;
            _serviceProvider = serviceProvider;
        }

        [Option(
            Description =
                "The base-url under which the webhooks are registered (e.g. https://foobar.ngrok.com/). " +
                "Either base url or service info must be set.")]
        public string? BaseUrl { get; init; }

        [Option(
            Description =
                "The name of a kubernetes service that should be used for communication. " +
                "Either base url or service info must be set.",
            Template = "--service-name <NAME>")]
        public string? ServiceName { get; init; }

        [Option(
            Description =
                "The namespace of the kubernetes service that should be used for communication. " +
                "Either base url or service info must be set.",
            Template = "--service-namespace <NAMESPACE>")]
        public string? ServiceNamespace { get; init; }

        [Option(
            Description =
                "The - if any - path to the specific route on the service. " +
                "Either base url or service info must be set.",
            Template = "--service-path <PATH>")]
        public string? ServicePath { get; init; }

        [Option(
            Description =
                "The port of the service that should be used (defaults to 443). " +
                "Either base url or service info must be set.",
            Template = "--service-port <PORT>")]
        public short? ServicePort { get; init; }

        [Option(
            Description =
                "The (pem encoded) ca-certificate - if any - bundle to validate the webhook server. " +
                "Either base url or service info must be set.",
            Template = "--ca-bundle <CA_PEM_DATA>")]
        public string? CaBundle { get; init; }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            await app.Out.WriteLineAsync($"Found {_resourceLocator.ValidatorTypes.Count()} validators.");
            await app.Out.WriteLineAsync($"Found {_resourceLocator.MutatorTypes.Count()} mutators.");

            var hookConfig = (
                _settings.Name,
                BaseUrl,
                CaBundle != null ? Encoding.UTF8.GetBytes(CaBundle) : null,
                ServiceName != null && ServiceNamespace != null
                    ? new Admissionregistrationv1ServiceReference(
                        ServiceName,
                        ServiceNamespace,
                        ServicePath,
                        ServicePort)
                    : null);
            var validatorConfig = Operator.Webhooks.Webhooks.CreateValidator(
                hookConfig,
                _resourceLocator,
                _serviceProvider);
            var mutatorConfig = Operator.Webhooks.Webhooks.CreateMutator(
                hookConfig,
                _resourceLocator,
                _serviceProvider);

            await app.Out.WriteLineAsync($@"Install ""{validatorConfig.Metadata.Name}"" validator on cluster.");
            await app.Out.WriteLineAsync($@"Install ""{mutatorConfig.Metadata.Name}"" mutator on cluster.");
            await _client.Save(validatorConfig);
            await _client.Save(mutatorConfig);

            return ExitCodes.Success;
        }
    }
}
