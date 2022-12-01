# Bitub TRex Xbim

Dev Build ![Dev status](https://dev.azure.com/bitub/BitubTRexXbim/_apis/build/status/bekraft.BitubTRexXbim?branchName=dev) | Release Build ![Dev status](https://dev.azure.com/bitub/BitubTRexXbim/_apis/build/status/bekraft.BitubTRexXbim?branchName=master)

## Goal

BitubTRexXbim uses the [Xbim libraries](https://github.com/xBimTeam) and adds some domain driven functionalities.

Mainly adds
- transforming pipes to filter, enhance and modify IFC models (2x3, 4.0 & 4.1).
- export into JSON or Protobuf format
- internal functionalities of [BitubTRexDynamo](https://github.com/bekraft/BitubTRexDynamo)

Provided assemblies:
- ```Bitub.Xbim.Ifc``` wraps all extensions, workflows and additions concerning Xbim IFC model handling 

## Use cases

See the [Wiki](https://github.com/bekraft/BitubTRexXbim/wiki) for the use cases.

- Building IFCs programmatically.
- Transforming IFCs by async transformation requests.
- Exporting IFCs to other formats.

Its part of the Dynamo plugin  [BitubTRexDynamo](https://github.com/bekraft/BitubTRexDynamo).

## Licenses

- CDDL [Xbim Essentials](https://github.com/xBimTeam/XbimEssentials) and [Xbim Geometry](https://github.com/xBimTeam/XbimGeometry)
- Apache 2.0 for BitubTRex and BitubTRexXbim
