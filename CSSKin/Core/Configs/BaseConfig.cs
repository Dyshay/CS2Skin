using CounterStrikeSharp.API.Core;

namespace CSSKin.Core.Configs;

public class BaseConfig : IBasePluginConfig
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
    public int Version { get; set; }
}