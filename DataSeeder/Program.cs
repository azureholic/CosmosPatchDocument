using Bogus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Models;
using System.Text.Json;

var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>();

IConfiguration config = builder.Build();

var bogusCars = new Faker<Car>()
               .StrictMode(true)
               .RuleFor(c => c.Id, f => Guid.NewGuid().ToString())
               .RuleFor(c => c.Vin, f => f.Vehicle.Vin())
               .RuleFor(c => c.Manufacturer, f => f.Vehicle.Manufacturer())
               .RuleFor(c => c.Fuel, f => f.Vehicle.Fuel())
               .RuleFor(c => c.Type, f => f.Vehicle.Type())
               .RuleFor(c => c.IsSold, f => false);


var cars = bogusCars.Generate(50000);

CosmosSerializationOptions options = new CosmosSerializationOptions();
options.PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase;

string connectionString = config["CosmosConnectionString"];
CosmosClient client = new CosmosClient(connectionString,
new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Direct,
    AllowBulkExecution = true,
    SerializerOptions = options
}) ;


var container = client.GetContainer(config["CosmosDatabaseName"], config["CosmosContainerName"]);

Console.WriteLine($"Inserting {cars.Count} cars....");
var startTime = DateTime.UtcNow;

List<Task> tasks = new List<Task>(cars.Count);
foreach (Car car in cars)
{
    tasks.Add(container.CreateItemAsync<Car>(car, new PartitionKey(car.Manufacturer))
        .ContinueWith(itemResponse =>
        {
            if (!itemResponse.IsCompletedSuccessfully)
            {
                AggregateException innerExceptions = itemResponse.Exception.Flatten();
                if (innerExceptions.InnerExceptions.FirstOrDefault(innerEx => innerEx is CosmosException) is CosmosException cosmosException)
                {
                    Console.WriteLine($"Received {cosmosException.StatusCode} ({cosmosException.Message}).");
                }
                else
                {
                    Console.WriteLine($"Exception {innerExceptions.InnerExceptions.FirstOrDefault()}.");
                }
            }
        }));
}

// Wait until all are done
await Task.WhenAll(tasks);

var endTime = DateTime.UtcNow;

var ts = endTime - startTime;
Console.WriteLine($"Inserts duration {ts.Minutes} minutes and {ts.Seconds} seconds");

Console.WriteLine($"Patching {cars.Count} cars to status isSold = true....");
startTime = DateTime.UtcNow;
foreach (var car in cars)
{

    tasks.Add(container.PatchItemAsync<Car>(
        id: car.Id,
        partitionKey: new PartitionKey(car.Manufacturer),
        patchOperations: new[] {
        PatchOperation.Replace("/isSold", true)
        })
        .ContinueWith(itemResponse =>
        {
            if (!itemResponse.IsCompletedSuccessfully)
            {
                AggregateException innerExceptions = itemResponse.Exception.Flatten();
                if (innerExceptions.InnerExceptions.FirstOrDefault(innerEx => innerEx is CosmosException) is CosmosException cosmosException)
                {
                    Console.WriteLine($"Received {cosmosException.StatusCode} ({cosmosException.Message}).");
                }
                else
                {
                    Console.WriteLine($"Exception {innerExceptions.InnerExceptions.FirstOrDefault()}.");
                }
            }
        }));
}

// Wait until all are done
await Task.WhenAll(tasks);

endTime = DateTime.UtcNow;
ts = endTime - startTime;
Console.WriteLine($"Patch duration {ts.Minutes} minutes and {ts.Seconds} seconds");
