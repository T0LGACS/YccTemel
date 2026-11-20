using CounterStrikeSharp.API.Core;

namespace YccTemel;

// Doğuşta Deagle yerine Revolver (AWP sunucusu, sunucu sahibi 2026-11-19). mp_*_default_secondary "weapon_revolver" yetmiyor:
// Deagle ile Revolver envanterde aynı yuvada, oyun oyuncunun kendi seçimini veriyor (sahibinde Deagle seçiliydi, Deagle geldi).
// Doğduktan kısa süre sonra elindeki Deagle alınır, Revolver verilir. Ayar: YccTemel.json "DeagleYerineRevolver": true
public partial class YccTemel
{
    private const ushort DeagleNo = 1, RevolverNo = 64;   // ItemDefinitionIndex

    private void RevolverKur()
    {
        if (!Config.DeagleYerineRevolver) return;
        RegisterEventHandler<EventPlayerSpawn>((e, _) =>
        {
            if (e.Userid is { IsValid: true } p) AddTimer(0.2f, () =>
            {
                // Eldeki silahı silmek oyuncunun oyununu "CopyExistingEntity: missing client entity" ile çökertiyordu (2026-11-20):
                // önce bıçağa geçilir, Deagle sonra silinir
                if (!p.IsValid || !p.PawnIsAlive) return;
                p.ExecuteClientCommand("slot3");
                AddTimer(0.15f, () => DeagleDegistir(p));
            });
            return HookResult.Continue;
        });
    }

    private static void DeagleDegistir(CCSPlayerController p)
    {
        if (!p.IsValid || !p.PawnIsAlive) return;
        var ws = p.PlayerPawn.Value?.WeaponServices;
        var deagle = ws?.MyWeapons.Select(h => h.Value).FirstOrDefault(w => w is { IsValid: true } && w.AttributeManager.Item.ItemDefinitionIndex == DeagleNo);
        if (deagle == null) return;
        if (!p.IsBot && ws!.ActiveWeapon.Value?.Index == deagle.Index) return;   // bıçağa geçmediyse dokunma (botun oyunu yok, çökmez)
        deagle.Remove();
        p.GiveNamedItem("weapon_revolver");
        p.ExecuteClientCommand("slot2");
    }
}
