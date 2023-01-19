# KubeOps Dotnet New Templates

To use the operator SDK as easy as possible, this
[Nuget Package](https://www.nuget.org/packages/KubeOps.Templates)
contains `dotnet new` templates.
These templates enable developers to create Kubernetes operators
with the simple dotnet new command in C# or F#.

## Installation

To install the template package, use the `dotnet` cli
(or you may use the exact version as provided in the link above):

```bash
dotnet new --install KubeOps.Templates::*
```

As soon as the templates are installed, you may use them with:

```bash
dotnet new operator
#or
dotnet new operator-empty
```

Note that several of the templates are available in multiple languages
of the .NET framework (i.e. C\# and F\#) and you may switch the
language with the `-lang` flag of `dotnet new`.

## Templates

### Empty Operator

_Available Languages_: C\#, F\#

_Type_: Generate a project

_Templatename_: `operator-empty`

_Example installation_: `dotnet new operator-empty -n MyOperator`

_Description_:
This template contains the well known `Program.cs`
and `Startup.cs` files of any other `ASP.NET` project
and configures the startup file to use KubeObs.
No additional code is provided.

### Demo Operator

_Available Languages_: C\#, F\#

_Type_: Generate a project

_Templatename_: `operator`

_Example installation_: `dotnet new operator -n MyOperator`

_Description_:
This template contains the well known `Program.cs`
and `Startup.cs` files of any other `ASP.NET` project
and configures the startup file to use KubeObs.
In addition to the empty operator, an example file
for each "concept" is provided. You'll find an
example implementation of:

- A resource controller
- A custom entity (that generates a CRD)
- A finalizer
- A validation webhook
- A mutation webhook

This template is meant to show all possible elements
of KubeOps in one go.
