using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using CounterStrikeSharp.API.Core;

namespace YccTemel;

// Sohbeti Discord webhook'una gönderir. Oyun karesini bekletmemek için mesajlar kuyruğa girer, arka planda sırayla gider;
// Discord "çok hızlı" (429) derse istenen süre beklenip tekrar denenir. Avatar Steam API'den alınır ve hatırlanır.
public partial class YccTemel
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private Channel<(string isim, ulong sid, string metin)>? _kuyruk;
    private CancellationTokenSource? _dur;
    private readonly ConcurrentDictionary<ulong, string?> _avatar = new();
    private Channel<(string adres, string govde)>? _vortexa;

    private void DiscordKur()
    {
        VortexaKur();
        if (!Config.Discord.Webhook.StartsWith("https://")) return;
        // Kuyruk dolarsa (Discord uzun süre cevap vermezse) en eski mesaj atılır, bellek şişmez
        _kuyruk = Channel.CreateBounded<(string, ulong, string)>(new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest });
        _dur ??= new CancellationTokenSource();
        var webhook = Config.Discord.Webhook;
        var renk = Metin.HexRenk(Config.Discord.Renk);
        var token = _dur.Token;
        _ = Task.Run(async () =>
        {
            await foreach (var (isim, sid, metin) in _kuyruk.Reader.ReadAllAsync(token))
            {
                try { await Gonder(webhook, Metin.DiscordGovde(isim, sid, await Avatar(sid), metin, renk), token); }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                catch (Exception e) { Console.WriteLine("[YccTemel] Discord'a gönderilemedi: " + e.Message); }
            }
        }, token);
    }

    private void DiscordGonder(string isim, ulong sid, string metin)
    {
        _kuyruk?.Writer.TryWrite((isim, sid, metin));
        if (Config.Discord.VortexaSohbet.StartsWith("https://")) _vortexa?.Writer.TryWrite((Config.Discord.VortexaSohbet, Metin.VortexaGovde(isim, $"{metin}\n{Metin.SteamProfil(sid)}")));
    }

    // Vortexa'ya düz mesaj (Discord biçimi: username + content). Vortexa [ad](link) tanımaz, sadece düz adresi link yapar:
    // Steam profili mesajın altına düz adres olarak eklenir (kullanıcı 2026-11-23). Kendi kuyruğu: Discord yavaşlarsa Vortexa beklemesin.
    private void VortexaKur()
    {
        if (!Config.Discord.VortexaSohbet.StartsWith("https://") && !Config.Discord.VortexaGiris.StartsWith("https://")) return;
        _vortexa = Channel.CreateBounded<(string, string)>(new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest });
        _dur ??= new CancellationTokenSource();
        var token = _dur.Token;
        _ = Task.Run(async () =>
        {
            await foreach (var (adres, govde) in _vortexa.Reader.ReadAllAsync(token))
            {
                try { await Gonder(adres, govde, token); }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                catch (Exception e) { Console.WriteLine("[YccTemel] Vortexa'ya gönderilemedi: " + e.Message); }
            }
        }, token);
        if (!Config.Discord.VortexaGiris.StartsWith("https://")) return;
        var adres = Config.Discord.VortexaGiris;
        var etiket = Config.Discord.Etiket == "" ? "" : Config.Discord.Etiket + " ";
        RegisterEventHandler<EventPlayerConnectFull>((e, _) =>
        {
            if (Gercek(e.Userid)) _vortexa.Writer.TryWrite((adres, Metin.VortexaGovde("CS2", $"{etiket}🟢 **{e.Userid!.PlayerName}** sunucuya girdi\n{Metin.SteamProfil(e.Userid.SteamID)}")));
            return HookResult.Continue;
        });
        RegisterEventHandler<EventPlayerDisconnect>((e, _) =>
        {
            if (Gercek(e.Userid)) _vortexa.Writer.TryWrite((adres, Metin.VortexaGovde("CS2", $"{etiket}🔴 **{e.Userid!.PlayerName}** sunucudan çıktı\n{Metin.SteamProfil(e.Userid.SteamID)}")));
            return HookResult.Continue;
        });
    }

    private void DiscordKapat()
    {
        _kuyruk?.Writer.TryComplete();
        _vortexa?.Writer.TryComplete();
        _dur?.Cancel();
    }

    private static async Task Gonder(string webhook, string govde, CancellationToken token)
    {
        for (var deneme = 0; deneme < 3; deneme++)
        {
            using var r = await Http.PostAsync(webhook, new StringContent(govde, Encoding.UTF8, "application/json"), token);
            if ((int)r.StatusCode != 429) { if (!r.IsSuccessStatusCode) Console.WriteLine($"[YccTemel] Discord cevabı: {(int)r.StatusCode}"); return; }
            var bekle = 1.0;
            try { using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync(token)); bekle = d.RootElement.GetProperty("retry_after").GetDouble(); } catch { }
            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(bekle, 0.2, 30)), token);
        }
    }

    private async Task<string?> Avatar(ulong sid)
    {
        if (_avatar.TryGetValue(sid, out var a)) return a;
        string? url = null;
        var anahtar = Config.SteamGrup.ApiAnahtari;
        if (anahtar != "")
        {
            try
            {
                using var d = JsonDocument.Parse(await Http.GetStringAsync(
                    $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={anahtar}&steamids={sid}"));
                var oyuncular = d.RootElement.GetProperty("response").GetProperty("players");
                if (oyuncular.GetArrayLength() > 0) url = oyuncular[0].GetProperty("avatarmedium").GetString();
            }
            catch { }   // avatar olmadan da gönderilir
        }
        if (_avatar.Count > 2000) _avatar.Clear();
        _avatar[sid] = url;
        return url;
    }
}
