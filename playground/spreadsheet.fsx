#r "nuget: arcIO.NET, 0.1.0-preview.6" 
#r "nuget: FsSpreadsheet.CsvIO, 0.3.0"
#r "nuget: FSharp.Data, 5.0.2"

open ISADotNet
open ISADotNet.QueryModel
open ISADotNet.QueryModel.Linq.Spreadsheet
open arcIO.NET    
open arcIO.NET.Converter
open FsSpreadsheet.DSL
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

let writeToXLSX (path : string) (sheetDSL : SheetEntity<Workbook>) =
    sheetDSL.Value.Parse()
    |> fun sheet -> FsSpreadsheet.ExcelIO.Writer.toFile(path,sheet)

let writeToCSV (path : string) (sheetDSL : SheetEntity<Workbook>) =
    sheetDSL.Value.Parse()
    |> fun sheet -> FsSpreadsheet.CsvIO.Writer.toFile(path,sheet,',')

let writeToTSV (path : string) (sheetDSL : SheetEntity<Workbook>) =
    sheetDSL.Value.Parse()
    |> fun sheet -> FsSpreadsheet.CsvIO.Writer.toFile(path,sheet,'\t')


// Specify these values
let arcDir = ""
let assayName = None // Some ""
let studyName = None // Some ""
let outputPath = ""

let i,s,a = getISA studyName assayName arcDir

// Build your converter
let result = 
    workbook {
        sheet "MySheet" {
            row{
                1
                2
                3           
            }        
        }
    }

// Test output
// will fail, if your result could not be computed because of missing values
writeToXLSX outputPath result