using CSSKin.Models;
using MongoDB.Driver;

namespace CSSKin.Core.Services;

public class WeaponServiceRepository : IServiceRepository<WeaponInfo>
{
    private readonly IMongoCollection<WeaponInfo> mongoCollection;
    
    public WeaponServiceRepository(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        client.StartSession();
        var database = client.GetDatabase(databaseName);

        mongoCollection = database.GetCollection<WeaponInfo>("UserSkins");
    }


    public WeaponInfo Create(WeaponInfo data)
    {
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
        mongoCollection.ReplaceOne(Data => Data._id == data._id, data);
    }
}