using CounterStrikeSharp.API.Core;

namespace YccTemel;

// Ağır çekim araştırması (2026-11-15): sunucu 64 FPS'te normal akarken oyuncular ağır çekim görüyor ve alt-tab düzeltiyor.
// Oyunculara "zaman ölçeği" mesajı gidiyorsa istemci yavaş oynatır. Genel: UM_CurrentTimescale=104, UM_DesiredTimescale=105;
// CS2'ye özel: CS_UM_DesiredTimescale=331, CS_UM_CurrentTimescale=332 (numaralar libserver.so'dan, 2026-11-15).
// Bu bölüm hiçbir şeyi değiştirmez; böyle bir mesaj giderse dakikada en fazla bir kez kaydeder.
public partial class YccTemel
{
    private DateTime _sonTeshis = DateTime.MinValue;

    private void TeshisKur()
    {
        var dosya = Path.Combine(ModuleDirectory, "teshis.log");
        foreach (var id in new[] { 104, 105, 331, 332 })
            HookUserMessage(id, um =>
            {
                if (DateTime.Now - _sonTeshis < TimeSpan.FromMinutes(1)) return HookResult.Continue;
                _sonTeshis = DateTime.Now;
                string ayrinti;
                try { ayrinti = um.DebugString.Replace('\n', ' '); } catch { ayrinti = "?"; }
                var satir = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} zaman ölçeği mesajı id={id} alıcı={um.Recipients.Count} {ayrinti}";
                Console.WriteLine("[YccTemel] " + satir);
                try { File.AppendAllText(dosya, satir + Environment.NewLine); } catch { }
                return HookResult.Continue;
            });
    }
}
