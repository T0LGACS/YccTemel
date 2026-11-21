using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace YccTemel;

// !rapor <oyuncu> <sebep> (sunucu sahibi 2026-11-21): içerideki adminlere sohbet + ekran uyarısı, sohbet webhook'u açıksa Discord'a da düşer.
// Kötüye kullanılmasın diye oyuncu başına RaporAralik saniyede bir.
public partial class YccTemel
{
    private const string RaporYetki = "@css/generic";
    private const int RaporAralik = 120;
    private readonly Dictionary<ulong, DateTime> _sonRapor = new();

    private void RaporKur()
    {
        AddCommand("css_rapor", "Oyuncuyu adminlere bildirir: !rapor <isim> <sebep>", (p, info) =>
        {
            if (!Gercek(p)) return;
            var r = p!;
            if (info.ArgCount < 3) { Yaz(r, "{GOLD}[Rapor]{DEFAULT} Kullanım: {GREEN}!rapor <oyuncu adı> <sebep>"); return; }

            if (_sonRapor.TryGetValue(r.SteamID, out var son) && (DateTime.Now - son).TotalSeconds < RaporAralik)
            {
                Yaz(r, $"{{GOLD}}[Rapor]{{DEFAULT}} Tekrar rapor için {{RED}}{RaporAralik - (int)(DateTime.Now - son).TotalSeconds} sn{{DEFAULT}} bekle.");
                return;
            }

            var ara = info.GetArg(1);
            var adaylar = Utilities.GetPlayers().Where(o => o.IsValid && !o.IsHLTV && o != r
                && o.PlayerName.Contains(ara, StringComparison.OrdinalIgnoreCase)).ToList();
            if (adaylar.Count != 1)
            {
                Yaz(r, adaylar.Count == 0 ? $"{{GOLD}}[Rapor]{{DEFAULT}} \"{ara}\" adında oyuncu yok."
                    : $"{{GOLD}}[Rapor]{{DEFAULT}} \"{ara}\" birden fazla oyuncuya uyuyor, adı biraz daha yaz.");
                return;
            }
            var h = adaylar[0];
            var sebep = string.Join(' ', Enumerable.Range(2, info.ArgCount - 2).Select(info.GetArg)).Trim();
            if (sebep.Length > 150) sebep = sebep[..150];
            _sonRapor[r.SteamID] = DateTime.Now;

            var adminler = Utilities.GetPlayers().Where(o => Gercek(o) && Yetkili(o, RaporYetki)).ToList();
            foreach (var a in adminler)
            {
                Yaz(a, $"{{RED}}[RAPOR]{{DEFAULT}} {{GREEN}}{r.PlayerName}{{DEFAULT}} → {{LIGHTRED}}{h.PlayerName}{{DEFAULT}}: {sebep}");
                a.PrintToCenter($"RAPOR: {h.PlayerName} — {sebep}");
            }
            var port = ConVar.Find("hostport")?.GetPrimitiveValue<int>() ?? 0;
            DiscordGonder(r.PlayerName, r.SteamID, $"🚩 RAPOR (port {port}) → {h.PlayerName} ({(h.IsBot ? "bot" : h.SteamID.ToString())}): {sebep}");
            Yaz(r, adminler.Count > 0 ? $"{{GOLD}}[Rapor]{{DEFAULT}} Raporun içerideki {adminler.Count} admine iletildi, teşekkürler."
                : "{GOLD}[Rapor]{DEFAULT} Şu an içeride admin yok, raporun Discord'a iletildi.");
        });
    }
}
