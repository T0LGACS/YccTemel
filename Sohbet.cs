using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;

namespace YccTemel;

public partial class YccTemel
{
    // CS2 sohbet mesajı (CUserMessageSayText2). ZirhAyari'deki gibi id elle: FindIdByName bu CSS'te hata veriyor.
    private const int SayText2 = 118;
    private string _kayitKlasoru = "";

    private void SohbetKur()
    {
        var kok = Path.GetDirectoryName(Path.GetDirectoryName(ModuleDirectory))!; // .../addons/counterstrikesharp
        _kayitKlasoru = Path.Combine(kok, Config.Sohbet.KayitKlasoru);
        Directory.CreateDirectory(_kayitKlasoru);
        EskiKayitlariSil();
        RegisterListener<Listeners.OnMapStart>(_ => EskiKayitlariSil());

        AddCommandListener("say", (p, i) => Soyledi(p, i, false));
        AddCommandListener("say_team", (p, i) => Soyledi(p, i, true));

        // Etiket ve gözcü, oyunun gerçekten gönderdiği sohbet mesajı üzerinden yapılır. SwearGuard/KickGuard/ReklamGuard ya da
        // susturma bir mesajı engellediyse bu mesaj hiç oluşmaz; eski etiket eklentisi "say"i yeniden bastığı için yükleme
        // sırasına göre engellenen takım mesajlarını yine gösterebiliyordu.
        HookUserMessage(SayText2, SohbetMesaji);

        if (Config.Sohbet.SkorEtiketi != "")
        {
            RegisterEventHandler<EventPlayerConnectFull>((e, _) => { SkorEtiketiSonra(e.Userid); return HookResult.Continue; });
            RegisterEventHandler<EventPlayerSpawn>((e, _) => { SkorEtiketiSonra(e.Userid); return HookResult.Continue; });
        }
    }

    private HookResult Soyledi(CCSPlayerController? p, CommandInfo info, bool takim)
    {
        if (!Gercek(p)) return HookResult.Continue;
        var mesaj = info.GetArg(1).Trim();
        if (mesaj.Length == 0) return HookResult.Continue;

        var simdi = DateTime.Now;
        // Komutlar (!ws, /knife...) da Discord'a gider (kullanıcı 2026-11-15); dosya kaydına girmez, panel ve bekçi eskisi gibi
        DiscordGonder(p!.PlayerName, p.SteamID, Metin.Doldur(Config.Discord.Bicim,
            ("SAAT", simdi.ToString("HH:mm:ss")), ("TAKIM", takim ? "[TEAM]" : "[ALL]"), ("MESAJ", mesaj)));
        if (!Metin.Kaydedilmez(mesaj, Config.Sohbet.KayitDisi))
        {
            var satir = Metin.KayitSatiri(simdi, p.SteamID, Metin.IpYalin(p.IpAddress), takim, p.PlayerName, mesaj);
            try { File.AppendAllText(Path.Combine(_kayitKlasoru, Metin.KayitDosyasi(simdi)), satir + Environment.NewLine); }
            catch (Exception e) { Console.WriteLine("[YccTemel] sohbet kaydı yazılamadı: " + e.Message); }
        }

        // Takım sohbetinde "@" ile başlayan mesaj sadece adminlere gider (admin sohbeti)
        if (takim && mesaj.StartsWith('@') && mesaj.Length > 1 && Yetkili(p!, Config.Sohbet.AdminYetki))
        {
            foreach (var a in Utilities.GetPlayers().Where(x => Gercek(x) && Yetkili(x, Config.Sohbet.AdminYetki)))
                Yaz(a, $"{{LIME}}(ADMIN) {{DEFAULT}}{p!.PlayerName}: {mesaj[1..]}");
            return HookResult.Handled;
        }
        return HookResult.Continue;
    }

    private HookResult SohbetMesaji(UserMessage um)
    {
        string tur, isim, mesaj;
        int index;
        try
        {
            tur = um.ReadString("messagename");
            if (!tur.Contains("Cstrike_Chat_")) return HookResult.Continue;   // ad değişikliği vb. sohbet dışı mesajlar
            isim = um.ReadString("param1");
            mesaj = um.ReadString("param2");
            index = um.ReadInt("entityindex");
        }
        catch { return HookResult.Continue; }

        var p = Utilities.GetPlayerFromIndex(index);
        if (!Gercek(p)) return HookResult.Continue;
        var takim = !tur.Contains("_All");
        var alicilar = um.Recipients.Where(x => x != null && x.IsValid).ToList();

        Etiket? e = null;
        foreach (var et in Config.Sohbet.Etiketler)
            if ((takim ? et.Takim : et.Genel) && (et.Kim == "herkes" || Yetkili(p!, et.Kim))) { e = et; break; }

        var oluMu = !p!.PawnIsAlive;
        var takimAdi = p.Team switch { CsTeam.Terrorist => "{GOLD}(T) ", CsTeam.CounterTerrorist => "{LIGHTBLUE}(CT) ", _ => "{GREY}(İzleyici) " };
        var bas = (oluMu ? "{DEFAULT}☠ " : "") + (takim ? takimAdi : "");
        var gozculer = takim
            ? Utilities.GetPlayers().Where(x => Gercek(x) && x.Team != p.Team && !alicilar.Contains(x) && Yetkili(x, Config.Sohbet.AdminYetki)).ToList()
            : new List<CCSPlayerController>();

        if (e == null && gozculer.Count == 0) return HookResult.Continue;

        var satir = e == null ? null : $"{bas}{e.On}{(e.On == "" ? "" : " ")}{e.IsimRenk}{isim}{{DEFAULT}}: {e.MesajRenk}{mesaj}";
        var gozcuSatir = $"{{GREY}}[Gözcü] {takimAdi}{(oluMu ? "☠ " : "")}{isim}: {mesaj}";
        // Mesaj kancasının içinden yeni mesaj gönderilmez, bir sonraki karede gönderilir
        Server.NextFrame(() =>
        {
            if (satir != null) foreach (var a in alicilar) if (a.IsValid) Yaz(a, satir);
            foreach (var g in gozculer) if (g.IsValid) Yaz(g, gozcuSatir);
        });
        return satir != null ? HookResult.Handled : HookResult.Continue;
    }

    private void SkorEtiketiSonra(CCSPlayerController? p)
    {
        if (!Gercek(p)) return;
        AddTimer(1.5f, () =>
        {
            // YccSkin'in "[FACEIT 7]" etiketi ezilmez (2026-11-19)
            if (!Gercek(p) || p!.Clan == Config.Sohbet.SkorEtiketi || p.Clan.StartsWith("[FACEIT")) return;
            p.Clan = Config.Sohbet.SkorEtiketi;
            Utilities.SetStateChanged(p, "CCSPlayerController", "m_szClan");
        });
    }

    private void EskiKayitlariSil()
    {
        try
        {
            var sinir = DateTime.Now.AddDays(-Config.Sohbet.KayitGun);
            foreach (var f in Directory.GetFiles(_kayitKlasoru, "*.txt"))
                if (File.GetLastWriteTime(f) < sinir) File.Delete(f);
        }
        catch (Exception e) { Console.WriteLine("[YccTemel] eski sohbet kayıtları silinemedi: " + e.Message); }
    }
}
