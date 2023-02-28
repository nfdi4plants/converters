#r "nuget: arcIO.NET, 0.1.0-preview.6" 
#r "nuget: FSharp.Data, 5.0.2"
#r "nuget: JsonDSL, 0.0.4"

//#r @"C:\Users\HLWei\source\repos\ISADotNet\src\ISADotNet.QueryModel\bin\Release\netstandard2.0\ISADotNet.dll"
//#r @"C:\Users\HLWei\source\repos\ISADotNet\src\ISADotNet.QueryModel\bin\Release\netstandard2.0\ISADotNet.QueryModel.dll"
//#r @"C:\Users\HLWei\source\repos\arcIO.NET\src\arcIO.NET\bin\Release\netstandard2.1\arcIO.NET.dll"
//#r @"C:\Users\HLWei\source\repos\JsonDSL\src\JsonDSL\bin\Release\net7.0\JsonDSL.dll"

open arcIO.NET
open arcIO.NET.Converter
open JsonDSL
open System.IO
open ISADotNet.QueryModel
open ISADotNet.QueryModel.Linq
//open ISADotNet.QueryModel.Linq.Json

let now = System.DateTime.Now

let create() = ARCconverter.ARCtoJSON (
    fun i s a ->
        object {
            required
            property "resource_type" (object { // required: always dataset
                property "id" "dataset"
            })
            property "creators" (array { // required: at least 1
                
                for p in Investigation.contacts i do
                    object {
                        property "person_or_org" (object {
                            property "type" "personal"
                            property "given_name" ((+.) Person.firstName p)                     // required: always dataset
                            property "family_name" ((+.) Person.lastName p)                       // required: always dataset
                        })
                    }                              
            })
            property "title" ((+.) Investigation.title i)                                               // required: always dataset
            property "publication_date" $"{now.Year}" //$"{now.Year}-{now.Month}-{now.Day}" // required but with default value?
            property "additional_titles" (array {
                optionaL
                for s in i.Studies do
                    object {
                        required
                        property "title" ((+.) Study.title s)
                        //property "type" {
                        //    property 
                        
                        //}
                    }
            
            })
        }


)