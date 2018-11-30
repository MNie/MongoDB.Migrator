// Learn more about F# at http://fsharp.org

open System
open System.Threading
open System.Linq

module Database =
    open MongoDB.Bson
    open System
    open MongoDB.Driver

    let client (con: string) db =
        let c = MongoClient(con)
        c.GetDatabase db
        
    let options =
        let f = FindOptions<BsonDocument>()
        f.BatchSize <- Nullable<int> 100
        f

module Migrate =
    open MongoDB.Bson
    open MongoDB.Bson
    open MongoDB.Driver

    let basedOn inConnection outConnection db collection field token =
        let inClient = Database.client inConnection db
        let outClient = Database.client outConnection db
        let collection = inClient.GetCollection<BsonDocument> collection

        async {
            let! r = collection.FindAsync(FilterDefinition.Empty, Database.options, token) |> Async.AwaitTask
            let! materialized = r.ToListAsync() |> Async.AwaitTask
            do materialized
                .GroupBy(fun x -> x.Elements |> Seq.find (fun y -> y.Name.Equals(field, StringComparison.OrdinalIgnoreCase)) |> fun y -> y.Value)
                .ToDictionary((fun x -> x.Key.AsString), fun x -> x.ToList())
            |> Seq.iter (fun x -> 
                Console.WriteLine (sprintf "Creating collection with name %s" x.Key)
                let newClient = outClient.GetCollection<BsonDocument> (x.Key.ToLower())
                async {
                    do! newClient.InsertManyAsync(x.Value) |> Async.AwaitTask
                } |> Async.StartImmediate
            )
        }
        
module Delete =
    open MongoDB.Bson

    let withName con db name =
        let client = Database.client con db
        async {
            Console.WriteLine (sprintf "Deleting collection with name %s" name)
            do! client.DropCollectionAsync name |> Async.AwaitTask
        }        

[<EntryPoint>]
let main argv =
    let inConnection = argv.[0] //connection string to db from "mongodb://localhost"
    let action = argv.[1] //migrate
    let db = argv.[2] //db
    let name = argv.[3] //collection
    let field = argv.[4] //documentType
    let outConnection = if (argv |> Array.length > 5) then argv.[5] else inConnection

    match action with
    | "migrate" -> async { do! Migrate.basedOn inConnection outConnection db name field CancellationToken.None } |> Async.RunSynchronously
    | "delete" -> async { do! Delete.withName inConnection db name } |> Async.RunSynchronously
    | _ -> "invalid action" |> ArgumentException |> raise

    0
