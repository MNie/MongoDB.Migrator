# MongoDB.Migrator
Azure function which helps you migrate records from one colleciton to multiple ones after revolution in pricing in Azure CosmosDB.

Usage:

1. Deploy to AzureFunctions,
2. Open Insomnia/Postman or other rest client,
3. Run query [POST]
```
{
	"db": "databaseName",
	"action": "migrate",
	"in": "migrateFromConnectionString",
	"out": "migrateToConnectionString",
	"collection": "newCollectionName (typename)",
	"oldCollection": "oldSharedCollection",
	"letterSize": "namingConvention (starting from small or big letter)"
}
```
4. Smile because of better separation of collections.
