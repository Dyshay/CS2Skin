using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CSSKin.Models;

public class WeaponInfo
{
    [BsonId]
    public long Id { get; set; }
    public int DefIndex { get; set; }
    public int Paint { get; set; }
    public int Seed { get; set; }
    public double Wear { get; set; }
    public bool IsKnife { get; set; }
    public string steamid { get; set; }
}