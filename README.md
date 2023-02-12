# CosmosPatchDocument
Example patch document

This example shows how to update CosmosDb documents in bulk by changing a single property.

It generates 50000 car objects that will be Created. Evert Car has an IsSold property, initially set to false
The Inserts are done in bulk

After the creation is done, all exisiting cars are patched in bulk where IsSold will be set to true.

Create a CosmosDb account with a database and a collection.
For this sample to work the Collection should have a partitionkey "/manafacturer"

references
Partial Document Update
https://learn.microsoft.com/en-us/azure/cosmos-db/partial-document-update-getting-started?tabs=dotnet

Bulk Import Data 
https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/tutorial-dotnet-bulk-import
