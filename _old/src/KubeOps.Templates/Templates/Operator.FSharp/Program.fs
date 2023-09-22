namespace GeneratedOperatorProject

open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open KubeOps.Operator

module Program =
    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webBuilder -> webBuilder.UseStartup<Startup>() |> ignore)

    [<EntryPoint>]
    let main args =
        createHostBuilder(args)
            .Build()
            .RunOperatorAsync args
        |> Async.AwaitTask
        |> Async.RunSynchronously
