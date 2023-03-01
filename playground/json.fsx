#r "nuget: arcIO.NET, 0.1.0-preview.6" 
#r "nuget: System.Text.Json, 8.0.0-preview.1.23110.8"
#r "nuget: JsonDSL, 0.0.4"
#r "nuget: FSharp.Data, 5.0.2"



open ISADotNet
open ISADotNet.QueryModel
open arcIO.NET    

open JsonDSL
open System.Text.Json
open FSharp.Data

let getISA (studyIdentifier : string option) (assayIdentifier : string option) arcDir : QInvestigation*QStudy*QAssay= 
    let i = Investigation.fromArcFolder arcDir
    let s = 
        let s = 
            match studyIdentifier with
            | Option.Some si ->  i.Studies.Value |> List.find (fun s -> s.Identifier.Value = si)
            | None -> i.Studies.Value.Head
        let ps = 
            (s.ProcessSequence |> Option.defaultValue [])
            @
            (s.Assays |> Option.defaultValue [] |> List.collect (fun a -> a.ProcessSequence |> Option.defaultValue []))            
        {s with ProcessSequence = Option.Some ps}
    let a = 
        match assayIdentifier with
            | Option.Some ai ->  s.Assays.Value |> List.find (fun a -> a.FileName.Value = Assay.nameToFileName ai)
            | None -> s.Assays.Value.Head
        
    QInvestigation.fromInvestigation(i),
    QStudy.fromStudy(s),
    QAssay.fromAssay(a,[])

let writeToJson (path : string) (jsonDSL : JEntity<Nodes.JsonNode>) =
    jsonDSL.Value.ToJsonString()
    |> fun s -> System.IO.File.WriteAllText(path,s)


// Specify these values
let arcDir = ""
let assayName = None // Some ""
let studyName = None // Some ""
let outputPath = ""

let i,s,a = getISA studyName assayName arcDir

// Build your converter
let result = 
    object {
        required
        property "value" "myValue"
        property "object" (object {
            property "subValue" "mySubValue"
        })
        property "array" (array {
            1
            2
            3        
        })
    }

// Test output
// will fail, if your result could not be computed because of missing values
writeToJson outputPath result