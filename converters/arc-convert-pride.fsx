#r "nuget: arcIO.NET, 0.1.0-preview.4" 

open arcIO.NET
open ISADotNet
open ISADotNet.QueryModel
open ISADotNet.QueryModel.Linq
open ISADotNet.QueryModel.Linq.Spreadsheet
open Helpers
open FsSpreadsheet.DSL
open FsSpreadsheet.ExcelIO
open System.IO

let arcDir = @"C:\Users\olive\OneDrive\CSB-Stuff\NFDI\testARC28"
let outPath = Path.Combine(arcDir, ".arc", "GEO.xlsx")

let investigation = Investigation.fromArcFolder arcDir

let study = investigation.Studies.Value.Head |> API.Study.update
let assay = arcIO.NET.Assay.readByName arcDir "aid" |> snd

let pss = QStudy.fromStudy(study).FullProcessSequence
let psa = QStudy.fromAssay(assay)


type ISAQueryBuilder with
    
    /// Adjusts a text to fit set char limits.
    [<CustomOperation "adjustToCharLimits">]
    member this.AdjustToCharLimits(text: string, minLimit, maxLimit) =
        match text.Length with
        | x when x < minLimit -> failwith ""
        | x when x > maxLimit -> $"{text[.. maxLimit - 1 - 5]}[...]"
        | _ -> text


sheet "" {
    row {
        "MTD"
        "submitter_name"
        cell {
            !! (Investigation.contacts >> Person.firstName) i
            !? (Investigation.contacts >> Person.midInitials) i
            !! (Investigation.contacts >> Person.lastname) i
            Concat ','
        }
        required
    }
    row {
        "MTD"
        "submitter_email"
        !! investigation.Contacts.Value.Head.EMail.Value
        required
    }
    row {
        "MTD"
        "submitter_affiliation"
        !! investigation.Contacts.Value.Head.Affiliation.Value
        required
    }
    row {
        "MTD"
        "lab_head_name"
        !! $"{investigation.Contacts.Value.Head.FirstName.Value} {investigation.Contacts.Value.Head.MidInitials.Value} {investigation.Contacts.Value.Head.LastName.Value}"
        required
    }
    row {
        "MTD"
        "lab_head_email"
        !! investigation.Contacts.Value.Head.EMail.Value
        required
    }
    row {
        "MTD"
        "lab_head_affiliation"
        !! investigation.Contacts.Value.Head.Affiliation.Value
        required
    }
    row {
        "MTD"
        "submitter_pride_login"
        // im Prompt abfragen?
        required
    }
    row {
        "MTD"
        "project_title"
        !! investigation.Title
        required
    }
    row {
        "MTD"
        "project_description"
        !! investigation.Description
        required
    }
    // we ignore `MTD project_tag` since, first, it is optional and, second, we have no fitting field in ISA
    row {
        "MTD"
        "sample_processing_protocol"
        cells {
            for prot in pss.Protocols do
                selectDescriptionText
                concat ';'
                adjustToCharLimits 50 5000
        }
        required
    }
    row {
        "MTD"
        "data_processing_protocol"
        cells {
            for prot in psa.Protocols do
                selectDescriptionText
                concat ';'
                adjustToCharLimits 50 5000
        }
        required
    }
}

