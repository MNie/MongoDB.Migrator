module Delete
    open Domain
    open Microsoft.Extensions.Logging

    let withName (req: MigrationRequest) (log: ILogger) =
        let client = Database.client req.``in`` req.db
        async {
            do log.LogInformation (sprintf "Deleting collection with name %s" req.collection)
            do! client.DropCollectionAsync req.collection |> Async.AwaitTask
            return Ok true
        }  
