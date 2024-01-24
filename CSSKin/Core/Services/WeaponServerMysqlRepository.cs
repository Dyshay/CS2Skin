using CSSKin.Models;
using Dapper;
using MySqlConnector;

namespace CSSKin.Core.Services;

public class WeaponServerMysqlRepository : IServiceRepository<WeaponInfo>
{
    private MySqlConnection _connection;
    private MySqlCommand _command;
    private string TableName;

    public WeaponServerMysqlRepository(string connectionString, string tableName)
    {
        TableName = tableName;
        _connection = new MySqlConnection(connectionString);
        _connection.Open();
        _connection.Query(
            $"CREATE TABLE IF NOT EXISTS {TableName} (Id BIGINT PRIMARY KEY auto_increment,DefIndex INT,Paint INT, Seed INT, Wear DOUBLE NOT NULL, IsKnife BOOLEAN NOT NULL, steamid VARCHAR(255) NOT NULL);");
        _connection.Close();
    }

    public WeaponInfo Create(WeaponInfo data)
    {
        _connection.Open();
        var newEntityId = _connection.QuerySingle<int>(
            $"INSERT INTO {TableName} (DefIndex, Paint, Seed, Wear, IsKnife, steamid) VALUES ({data.DefIndex}, {data.Paint}, {data.Seed}, {data.Wear}, {data.IsKnife}, '{data.steamid}');");
        _connection.Close();
        return Get(data.steamid).First(c => c.Id == newEntityId);
    }

    public void Delete(string uuid)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<WeaponInfo?> Get()
    {
        _connection.Open();
        List<WeaponInfo> skins = _connection.Query<WeaponInfo>($"SELECT * FROM {TableName}").ToList();
        _connection.Close();
        return skins;
    }

    public IEnumerable<WeaponInfo>? Get(string uuid)
    {
        _connection.Open();
        List<WeaponInfo> skins = _connection.Query<WeaponInfo>($"SELECT * FROM {TableName}").ToList();
        _connection.Close();
        return skins;
    }

    public void Update(WeaponInfo data)
    {
        _connection.Open();
        _connection.Query(
            $"UPDATE {TableName} SET DefIndex = {data.DefIndex},Paint = {data.Paint},Seed = {data.Seed},Wear = {data.Wear},IsKnife = {data.IsKnife},steamid = '{data.steamid}' WHERE Id = {data.Id};");
        _connection.Close();
    }
}