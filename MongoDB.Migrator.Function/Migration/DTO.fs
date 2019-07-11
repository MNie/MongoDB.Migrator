module DTO

    [<CLIMutable>]
    type MigrationDTO =
        {
            db: string
            action: string
            ``in``: string
            ``out``: string
            collection: string
            oldCollection: string
            letterSize: string
        }
    