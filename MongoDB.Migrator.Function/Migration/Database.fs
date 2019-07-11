module Database
    open MongoDB.Bson
    open System
    open MongoDB.Driver

    let client (con: string) db =
        let c = MongoClient(con)
        c.GetDatabase db
        
    let options =
        let f = FindOptions<BsonDocument>()
        f.BatchSize <- Nullable<int> 100
        f.NoCursorTimeout <- Nullable<bool>(true)
        f
