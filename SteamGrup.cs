using System.Collections.Concurrent;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace YccTemel;

// Bazı sohbet komutları (!ws, !knife...) sadece Steam grubu üyelerine açık. Üyelik oyuncu girince Steam API'den
// sorulur ve hatırlanır; !group_check ile oyuncu tekrar sorgulatabilir (gruba yeni katıldıysa).
public partial class YccTemel
{
    private readonly ConcurrentDictionary<ulong, bool> _uye = new();

    private void SteamGrupKur()
    {
        var a = Config.SteamGrup;
        if (a.GrupId == "" || a.ApiAnahtari == "" || a.Komutlar.Count == 0) return;
        var kapali = new HashSet<string>(a.Komutlar, StringComparer.OrdinalIgnoreCase);

        RegisterEventHandler<EventPlayerConnectFull>((e, i) => { if (Gercek(e.Userid)) _ = UyeMi(e.Userid!.SteamID); return HookResult.Continue; });
        RegisterEventHandler<EventPlayerDisconnect>((e, i) => { if (e.Userid != null) _uye.TryRemove(e.Userid.SteamID, out _); return HookResult.Continue; });
        // Eklenti oyun sırasında yüklendiyse içerdekileri sor. Sunucu açılırken oyuncu listesi henüz yok ve hata veriyor (2026-11-15).
        try { foreach (var p in Utilities.GetPlayers().Where(Gercek)) _ = UyeMi(p.SteamID); } catch { }

        foreach (var komut in new[] { "say", "say_team" })
            AddCommandListener(komut, (p, info) =>
            {
                if (!Gercek(p) || !kapali.Contains(Metin.IlkKelime(info.GetArg(1)))) return HookResult.Continue;
                if (_uye.TryGetValue(p!.SteamID, out var uye) && uye) return HookResult.Continue;
                Yaz(p, a.Yasak);
                return HookResult.Handled;
            });

        AddCommand("css_group_check", "Steam grubu üyeliğini yeniden sorar", (p, i) =>
        {
            if (!Gercek(p)) return;
            var sid = p!.SteamID;
            _uye.TryRemove(sid, out _);
            Task.Run(async () =>
            {
                var uye = await UyeMi(sid);
                Server.NextFrame(() => { if (Gercek(p)) Yaz(p, uye ? a.Uye : a.UyeDegil); });
            });
        });
    }

    private async Task<bool> UyeMi(ulong sid)
    {
        if (_uye.TryGetValue(sid, out var bilinen)) return bilinen;
        var a = Config.SteamGrup;
        try
        {
            using var d = JsonDocument.Parse(await Http.GetStringAsync(
                $"https://api.steampowered.com/ISteamUser/GetUserGroupList/v1/?key={a.ApiAnahtari}&steamid={sid}"));
            var r = d.RootElement.GetProperty("response");
            // Profili gizli olanlarda success=false gelir: üye değil sayılır (eski eklentiyle aynı)
            var uye = r.TryGetProperty("groups", out var g) && g.EnumerateArray().Any(x => x.GetProperty("gid").GetString() == a.GrupId);
            _uye[sid] = uye;
            return uye;
        }
        catch (Exception e)
        {
            Console.WriteLine("[YccTemel] Steam grup sorgusu başarısız: " + e.Message);
            return false;   // hatırlanmaz, bir sonraki !group_check ya da girişte tekrar sorulur
        }
    }
}
