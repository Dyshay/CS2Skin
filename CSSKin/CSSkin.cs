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
    private Dictionary<ulong, WeaponInfo> g_PlayersWeapons = new();
    private IServiceRepository<WeaponInfo> _usersService;

    public void OnConfigParsed(BaseConfig config)
    {
        Config = new BaseConfig();
    }


    public override void Load(bool hotReload)
    {
        Logger.LogInformation("Plugin loaded");

        _usersService = new WeaponServiceRepository(Config.ConnectionString, Config.DatabaseName);

        RegisterListener<Listeners.OnEntityCreated>(entity => { });

        RegisterListener<Listeners.OnEntitySpawned>(entity =>
        {
            CBasePlayerWeapon? pBasePlayerWeapon = new(entity.Handle);
            CEconEntity pCEconEntityWeapon = new(entity.Handle);

            Server.NextFrame(() =>
            {
                Logger.LogInformation(pCEconEntityWeapon.DesignerName);
                if (pCEconEntityWeapon != null && pCEconEntityWeapon.DesignerName != null &&
                    pCEconEntityWeapon.DesignerName.Contains("knife"))
                {
                    ushort weaponId = pCEconEntityWeapon.AttributeManager.Item.ItemDefinitionIndex;
                    int weaponOwner = (int)pBasePlayerWeapon.OwnerEntity.Index;
                    CBasePlayerPawn pBasePlayerPawn =
                        new CBasePlayerPawn(NativeAPI.GetEntityFromIndex(weaponOwner));

                    if (!pBasePlayerPawn.IsValid) return;

                    var playerIndex = (int)pBasePlayerPawn.Controller.Index;
                    var player = Utilities.GetPlayerFromIndex(playerIndex);
                    if (g_PlayersWeapons.TryGetValue(player.SteamID, out WeaponInfo weaponInfo))
                    {
                        pCEconEntityWeapon.FallbackPaintKit = weaponInfo.Paint;
                        pCEconEntityWeapon.FallbackSeed = weaponInfo.Seed;
                        pCEconEntityWeapon.FallbackWear = (float)weaponInfo.Wear;
                        pCEconEntityWeapon.FallbackStatTrak = -1;

                        pCEconEntityWeapon.AttributeManager.Item.ItemDefinitionIndex = (ushort)weaponInfo.DefIndex;
                        
                        Logger.LogInformation("def index {0}", weaponId);

                        pCEconEntityWeapon.AttributeManager.Item.ItemID = 16384;
                        pCEconEntityWeapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
                        pCEconEntityWeapon.AttributeManager.Item.ItemIDHigh = 16384 >> 32;

                        if (pBasePlayerWeapon.CBodyComponent is { SceneNode: not null })
                        {
                            var skeleton = GetSkeletonInstance(pBasePlayerWeapon.CBodyComponent.SceneNode);
                            skeleton.ModelState.MeshGroupMask = 2;
                        }

                        if (ConstantsWeapon.g_KnivesMap.TryGetValue(weaponId, out string knife_name))
                        {
                            if (!pBasePlayerWeapon.IsValid) return;
                            if (pBasePlayerWeapon.OwnerEntity.Value == null) return;
                            if (pBasePlayerWeapon.OwnerEntity.Index <= 0) return;
                            
                            Logger.LogInformation("knife name: {0}", knife_name);

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

        if (ConstantsWeapon.g_KnivesMap.TryGetValue(defIndex, out string? selectedWeaponKnifesName))
        {
            if (!player.IsValid || player.Index <= 0) return;

            if (!g_PlayersWeapons.ContainsKey(playerSteamId))
            {
                var weapon = new WeaponInfo()
                {
                    DefIndex = defIndex,
                    Paint = paintId,
                    Wear = 0.00000001f,
                    Seed = seed,
                    IsKnife = true,
                    steamid = player.SteamID.ToString()
                };
                g_PlayersWeapons.TryAdd(playerSteamId, weapon);
            }

            if (g_PlayersWeapons.TryGetValue(playerSteamId, out WeaponInfo weaponUser))
            {
                weaponUser.DefIndex = defIndex;
                weaponUser.Paint = paintId;
                weaponUser.Wear = 0.00000001f;
                weaponUser.Seed = seed;
                weaponUser.IsKnife = true;
                weaponUser.steamid = player.SteamID.ToString();
            }

            var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;
            CCSPlayer_ItemServices service = new(player.PlayerPawn.Value.ItemServices.Handle);

            foreach (var weaponData in weapons.Where(data =>
                         ConstantsWeapon.g_KnivesMap.ContainsKey(data.Value.AttributeManager.Item.ItemDefinitionIndex)))
            {
                if (weaponData.IsValid && weaponData.Value != null)
                {
                    if (ConstantsWeapon.g_KnivesMap.ContainsKey(weaponData.Value.AttributeManager.Item
                            .ItemDefinitionIndex))
                    {
                        service.DropActivePlayerWeapon(weaponData.Value);
                        player.RemoveItemByDesignerName("weapon_knife");
                        NativeAPI.IssueClientCommand((int)player.Index - 1, "slot3");
                    }
                }
            }

            Logger.LogInformation("gived weapon {0}", selectedWeaponKnifesName);
            player.GiveNamedItem(selectedWeaponKnifesName);
        }
    }

}