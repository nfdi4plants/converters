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
        
        let geoOntology = 
            Http.Request(@"https://raw.githubusercontent.com/nfdi4plants/converters/main/ontologies/geo.obo").Body.ToString().Split('\n')
            |> Obo.OboOntology.fromLines false
       
        workbook {
            sheet "MyAssay" {

                // ---- STUDY section ---- 
                row {"STUDY"}
                row {
                    "title"
                    required
                    !! s.Title
                }
                row {
                    "summary (abstract)"
                    required
                    !! s.Description
                }
                if s.Contacts.IsNone then dropSheet (message "No contributors in the assay file")
                for person in s.Contacts |> Option.defaultValue [] do
                    row {
                        "contributor"
                        optional
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
                    for sample in s.LastSamples do
                        sample.Name
                }
            
                column {
                    "title"
                    required
                    for sample in s.LastSamples do
                        sample.Name
                }
                column {
                    "organism"
                    required
                    for sample in s.LastSamples do
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
                    for sample in s.LastSamples do
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
                    for sample in s.LastSamples do
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
                    for sample in s.LastSamples do
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
                    for sample in s.LastSamples do
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
                    for sample in s.LastSamples do
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
                    for sample in s.LastSamples do
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
                    for sample in s.LastSamples do
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
                // Until then, this might be one possiblity that kees the integrity of the table up even if the samples have different numbers of processed data
                if s.LastSamples |> List.map (s.ProcessedDataOf >> Seq.length) |> List.isEmpty |> not then
                    let maxNumProcessedData = 
                        s.LastSamples |> List.map (s.ProcessedDataOf >> Seq.length) |> List.max
                    for i = 0 to maxNumProcessedData - 1 do
                        column {
                            "processed data file"
                            for sample in s.LastSamples do
                                sample.ProcessedData |> List.tryItem i |> Option.map (fun n -> n.Name) |> Option.defaultValue ""                       
                        }
                else dropSheet (message "No processed data found in any sheet.")

                if s.LastSamples |> List.map (s.RawDataOf >> Seq.length) |> List.isEmpty |> not then
                    let maxNumRawData = 
                        s.LastSamples |> List.map (s.RawDataOf >> Seq.length) |> List.max
                    for i = 0 to maxNumRawData - 1 do
                        column {
                            "raw data file"
                            for sample in s.LastSamples do
                                sample.RawData |> List.tryItem i |> Option.map (fun n -> n.Name) |> Option.defaultValue ""                       
                        }
                else dropSheet (message "No raw data found in any sheet.")

                // ---- Protocols section ----
                row
                row {"Protocol"}

                row { 
                    "growth protocol"
                    optional
                    cells {
                        for protocol in s.Protocols do
                            whereProtocolTypeIsChildOf geoOntology (OntologyAnnotation.fromString "growth protocol" "EFO" "EFO:0003789")
                            selectDescriptionText
                            exactlyOne
                    }
                }
                row {
                    "treatment protocol"
                    optional
                    cells {
                        for protocol in s.Protocols do
                            whereProtocolTypeIsChildOf geoOntology (OntologyAnnotation.fromString "treatment protocol" "DPBO" "DPBO:1000168")
                            selectDescriptionText
                            exactlyOne
                    }
                
                }
                row {
                    "extract protocol"
                    required
                    cells {
                        for protocol in s.Protocols do
                            whereProtocolTypeIsChildOf geoOntology (OntologyAnnotation.fromString "extraction protocol" "DPBO" "DPBO:1000171")
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
                        for i in s.Values() do
                            whereName "Library Selection"
                            asValueOfOntology geoOntology "GEO"
                            selectValueText
                            exactlyOne                       
                    }
                }  
            
                for cell in 
                    cells {
                        for protocol in s.Protocols do
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
                        for i in s.Values() do
                            whereName "genome reference sequence"
                            distinct
                            selectValueText
                            exactlyOne                        
                    }
                }

                for cell in 
                    cells {
                        for i in s.Values() do
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

)

