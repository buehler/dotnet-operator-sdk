# MS Build extensions

This project extends the default build process of dotnet with some
code generation targets after the build.

You'll find the configurations and targets here:

- [KubeOps.targets](https://github.com/buehler/dotnet-operator-sdk/blob/master/src/KubeOps/Build/KubeOps.targets): defines the additional build targets

An example of such a target is:

[!code-xml[KubeOps.targets](../../src/KubeOps/Build/KubeOps.targets?range=2-17&dedent=4&highlight=2-7)]

They can be configured with the prop settings described below.
The props file just defines the defaults.

## Prop Settings

You can overwrite the default behaviour of the building parts with the following
variables that you can add in a `<PropertyGroup>` in your `csproj` file:

| Property               | Description                                                                | Default Value                                                           |
| ---------------------- | -------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| KubeOpsDockerfilePath  | The path of the dockerfile                                                 | $(MSBuildProjectDirectory)\Dockerfile                                   |
| KubeOpsDockerTag       | Which dotnet sdk / run tag should be used                                  | latest                                                                  |
| KubeOpsConfigRoot      | The base directory for generated elements                                  | $(MSBuildProjectDirectory)\config                                       |
| KubeOpsCrdDir          | The directory for the generated crds                                       | \$(KubeOpsConfigRoot)\crds                                              |
| KubeOpsCrdFormat       | Output format for crds                                                     | Yaml                                                                    |
| KubeOpsCrdUseOldCrds   | Use V1Beta version of crd instead of V1<br>(for kubernetes version < 1.16) | false                                                                   |
| KubeOpsRbacDir         | Where to put the roles                                                     | \$(KubeOpsConfigRoot)\rbac                                              |
| KubeOpsRbacFormat      | Output format for rbac                                                     | Yaml                                                                    |
| KubeOpsOperatorDir     | Where to put operator related elements<br>(e.g. Deployment)                | \$(KubeOpsConfigRoot)\operator                                          |
| KubeOpsOperatorFormat  | Output format for the operator                                             | Yaml                                                                    |
| KubeOpsInstallerDir    | Where to put the installation files<br>(e.g. Namespace / Kustomization)    | \$(KubeOpsConfigRoot)\install                                           |
| KubeOpsInstallerFormat | Output format for the installation files                                   | Yaml                                                                    |
| KubeOpsSkipDockerfile  | Skip dockerfile during build                                               | ""                                                                      |
| KubeOpsSkipCrds        | Skip crd generation during build                                           | ""                                                                      |
| KubeOpsSkipRbac        | Skip rbac generation during build                                          | ""                                                                      |
| KubeOpsSkipOperator    | Skip operator generation during build                                      | ""                                                                      |
| KubeOpsSkipInstaller   | Skip installer generation during build                                     | ""                                                                      |
