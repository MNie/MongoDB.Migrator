// Learn more about F# at http://fsharp.org

open System
open System.Threading
open System.Linq

module Database =
    open MongoDB.Bson
    open System
    open MongoDB.Driver

    let client db =
        let c = MongoClient("mongodb://localhost")
        c.GetDatabase db
        
    let options =
        let f = FindOptions<BsonDocument>()
        f.BatchSize <- Nullable<int> 100
        f

module Migrate =
    open MongoDB.Bson
    open MongoDB.Bson
    open MongoDB.Driver

    let basedOn db collection field token =
        let client = Database.client db
        let collection = client.GetCollection<BsonDocument> collection

        async {
            let! r = collection.FindAsync(FilterDefinition.Empty, Database.options, token) |> Async.AwaitTask
            let! materialized = r.ToListAsync() |> Async.AwaitTask
            do materialized
                .GroupBy(fun x -> x.Elements |> Seq.find (fun y -> y.Name.Equals(field, StringComparison.OrdinalIgnoreCase)) |> fun y -> y.Value)
                .ToDictionary((fun x -> x.Key.AsString), fun x -> x.ToList())
            |> Seq.iter (fun x -> 
                Console.WriteLine (sprintf "Creating collection with name %s" x.Key)
                let newClient = client.GetCollection<BsonDocument> (x.Key.ToLower())
                async {
                    do! newClient.InsertManyAsync(x.Value) |> Async.AwaitTask
                } |> Async.StartImmediate
            )
        }
        
module Delete =
    open MongoDB.Bson

    let withName db name =
        let client = Database.client db
        async {
            Console.WriteLine (sprintf "Deleting collection with name %s" name)
            do! client.DropCollectionAsync name |> Async.AwaitTask
        }        

[<EntryPoint>]
let main argv =
    let action = argv.[0] //migrate
    let db = argv.[1] //db
    let name = argv.[2] //collection
    let field = argv.[3] //documentType

    match action with
    | "migrate" -> async { do! Migrate.basedOn db name field CancellationToken.None } |> Async.RunSynchronously
    | "delete" -> async { do! Delete.withName db name } |> Async.RunSynchronously
    | _ -> "invalid action" |> ArgumentException |> raise

    0
