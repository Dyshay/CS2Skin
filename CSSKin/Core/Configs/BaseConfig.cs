using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CSSKin.Core.Enums;

namespace CSSKin.Core.Configs;

public class BaseConfig : IBasePluginConfig
{
    public string ConnectionString { get; set; } = "";
    public string MongoDatabaseName { get; set; } = "";
    public string MysqlTableName { get; set; } = "";
    [JsonPropertyName("DatabaseType")]
    public string DbType { get; set; } = Enum.GetName(DatabaseType.MONGODB);
    public int Version { get; set; }
}