using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace YccTemel;

public partial class YccTemel
{
    private void ReklamKur()
    {
        var r = Config.Reklam;
        if (r.Hosgeldin != "")
            RegisterEventHandler<EventPlayerConnectFull>((e, _) =>
            {
                var p = e.Userid;
                if (Gercek(p)) AddTimer(r.HosgeldinGecikme, () => { if (Gercek(p)) Yaz(p!, r.Hosgeldin.Replace("{ISIM}", p!.PlayerName)); });
                return HookResult.Continue;
            });

        if (r.GirisAfisi != "") AfisKur(r);

        foreach (var g in r.Gruplar.Where(g => g.Mesajlar.Count > 0 && g.Aralik >= 5))
        {
            var sira = 0;
            AddTimer(g.Aralik, () =>
            {
                if (!Utilities.GetPlayers().Any(Gercek)) return;   // boş sunucuya yazma
                HerkeseYaz(g.Mesajlar[sira++ % g.Mesajlar.Count]);
            }, TimerFlags.REPEAT);
        }
    }

    private string? _denemeHtml;
    private readonly HashSet<int> _afisBekleyen = new();          // bağlandı, henüz doğmadı
    private readonly Dictionary<int, float> _afisBitis = new();   // slot → afişin kalkacağı an

    private void AfisKur(ReklamAyar r)
    {
        var html = $"<img src='{System.Net.WebUtility.HtmlEncode(r.GirisAfisi)}' style='width:{r.GirisAfisiGenislik}px;height:{r.GirisAfisiYukseklik}px;'/>" + (r.GirisAfisiYazi == "" ? "" : "<br>" + r.GirisAfisiYazi);   // yazı sunucu ayarından gelir, HTML olarak kalır
        RegisterEventHandler<EventPlayerConnectFull>((e, _) => { if (Gercek(e.Userid)) _afisBekleyen.Add(e.Userid!.Slot); return HookResult.Continue; });
        RegisterEventHandler<EventPlayerSpawn>((e, _) =>
        {
            var p = e.Userid;
            if (Gercek(p) && p!.TeamNum > 1 && _afisBekleyen.Remove(p.Slot)) _afisBitis[p.Slot] = Server.CurrentTime + r.GirisAfisiSure;
            return HookResult.Continue;
        });
        RegisterListener<Listeners.OnClientDisconnect>(slot => { _afisBekleyen.Remove(slot); _afisBitis.Remove(slot); });
        if (r.AfisAralik >= 30)
            AddTimer(r.AfisAralik, () =>
            {
                foreach (var o in Utilities.GetPlayers().Where(Gercek).Where(o => o.TeamNum > 1)) _afisBitis[o.Slot] = Server.CurrentTime + r.GirisAfisiSure;
            }, TimerFlags.REPEAT);
        // Sadece sunucu konsolundan/rcon: verilen HTML'i herkese Sure saniye gösterir (afiş boyutunu yeniden başlatmadan denemek için)
        AddCommand("css_afisdene", "Konsol: ortada HTML dener", (p, info) =>
        {
            if (p != null) return;
            // Konsol ayırıcıları (: ' gibi) HTML'i bozduğu için base64 de kabul edilir: css_afisdene b64:<base64>
            var ham = info.ArgString.Trim().Trim('"');
            _denemeHtml = ham.StartsWith("b64:") ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(ham[4..])) : ham;
            info.ReplyToCommand("afisdene: " + _denemeHtml);
            foreach (var o in Utilities.GetPlayers().Where(Gercek)) _afisBitis[o.Slot] = Server.CurrentTime + r.GirisAfisiSure;
        });
        // Ortadaki yazı her karede gönderilmezse anlık görünüp kayboluyor (OnlyHsVote'taki gibi)
        RegisterListener<Listeners.OnTick>(() =>
        {
            if (_afisBitis.Count == 0) { _denemeHtml = null; return; }
            foreach (var (slot, bitis) in _afisBitis.ToList())
            {
                var p = Utilities.GetPlayerFromSlot(slot);
                if (!Gercek(p) || Server.CurrentTime > bitis) { _afisBitis.Remove(slot); continue; }
                // Menüsü açık oyuncuya gösterilmez, ikisi de ortada üst üste biniyor. YccSkin ve YccAdmin menüleri açıkken karakteri
                // MOVETYPE_OBSOLETE ile donduruyor; ayrı eklentiler arasında bağ kurmadan menü açık mı buradan anlaşılır.
                if (p!.PlayerPawn.Value is { IsValid: true } pw && pw.MoveType == MoveType_t.MOVETYPE_OBSOLETE) continue;
                p.PrintToCenterHtml(_denemeHtml ?? html);
            }
        });
    }
}
