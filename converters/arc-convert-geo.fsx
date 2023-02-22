#r "nuget: Argu"
#r "nuget: arcIO.NET, 0.1.0-preview.4" 


open System.IO
open ISADotNet
open ISADotNet.QueryModel
open ISADotNet.QueryModel.Linq.Spreadsheet
open arcIO.NET
open FsSpreadsheet.DSL
open FsSpreadsheet.ExcelIO

open Argu

let prompt (msg:string) =
    System.Console.Write(msg)
    System.Console.ReadLine().Trim()
    |> function | "" -> None | s -> Option.Some s
    |> Option.map (fun s -> s.Replace ("\"","\\\""))

let rec promptYesNo msg =
    match prompt (sprintf "%s [Yn]: " msg) with
    | Option.Some "Y" | Option.Some "y" -> true
    | Option.Some "N" | Option.Some "n" -> false
    | _ -> System.Console.WriteLine("Sorry, invalid answer"); promptYesNo msg

type CliArguments =
    | [<Mandatory>][<AltCommandLine("-p")>] ARC_Directory of arc_dir:string
    | [<AltCommandLine("-o")>] Out_File of out_path:string

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | ARC_Directory _ -> "The path to the root diretoy of the ARC."
            | Out_File _ -> "Optional path for where the metadata json file should be written to, else written to ARC_DIRECTORY/metadata.json"

[<EntryPoint>]
let main argv =

    let parser = ArgumentParser.Create<CliArguments>()
    let results = parser.Parse argv

    let arcDir = results.GetResult(ARC_Directory)
    let outPath = 
        results.TryGetResult(Out_File)
        |> Option.defaultValue (Path.Combine(arcDir,".arc/GEO.xlsx"))

    let investigation = Investigation.fromArcFolder arcDir

    let study = investigation.Studies.Value.Head |> API.Study.update

    // Create queryable object
    let ps = 
        QStudy.fromStudy(study).FullProcessSequence
        //QAssay.fromAssay(assay)


    let ontology = 
        File.ReadAllLines (Path.Combine([|arcDir;".arc";"dpbo.obo"|]))
        |> Seq.append (File.ReadAllLines (Path.Combine([|arcDir;".arc";"GEO.obo"|])))
        |> Obo.OboOntology.fromLines false


    // Create spreadsheet building blocks using ISADotNet querymodel and FsSpreadsheet DSL
    let output = 
        workbook {
            sheet "MyAssay" {

                // ---- STUDY section ---- 
                row {"STUDY"}
                row {
                    "title"
                    required
                    !! study.Title
                }
                row {
                    "summary (abstract)"
                    required
                    !! study.Description
                }
                if study.Contacts.IsNone then dropSheet (message "No contributors in the assay file")
                for person in study.Contacts.Value do
                    row {
                        "contributor"
                        cell {
                            Concat ','
                            !! person.FirstName
                            !? person.MidInitials
                            !! person.LastName
                        }
                    }

                // ---- Samples section ----
                row
                row {"SAMPLES"}

                column {
                    "library name"
                    required
                    for sample in ps.LastSamples do
                        sample.Name
                }
            
                column {
                    "title"
                    required
                    for sample in ps.LastSamples do
                        sample.Name
                }
                column {
                    "organism"
                    required
                    for sample in ps.LastSamples do
                        cells {
                            for value in sample.Values do
                                whereCategory "Organism" "DPBO" "DPBO:11111111"
                                selectValueText
                                head
                                required
                        }               
                }
                column {
                    "cell line"
                    optional
                    for sample in ps.LastSamples do
                        cells {
                            for value in sample.Values do
                                whereName "Cell line"
                                selectValueText
                                head
                                required
                        }               
                }
                column {
                    "cell type"
                    optional
                    for sample in ps.LastSamples do
                        cells {
                            for value in sample.Values do
                                whereName "Sample type"
                                selectValueText
                                head
                                required
                        }               
                }
                column {
                    "genotype"
                    optional
                    for sample in ps.LastSamples do
                        cells {
                            for value in sample.Values do
                                whereName "Genotype"
                                selectValueText
                                head
                                optional
                        }               
                }
                column {
                    "molecule"
                    required
                    for sample in ps.LastSamples do
                        cells {
                            for value in sample.Values do
                                whereName "Library Selection"
                                //asValueOfOntology ontology "GEO" // for example: complementary DNA (DPBO) -> cDNA (GEO)
                                selectValueText
                                head
                           
                        }                              
                }
                column {
                    "single or paired-end"
                    required
                    for sample in ps.LastSamples do
                        cells {
                            for value in sample.Values do
                                whereName "Library layout"
                                //asValueOfOntology "GEO"
                                selectValueText
                                head
                                optional
                        }               
                }
                column {
                    "age"
                    optional
                    for sample in ps.LastSamples do
                        cells {
                            for value in sample.Values do
                                whereName "time"
                                selectValueText
                                head
                                required
                        }  
                
                } 
                column {
                    "instrument model"
                    required
                    for sample in ps.LastSamples do
                        cells {
                            for value in sample.Values do
                                whereName "next generation sequencing instrument model"                                 // this
                                //whereCategoryIsChildOf (OntologyAnnotation.fromString "instrument model" "source" "id") // or this?
                                //whereCategoryIsChildOf (OntologyAnnotation.fromString "instrument" "EFO" "http://www.ebi.ac.uk/efo/EFO_0000548") // or this?
                                selectValueText
                                head
                        }               
                }


                // This is kind of a tricky task for a general purpose query model, as the number of columns depends on the given input
                // Maybe I will find a way to make this nicer
                // Until then, this might be one possiblity that keeps the integrity of the table up even if the samples have different numbers of processed data
                let maxNumProcessedData = 
                    ps.LastSamples |> List.map (ps.ProcessedDataOf >> Seq.length) |> List.max
                for i = 0 to maxNumProcessedData - 1 do
                    column {
                        "processed data file"
                        for sample in ps.LastSamples do
                            sample.ProcessedData |> List.tryItem i |> Option.map (fun n -> n.Name) |> Option.defaultValue ""                       
                    }

                let maxNumRawData = 
                    ps.LastSamples |> List.map (ps.RawDataOf >> Seq.length) |> List.max
                for i = 0 to maxNumRawData - 1 do
                    column {
                        "raw data file"
                        for sample in ps.LastSamples do
                            sample.RawData |> List.tryItem i |> Option.map (fun n -> n.Name) |> Option.defaultValue ""                       
                    }

                // ---- Protocols section ----
                row
                row {"Protocol"}

                row { 
                    "growth protocol"
                    optional
                    cells {
                        for protocol in ps.Protocols do
                            whereProtocolTypeIsChildOf ontology (OntologyAnnotation.fromString "growth protocol" "EFO" "EFO:0003789")
                            selectDescriptionText
                            exactlyOne
                    }
                }
                row {
                    "treatment protocol"
                    optional
                    cells {
                        for protocol in ps.Protocols do
                            whereProtocolTypeIsChildOf ontology (OntologyAnnotation.fromString "treatment protocol" "DPBO" "DPBO:1000168")
                            selectDescriptionText
                            exactlyOne
                    }
                
                }
                row {
                    "extract protocol"
                    required
                    cells {
                        for protocol in ps.Protocols do
                            whereProtocolTypeIsChildOf ontology (OntologyAnnotation.fromString "extraction protocol" "DPBO" "DPBO:1000171")
                            selectDescriptionText
                            exactlyOne
                    }
                }
                row {
                    "library construction protocol"
                    required
                }
             
                row {
                    "library strategy"
                    required
                    cells {
                        for i in ps.Values() do
                            whereName "Library Selection"
                            asValueOfOntology ontology "GEO"
                            selectValueText
                            exactlyOne                       
                    }
                }  
            
                for cell in 
                    cells {
                        for protocol in ps.Protocols do
                            whereSoftwareProtocol
                            selectDescriptionText
                            atLeastN 1
                    }
                    do 
                        row {
                            "data processing step"
                            required
                            cell
                        }


                row {
                    "genome build/assembly"
                    required
                    cells {
                        for i in ps.Values() do
                            whereName "genome reference sequence"
                            distinct
                            selectValueText
                            exactlyOne                        
                    }
                }

                for cell in 
                    cells {
                        for i in ps.Values() do
                            whereName "processed data file format"
                            distinct
                            selectValueText
                            atLeastN 1
                    }
                    do 
                        row {
                            "data processing step"
                            required
                            cell
                        }

            }
        }

    let messagesOutPath = Path.Combine(arcDir,".arc/OutputMessages.txt")

    let writeMessages (messages : Message list) =
        messages
        |> List.map (fun m -> m.AsString())
        |> List.toArray
        |> Array.distinct
        |> fun messages -> 
            File.WriteAllLines(messagesOutPath,messages)

    match output with
    | Some (workbook,messages) -> 
        workbook.Parse().ToFile(outPath)
        writeMessages messages
        1
    | NoneOptional messages | NoneRequired messages ->
        writeMessages messages
        printfn "Arc could not be converted to GEO, as some required values could not be retreived, check %s for more info" messagesOutPath
        if promptYesNo "Do you want missing fields to be written back into ARC? (y/n)" then
            let transformations = 
                messages
                |> ErrorHandling.getStudyformations "GEO"
                |> List.distinct       
            let updatedStudy = 
                transformations
                |> List.fold (fun s transformation -> transformation.Transform s) study        
            let updatedArc = 
                investigation
                |> API.Investigation.mapStudies
                    (API.Study.updateByIdentifier API.Update.UpdateAll updatedStudy)
                |> API.Investigation.update
            Study.overWrite arcDir updatedStudy
            Investigation.overWrite arcDir updatedArc
        0