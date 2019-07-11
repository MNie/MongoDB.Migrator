module Domain
    open DTO

    type Action = Migrate | Delete
    type LetterSize = Small | Big

    type MigrationRequest =
        {
            db: string
            action: Action
            ``in``: string
            ``out``: string
            collection: string
            oldCollection: string
            letterSize: LetterSize
        }

    module MigrationRequest =
        let create (dto: MigrationDTO): MigrationRequest =
            {
                db = dto.db
                action = match dto.action with | "delete" -> Action.Delete | _ -> Action.Migrate
                ``in`` = dto.``in``
                ``out`` = dto.``out``
                collection = dto.collection
                oldCollection = dto.oldCollection
                letterSize = match dto.letterSize with | "small" -> LetterSize.Small | _ -> LetterSize.Big
            }

