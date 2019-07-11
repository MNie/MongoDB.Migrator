module Delete
    open System

    let withName con db name =
        let client = Database.client con db
        async {
            Console.WriteLine (sprintf "Deleting collection with name %s" name)
            do! client.DropCollectionAsync name |> Async.AwaitTask
            return Ok true
        }  
