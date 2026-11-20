using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using MySqlConnector;

namespace YccTemel;

// Sezon VIP'i (2026-11-20): her sunucunun sezon ilk 3'ü bir sonraki sezon boyunca VIP olur (ycc_vip tablosu, VDS'teki
// sezon-bitir.sh yazar). VIP: skor tablosunda "[S1 AIM #1]" etiketi (FACEIT ve [YCC] etiketinin önüne geçer), sohbette
// etiket + kendi seçtiği isim/mesaj rengi (!renk, !mesajrenk). Admin sohbet etiketi VIP'ten önce gelir.
public partial class YccTemel
{
    public record VipBilgi(string Etiket, string IsimRenk, string MesajRenk, long Bitis);

    // Seçilebilen renkler (admin kırmızısı yok, okunmayan koyu renkler yok)
    public static readonly string[] VipRenkler = { "GOLD", "YELLOW", "ORANGE", "LIME", "GREEN", "OLIVE", "LIGHTBLUE", "BLUE", "PURPLE", "MAGENTA", "LIGHTRED", "DEFAULT" };

    private readonly Dictionary<ulong, VipBilgi> _vip = new();   // sadece ana iş parçacığı
    private string _vipBaglanti = "";

    private void VipKur()
    {
        var a = Config.Vip;
        if (!a.Acik) return;
        _vipBaglanti = new MySqlConnectionStringBuilder
        {
            Server = a.Sunucu, Port = (uint)a.Port, UserID = a.Kullanici, Password = a.Sifre, Database = a.Ad,
            CharacterSet = "utf8mb4", ConnectionTimeout = 5, DefaultCommandTimeout = 10, Pooling = true,
        }.ConnectionString;

        RegisterEventHandler<EventPlayerConnectFull>((e, _) => { VipYukle(e.Userid); return HookResult.Continue; });
        RegisterEventHandler<EventPlayerDisconnect>((e, _) => { if (e.Userid != null) _vip.Remove(e.Userid.SteamID); return HookResult.Continue; });
        foreach (var p in Utilities.GetPlayers().Where(Gercek)) VipYukle(p);   // eklenti yeniden yüklendiyse

        AddCommand("css_vip", "VIP durumun", (p, _) => VipDurum(p));
        AddCommand("css_renk", "VIP: sohbette isim rengi (!renk <no>)", (p, i) => RenkSec(p, i, false));
        AddCommand("css_mesajrenk", "VIP: sohbette mesaj rengi (!mesajrenk <no>)", (p, i) => RenkSec(p, i, true));
    }

    private void VipYukle(CCSPlayerController? p)
    {
        if (!Gercek(p)) return;
        var sid = p!.SteamID;
        Task.Run(async () =>
        {
            try
            {
                await using var c = new MySqlConnection(_vipBaglanti);
                await c.OpenAsync();
                await using var k = new MySqlCommand("SELECT etiket, isim_renk, mesaj_renk, bitis FROM ycc_vip WHERE steamid = @s AND bitis > UNIX_TIMESTAMP()", c);
                k.Parameters.AddWithValue("@s", sid);
                await using var r = await k.ExecuteReaderAsync();
                VipBilgi? v = await r.ReadAsync() ? new VipBilgi(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt64(3)) : null;
                Server.NextFrame(() =>
                {
                    if (v == null) { _vip.Remove(sid); return; }
                    _vip[sid] = v;
                    if (Gercek(p) && p.SteamID == sid) SkorEtiketiSonra(p);
                });
            }
            catch (Exception e) { Console.WriteLine("[YccTemel] VIP okunamadı: " + e.Message); }
        });
    }

    private void VipDurum(CCSPlayerController? p)
    {
        if (!Gercek(p)) return;
        if (!_vip.TryGetValue(p!.SteamID, out var v))
        {
            Yaz(p, "VIP değilsin. Her ay sezon sonunda her sunucunun {GOLD}ilk 3{DEFAULT}'ü bir sonraki sezon boyunca VIP olur. Sıran için {GREEN}!sezon");
            return;
        }
        var bitis = DateTimeOffset.FromUnixTimeSeconds(v.Bitis).ToLocalTime();
        Yaz(p, $"{{GOLD}}VIP {v.Etiket}{{DEFAULT}} | bitiş: {{GOLD}}{bitis:dd.MM.yyyy HH:mm}");
        Yaz(p, $"İsim rengin: {{{v.IsimRenk}}}{v.IsimRenk}{{DEFAULT}} | Mesaj rengin: {{{v.MesajRenk}}}{v.MesajRenk}");
        Yaz(p, "Değiştirmek için {GREEN}!renk <no>{DEFAULT} (isim) ya da {GREEN}!mesajrenk <no>{DEFAULT} (mesaj):");
        Yaz(p, string.Join(" ", VipRenkler.Select((r, i) => $"{{{r}}}{i + 1}.{r}")));
    }

    private void RenkSec(CCSPlayerController? p, CommandInfo info, bool mesaj)
    {
        if (!Gercek(p)) return;
        var sid = p!.SteamID;
        if (!_vip.TryGetValue(sid, out var v)) { Yaz(p, "Bu komut sadece VIP'ler için. Bilgi: {GREEN}!vip"); return; }
        if (!int.TryParse(info.GetArg(1), out var no) || no < 1 || no > VipRenkler.Length) { VipDurum(p); return; }
        var renk = VipRenkler[no - 1];
        _vip[sid] = mesaj ? v with { MesajRenk = renk } : v with { IsimRenk = renk };
        Yaz(p, $"{(mesaj ? "Mesaj" : "İsim")} rengin artık {{{renk}}}{renk}{{DEFAULT}}.");
        var sutun = mesaj ? "mesaj_renk" : "isim_renk";   // sabit iki değerden biri, kullanıcıdan gelmez
        Task.Run(async () =>
        {
            try
            {
                await using var c = new MySqlConnection(_vipBaglanti);
                await c.OpenAsync();
                await using var k = new MySqlCommand($"UPDATE ycc_vip SET {sutun} = @r WHERE steamid = @s", c);
                k.Parameters.AddWithValue("@r", renk);
                k.Parameters.AddWithValue("@s", sid);
                await k.ExecuteNonQueryAsync();
            }
            catch (Exception e) { Console.WriteLine("[YccTemel] VIP rengi yazılamadı: " + e.Message); }
        });
    }
}
