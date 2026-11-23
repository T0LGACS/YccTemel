using System.Globalization;
using System.Reflection;
using System.Text.Json;
using CounterStrikeSharp.API.Modules.Utils;

namespace YccTemel;

// Oyun motoruna dokunmayan saf yardımcılar; test/ altındaki deneme bunları doğrudan çağırır.
public static class Metin
{
    // {GREEN}, {lime}, {GRAY} → CS2 sohbet renk karakteri. ChatColors'taki her alan adı geçerli.
    private static readonly Dictionary<string, string> Renkler = BuildRenkler();

    private static Dictionary<string, string> BuildRenkler()
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in typeof(ChatColors).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (f.GetValue(null) is char c) d["{" + f.Name + "}"] = c.ToString();
        if (d.TryGetValue("{Grey}", out var g)) d["{GRAY}"] = g;
        return d;
    }

    public static string Renk(string s)
    {
        if (string.IsNullOrEmpty(s) || !s.Contains('{')) return s;
        foreach (var (k, v) in Renkler) s = s.Replace(k, v, StringComparison.OrdinalIgnoreCase);
        return s;
    }

    // Biçim panelin ve agir-cekim-izle.sh'nin beklediğiyle birebir aynı:
    // [HH:mm:ss] [STEAMID64] IP [ALL]|[TEAM] (İsim) : mesaj
    public static string KayitSatiri(DateTime t, ulong steamId, string ip, bool takim, string isim, string mesaj) =>
        $"[{t:HH:mm:ss}] [{steamId}] {(ip == "" ? "-" : ip)} {(takim ? "[TEAM]" : "[ALL]")} ({isim}) : {mesaj}";

    public static string KayitDosyasi(DateTime t) => t.ToString("HH00_MM_dd_yyyy", CultureInfo.InvariantCulture) + ".txt";

    public static bool Kaydedilmez(string mesaj, string disi) => mesaj.Length == 0 || disi.Contains(mesaj[0]);

    // "1.2.3.4:27005" → "1.2.3.4"
    public static string IpYalin(string? ip) => string.IsNullOrEmpty(ip) ? "" : ip.Split(':')[0];

    // exec_after_delay 5 "say merhaba" → ArgString = 5 "say merhaba", ilk argüman atılır, tırnaklar soyulur
    public static string IlkArgumanSonrasi(string argString, string ilk)
    {
        var s = argString.Trim();
        if (s.StartsWith('"' + ilk + '"')) s = s[(ilk.Length + 2)..];
        else if (s.StartsWith(ilk)) s = s[ilk.Length..];
        return Tirnaksiz(s.Trim());
    }

    public static string Tirnaksiz(string s)
    {
        s = s.Trim();
        return s.Length >= 2 && s[0] == '"' && s[^1] == '"' ? s[1..^1] : s;
    }

    // aim_redline → aim_ ; alt çizgisiz adlarda önek yok
    public static string? HaritaOnek(string harita)
    {
        var i = harita.IndexOf('_');
        return i > 0 ? harita[..(i + 1)] : null;
    }

    // Sohbet mesajının ilk kelimesi (komut kontrolü için): "  !ws  ak47" → "!ws"
    public static string IlkKelime(string mesaj) => mesaj.Trim().Trim('"').Split(' ', 2)[0];

    public static int HexRenk(string hex) =>
        int.TryParse(hex.TrimStart('#'), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ? r : 0xFF7A00;

    public static string DiscordGovde(string isim, ulong steamId, string? avatar, string metin, int renk) =>
        JsonSerializer.Serialize(new
        {
            embeds = new[] { new {
                author = new { name = isim, url = SteamProfil(steamId), icon_url = avatar },
                description = metin,
                color = renk,
            } },
            allowed_mentions = new { parse = Array.Empty<string>() },   // sohbetteki @everyone kimseyi etiketlemesin
        }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

    public static string SteamProfil(ulong steamId) => $"https://steamcommunity.com/profiles/{steamId}";

    // Vortexa webhook gövdesi: yazan adı + düz metin (Discord biçiminin sade hali).
    public static string VortexaGovde(string isim, string metin) =>
        JsonSerializer.Serialize(new { username = isim, content = metin });

    // "{SAAT}" vb. yer tutucuları doldurur
    public static string Doldur(string bicim, params (string ad, string deger)[] d)
    {
        foreach (var (ad, deger) in d) bicim = bicim.Replace("{" + ad + "}", deger);
        return bicim;
    }
}
