namespace GeneratedOperatorProject

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open KubeOps.Operator

type Startup() =

    member _.ConfigureServices(services: IServiceCollection) =
        services.AddKubernetesOperator() |> ignore
        ()

    member _.Configure(app: IApplicationBuilder) =
        app.UseKubernetesOperator()
        ()
