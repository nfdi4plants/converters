# ARC Converters
Auto-deployed collection of [ARC](https://github.com/nfdi4plants/ARC-specification) converters for converting ARCs to end-point repositories.


## General workflow

- Converters in this repository are simple `fsx` script files that share a common structure
- When pushed to the main branch, they get automatically compiled and released
- Now they are accessible using the [ArcCommander](https://github.com/nfdi4plants/arcCommander), which makes use of the common structure to to run a streamlined conversion

## Contribution Guide

### Guidelines
1. Create issue with background information about the converter you want to add. This also serves as a place for discussion.
2. [Fork](https://docs.github.com/en/get-started/quickstart/fork-a-repo) this repository.
3. Create a feature branch.
4. [Clone](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository) your fork-branch.
5. [Add/Update](#create-a-converter) **ONE** new converter.
6. [Test](#test-a-converter) your converter locally.
7. Commit, push and [sync your branch](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/working-with-forks/syncing-a-fork).
    - 👀 Add your issue id (for example #32) to your commit so it is automatically linked.
8. Open a [pull request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/about-pull-requests) **referencing your issue**. :tada:

### Create a converter

- Start with adding a file to the `converters` folder. It MUST follow the naming scheme `arc-convert-<FORMATNAME>.fsx`.
- Go into the file and add a reference to `arcIO.NET`
- Add a `create()` function which has the converter body as return value
- The converter body must be of type `ARCconverter`(located in `arcIO.NET.Converter`), selecting the return type you want. Your code could now look like this:
   ```fsharp
   #r "nuget: arcIO.NET, 0.1.0-preview.5" 
   #r "nuget: FSharp.Data, 5.0.2"

   open ISADotNet
   open ISADotNet.QueryModel
   open ISADotNet.QueryModel.Linq.Spreadsheet
   open arcIO.NET.Converter
   open FsSpreadsheet.DSL
   open FSharp.Data
   
   let create() = ARCconverter.ARCtoXLSX (
       fun i s a ->
        
       )
   ```
   Note, that all converters take a `QInvestigation`, a `QStudy` and a `QAssay` as input. Which `study` and `assay` are selected to be converted is decided by the `ArcCommander` when running the conversion.
- Now fill out the converter body as you wish, the return type being determined by the format case you specified.
  - Documentation about the `ISADotNet Querymodel` can be found [here](https://nfdi4plants.github.io/ISADotNet-docs/docs/linq.html)
  - Documentation about the `FsSpreadsheet DSL` can be found [here](https://github.com/CSBiology/FsSpreadsheet#dsl)
  - Documentation about the `LitXML DSL` can be found [here](https://github.com/CSBiology/LitXml#usage)

### Test a converter

Lorem ipsum

### Run a deployed converter

Released converters can be run by the [ArcCommander](https://github.com/nfdi4plants/arcCommander) using the syntax:

`arc convert geo`

Check out `arc convert --help` for additional info.

### Allowed references

Only references to libraries referenced by the [ArcCommander](https://github.com/nfdi4plants/arcCommander) are allowed:
```fsharp
#r "nuget: arcIO.NET, 0.1.0-preview.5" 
#r "nuget: FSharp.Data, 5.0.2"
#r "nuget: LitXml"
```
Note, that `arcIO.NET` is always required and already comes packed with all dependencies for using the `ISADotNet QueryModel` and parsing to `Spreadsheet` based output formats.
