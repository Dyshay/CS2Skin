using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CSSKin.Core.Configs;
using CSSKin.Core.Services;
using CSSKin.Core.Utilities;
using CSSKin.Models;
using Microsoft.Extensions.Logging;

namespace CSSKin;

public class CSSkin : BasePlugin, IPluginConfig<BaseConfig>
{
    public override string ModuleName => "CsSkin";
    public override string ModuleVersion => "1.0.2";
    public BaseConfig Config { get; set; }
    private Dictionary<ulong, List<WeaponInfo>> g_PlayersWeapons = new();
    private IServiceRepository<WeaponInfo> _usersService;

    public void OnConfigParsed(BaseConfig config)
    {
        Config = config;
    }


    public override void Load(bool hotReload)
    {
        Logger.LogInformation("Plugin loaded");

        _usersService = new WeaponServiceRepository(Config.ConnectionString, Config.DatabaseName);

        RegisterListener<Listeners.OnClientAuthorized>((slot, id) =>
        {
            var skins = _usersService.Get(id.SteamId64.ToString());
            g_PlayersWeapons.TryAdd(id.SteamId64,
                skins != null ? skins.ToList() : new List<WeaponInfo>());
        });

        RegisterListener<Listeners.OnEntityCreated>(entity => { });

        RegisterListener<Listeners.OnEntitySpawned>(entity =>
        {
            CBasePlayerWeapon? pBasePlayerWeapon = new(entity.Handle);
            CEconEntity pCEconEntityWeapon = new(entity.Handle);

            Server.NextFrame(() =>
            {
                Logger.LogInformation(pCEconEntityWeapon.DesignerName);
                if (pCEconEntityWeapon != null && pCEconEntityWeapon.DesignerName != null && pCEconEntityWeapon.DesignerName.StartsWith("weapon_"))
                {
                    string designerName = pCEconEntityWeapon.DesignerName;
                    bool isKnife = designerName.Contains("knife") || designerName.Contains("bayonet");
                    bool isWeapon = designerName.Contains("weapon_") && !isKnife;

                    ushort weaponId = pCEconEntityWeapon.AttributeManager.Item.ItemDefinitionIndex;
                    int weaponOwner = (int)pBasePlayerWeapon.OwnerEntity.Index;

                    CBasePlayerPawn pBasePlayerPawn =
                        new CBasePlayerPawn(NativeAPI.GetEntityFromIndex(weaponOwner));

                    if (!pBasePlayerPawn.IsValid) return;

                    var playerIndex = (int)pBasePlayerPawn.Controller.Index;
                    var player = Utilities.GetPlayerFromIndex(playerIndex);
                    g_PlayersWeapons.TryGetValue(player.SteamID, out List<WeaponInfo> weaponsInfo);
                    var requestWeapon = weaponsInfo.FirstOrDefault(c => c.DefIndex == weaponId && !isKnife || isKnife && ConstantsWeapon.g_KnivesMap.ContainsValue(designerName));
                    if (requestWeapon != null)
                    {
                        var weaponInfo = weaponsInfo.FirstOrDefault(weapon =>
                            (weaponId == weapon.DefIndex && isWeapon && !isKnife) ||
                            ConstantsWeapon.g_KnivesMap.ContainsKey(weaponId));
                        pCEconEntityWeapon.FallbackPaintKit = weaponInfo.Paint;
                        pCEconEntityWeapon.FallbackSeed = weaponInfo.Seed;
                        pCEconEntityWeapon.FallbackWear = (float)weaponInfo.Wear;
                        pCEconEntityWeapon.FallbackStatTrak = -1;

                        pCEconEntityWeapon.AttributeManager.Item.ItemDefinitionIndex = (ushort)weaponInfo.DefIndex;

                        pCEconEntityWeapon.AttributeManager.Item.ItemID = 16384;
                        pCEconEntityWeapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
                        pCEconEntityWeapon.AttributeManager.Item.ItemIDHigh = 16384 >> 32;

                        if (pBasePlayerWeapon.CBodyComponent is { SceneNode: not null })
                        {
                            var skeleton = GetSkeletonInstance(pBasePlayerWeapon.CBodyComponent.SceneNode);
                            skeleton.ModelState.MeshGroupMask = 2;
                        }

                        if (ConstantsWeapon.g_KnivesMap.ContainsKey(weaponId))
                        {
                            Server.ExecuteCommand($"i_subclass_change {weaponInfo.DefIndex} {entity.Index}");
                        }
                    }
                }
            });
        });

        RegisterListener<Listeners.OnEntityParentChanged>((entity, parent) => { });

        RegisterListener<Listeners.OnEntityDeleted>(entity => { });
        base.Load(hotReload);
    }

    private static CSkeletonInstance GetSkeletonInstance(CGameSceneNode node)
    {
        Func<nint, nint> GetSkeletonInstance = VirtualFunction.Create<nint, nint>(node.Handle, 8);
        return new CSkeletonInstance(GetSkeletonInstance(node.Handle));
    }

    // Commands can also be registered using the `Command` attribute.
    [ConsoleCommand("css_skin", "Get skin")]
    // The `CommandHelper` attribute can be used to provide additional information about the command.
    [CommandHelper(minArgs: 1, usage: "[defIndex] [paintId] [seed]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCssSkinCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var defIndex = int.Parse(commandInfo.GetArg(1));
        var paintId = int.Parse(commandInfo.GetArg(2));
        var seed = int.Parse(commandInfo.GetArg(3));

        ulong playerSteamId = player!.SteamID;

        bool isKnife = ConstantsWeapon.g_KnivesMap.ContainsKey(defIndex);
        bool isWeapon = ConstantsWeapon.g_WeaponsMap.ContainsKey(defIndex);


        if (!player.IsValid || player.Index <= 0) return;

        var skins = _usersService.Get(playerSteamId.ToString()).ToList();

        if (isKnife)
        {
            var skin = skins.FirstOrDefault(data => data.IsKnife);
            if (skin != null)
            {
                skin.DefIndex = defIndex;
                skin.Paint = paintId;
                skin.Wear = 0.00000001f;
                skin.Seed = seed;
                skin.IsKnife = true;
                skin.steamid = player.SteamID.ToString();
                _usersService.Update(skin);
            }
            else
            {
                var newSkin = new WeaponInfo()
                {
                    steamid = playerSteamId.ToString(),
                    Seed = seed,
                    Wear = 0.00000001f,
                    IsKnife = true,
                    DefIndex = defIndex,
                    Paint = paintId
                };
                _usersService.Create(newSkin);
            }

        }

        if (isWeapon)
        {
            var skin = skins.FirstOrDefault(data => data.DefIndex == defIndex);
            if (skin != null)
            {
                skin.DefIndex = defIndex;
                skin.Paint = paintId;
                skin.Wear = 0.00000001f;
                skin.Seed = seed;
                skin.IsKnife = false;
                skin.steamid = player.SteamID.ToString();
                _usersService.Update(skin);
            }
            else
            {
                var newSkin = new WeaponInfo()
                {
                    steamid = playerSteamId.ToString(),
                    Seed = seed,
                    Wear = 0.00000001f,
                    IsKnife = false,
                    DefIndex = defIndex,
                    Paint = paintId
                };
                _usersService.Create(newSkin);
            }
        }

        g_PlayersWeapons[playerSteamId] = _usersService.Get(playerSteamId.ToString()).ToList();

        var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;
        CCSPlayer_ItemServices service = new(player.PlayerPawn.Value.ItemServices.Handle);

        foreach (var weaponData in weapons.Where(data =>
                     ConstantsWeapon.g_KnivesMap.ContainsKey(data.Value.AttributeManager.Item.ItemDefinitionIndex)))
        {
            if (weaponData.IsValid && weaponData.Value != null)
            {
                if (ConstantsWeapon.g_KnivesMap.ContainsKey(weaponData.Value.AttributeManager.Item
                        .ItemDefinitionIndex) || ConstantsWeapon.g_WeaponsMap.ContainsKey(weaponData.Value
                        .AttributeManager.Item
                        .ItemDefinitionIndex))
                {
                    if (isWeapon)
                    {
                        player.RemoveItemByDesignerName(weaponData.Value.DesignerName);
                    }

                    if (isKnife)
                    {
                        player.RemoveItemByDesignerName("weapon_knife");
                        NativeAPI.IssueClientCommand((int)player.Index - 1, "slot3");
                    }
                }
            }
        }

        if (ConstantsWeapon.g_WeaponsMap.TryGetValue(defIndex, out string weapon_name))
        {
            player.GiveNamedItem(weapon_name);
        }

        if (ConstantsWeapon.g_KnivesMap.TryGetValue(defIndex, out string knife_name))
        {
            player.GiveNamedItem("weapon_knife");
        }
    }
}