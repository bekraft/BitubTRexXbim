# Bitub TRex Xbim

![Build status](https://dev.azure.com/bitub/BitubTRexXbim/_apis/build/status/bekraft.BitubTRexXbim?branchName=master&label=MASTER)
![Build status](https://dev.azure.com/bitub/BitubTRexXbim/_apis/build/status/bekraft.BitubTRexXbim?branchName=dev&label=DEV)
![Nuget](https://img.shields.io/nuget/v/Bitub.Xbim.Ifc.svg)

## Goals & use cases

BitubTRexXbim uses the [Xbim libraries](https://github.com/xBimTeam) and adds some domain driven functionalities.
- IFC model builder pattern with uniform version handling
- IFC model-2-model geometrical transformation (i.e. typical geometrical alignment tasks for model fragments)
- IFC model-2-model property clean-up and transformation (i.e. removing property sets or single properties, mapping properties)
- IFC model export and further evaluation in model usage pipes (JSON export and via [Dynamo TRex](https://github.com/bekraft/BitubTRexDynamo) and [Assimp](https://github.com/assimp/assimp) to a number of visualization formats.

See the [Wiki](https://github.com/bekraft/BitubTRex/wiki) for the use cases.

Provided assemblies:
- ```Bitub.Xbim.Ifc``` wraps all extensions, workflows and additions concerning Xbim IFC model handling 

## Building

Build library
```
dotnet build -c (Release|Dev|XbimDev)
```

and running tests
```
dotnet test -c (Release|Dev|XbimDev) -a x64
```

Deployment configurations and dependencies
| Configuration | Xbim dependency | Frameworks |
|---------------|-----------------|------------|
| Release | Xbim master nugets | net472, net48, net481, net6.0 |
| Dev | Xbim master nugets |  net472, net48, net481, net6.0 |
| XbimDev | Xbim develop nugets | net472, net48, net481, net6.0 |

## Licenses

- CDDL [Xbim Essentials](https://github.com/xBimTeam/XbimEssentials) and [Xbim Geometry](https://github.com/xBimTeam/XbimGeometry)
- Apache 2.0 for BitubTRex and BitubTRexXbim
