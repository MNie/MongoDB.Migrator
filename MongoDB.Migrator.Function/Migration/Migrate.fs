module Migrate
    open MongoDB.Bson
    open MongoDB.Driver
    open System
    open System.Linq
    open DTO

    let basedOn inConnection outConnection db collection field letterSize token =
        let inClient = Database.client inConnection db
        let outClient = Database.client outConnection db
        let collection = inClient.GetCollection<BsonDocument> collection

        let filter = 
            match letterSize with
            | LetterSize.Big -> BsonDocument.Parse(sprintf "{'Environment': 'prd', 'TypeName': '%s'}" field)
            | LetterSize.Small -> BsonDocument.Parse(sprintf "{'environment': 'prd', 'typeName': '%s'}" field)

        let group =
            match letterSize with
            | LetterSize.Big -> "TypeName"
            | LetterSize.Small -> "typeName"

        let byEnvAndCollection = BsonDocumentFilterDefinition(filter)
        let rec doSmth skip how =
            async {
                let rec tryGetData (i) =
                    if i > 3 then
                        "I tried to get data %A times but it always fails :(" |> ArgumentException |> raise
                    
                    async {
                        let! r = collection.FindAsync(byEnvAndCollection, Database.options, token) |> Async.AwaitTask
                        let m = r.ToEnumerable().Skip(skip).Take(how).ToList()
                        return m
                            .GroupBy(fun x -> x.Elements |> Seq.find (fun y -> y.Name.Equals(group, StringComparison.OrdinalIgnoreCase)) |> fun y -> y.Value)
                            .ToDictionary((fun x -> x.Key.AsString), fun x -> x.ToList())
                    } |> Async.Catch
                        
                let rec materialized (i) = 
                    async {
                        match! tryGetData (i) with 
                        | Choice1Of2 c -> return c
                        | Choice2Of2 d -> 
                            Console.WriteLine (sprintf "Something went wrong when fething data with following parameters %A %A and error: %A" skip how d)
                            return! materialized (i + 1)
                    }
                    
                let! mat = materialized (0)
                Console.WriteLine (sprintf "Currently processing data with following parameters %A %A" skip how)
                mat
                |> Seq.iter (fun x -> 
                    Console.WriteLine (sprintf "Getting collection with name %s" x.Key)
                    let newClient = outClient.GetCollection<BsonDocument> (x.Key)
                    x.Value
                    |> Seq.split 300
                    |> Seq.iter (fun y -> 
                        async {
                            Console.WriteLine("Waiting a second to insert something")
                            do System.Threading.Thread.Sleep(1001)
                            try
                                do Console.WriteLine (sprintf "insert records")
                                do! newClient.InsertManyAsync(y, InsertManyOptions(), token) |> Async.AwaitTask
                            with
                                | ex -> 
                                    Console.WriteLine(sprintf "Something went wrong for collection with name: %s" x.Key)
                                    Console.WriteLine(ex)
                        } |> Async.RunSynchronously
                    )
                )
                if (mat |> Seq.length) > 0 then return! doSmth (skip + how) how
                else return Ok true
            }
        async {
            return! doSmth 0 5000
        }