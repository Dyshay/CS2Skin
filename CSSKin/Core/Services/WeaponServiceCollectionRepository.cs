using CSSKin.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CSSKin.Core.Services;

public class WeaponServiceCollectionRepository : IServiceRepository<WeaponInfo>
{
    private readonly IMongoCollection<WeaponInfo> mongoCollection;
    
    public WeaponServiceCollectionRepository(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        client.StartSession();
        var database = client.GetDatabase(databaseName);

        mongoCollection = database.GetCollection<WeaponInfo>("UserSkins");
    }


    public WeaponInfo Create(WeaponInfo data)
    {
        var index = mongoCollection.CountDocuments(new BsonDocument()) + 1;
        data.Id = index;
        mongoCollection.InsertOne(data);
        return data;
    }

    public void Delete(string uuid)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<WeaponInfo?> Get()
    {
        return mongoCollection.Find(data => true).ToEnumerable();
    }

    public IEnumerable<WeaponInfo>? Get(string uuid)
    {
        return mongoCollection.Find(data => data.steamid == uuid).ToEnumerable();;
    }

    public void Update(WeaponInfo data)
    {
        mongoCollection.ReplaceOne(Data => Data.Id == data.Id, data);
    }
}