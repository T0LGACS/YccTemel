using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace YccTemel;

public partial class YccTemel
{
    private void KomutlarKur()
    {
        foreach (var k in Config.Komutlar)
            foreach (var ad in k.Ad.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                AddCommand("css_" + ad, "YccTemel özel komut", (p, info) => OzelKomut(k, p, info));

        AddCommand("css_rr", "Oyunu yeniden başlatır", YenidenBaslat);
        AddCommand("css_restartgame", "Oyunu yeniden başlatır", YenidenBaslat);

        AddCommand("css_ip", "Sunucu adresini gösterir", (p, info) =>
        {
            if (p == null) { info.ReplyToCommand(Config.SunucuAdresi); return; }
            Sonra(() =>
            {
                if (!p.IsValid) return;
                Yaz(p, $"Bu sunucunun adresi: {{GREEN}}{Config.SunucuAdresi}");
                p.PrintToCenter("Sunucu adresi: " + Config.SunucuAdresi);
            });
        });

        // Panel için konsola oyuncu listesi: ||userid||STEAM_0:x:y||steamid64||TE/CT/SPEC||isim
        AddCommand("css_oyuncuverim", "Oyuncu listesini konsola yazar", (_, _) =>
        {
            foreach (var p in Utilities.GetPlayers().Where(Gercek))
            {
                var takim = p.Team switch { CsTeam.Terrorist => "TE", CsTeam.CounterTerrorist => "CT", CsTeam.Spectator => "SPEC", _ => "" };
                if (takim == "") continue;
                var sid = new SteamID(p.SteamID);
                Server.PrintToConsole($"||{p.UserId}||{sid.SteamId2}||{sid.SteamId64}||{takim}||{p.PlayerName}");
            }
        });
    }

    private void OzelKomut(KomutAyar k, CCSPlayerController? p, CommandInfo info)
    {
        if (p != null && !Yetkili(p, k.Yetki)) { Yaz(p, "{RED}Bu komut için yetkin yok."); return; }
        foreach (var c in k.Calistir) Server.ExecuteCommand(c);
        if (p == null) { if (k.Mesaj != "") info.ReplyToCommand(k.Mesaj); return; }
        Sonra(() =>
        {
            if (k.Mesaj != "") { if (k.Herkese) HerkeseYaz(k.Mesaj); else if (p.IsValid) Yaz(p, k.Mesaj); }
            if (k.Orta != "" && p.IsValid) p.PrintToCenter(k.Orta);
        });
    }

    // Sohbetten yazılan komutun cevabı oyuncunun "!grup" mesajından önce görünüyordu (komut, mesaj sohbete düşmeden
    // çalışıyor; sunucu sahibi 2026-11-15): cevap kısa bir süre sonra yazılır
    private void Sonra(Action f) => AddTimer(0.1f, f);

    private void YenidenBaslat(CCSPlayerController? p, CommandInfo info)
    {
        if (p != null && !Yetkili(p, "@css/changemap")) { Yaz(p, "{RED}Bu komut için yetkin yok."); return; }
        Server.ExecuteCommand("mp_restartgame 1");
    }
}
