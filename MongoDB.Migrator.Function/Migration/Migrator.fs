module MongoDB.Migrator.Function

    open Microsoft.Azure.WebJobs
    open Microsoft.Azure.WebJobs.Extensions.Http
    open System.IO
    open Microsoft.AspNetCore.Http
    open System
    open System.Text
    open Newtonsoft.Json
    open DTO
    open Domain
    open Microsoft.AspNetCore.Mvc
    open System.Threading
    open Microsoft.Extensions.Logging

    [<FunctionName("Migrator")>]
    let public run ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = null)>] req : HttpRequest, log: ILogger)  =
        async {
            use reader = new StreamReader(req.Body, Encoding.UTF8)
            let! body = reader.ReadToEndAsync() |> Async.AwaitTask
            return JsonConvert.DeserializeObject<MigrationDTO>(body)
            |> MigrationRequest.create
            |> fun x -> 
                match x.action with
                | Action.Migrate -> async { return! Migrate.basedOn x log CancellationToken.None } |> Async.RunSynchronously
                | Action.Delete -> async { return! Delete.withName x log } |> Async.RunSynchronously
            |> fun x ->
                match x with
                | Ok _ -> OkObjectResult () :> ObjectResult
                | Error _ -> 
                    let r = ObjectResult ()
                    r.StatusCode <- Nullable<int>(500)
                    r
        } |> Async.StartAsTask