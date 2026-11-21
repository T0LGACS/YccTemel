using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace YccTemel;

// Kanca (CS:GO'daki hook, sunucu sahibi 2026-11-21: "sen yapsana, animasyonda yapabilir miyiz"). Admin ve VIP'ler !kanca ile açar,
// açıkken R basılı tutulunca baktığı yere ip fırlar (ip uçarak gider: animasyon), takılınca oyuncu o noktaya çekilir, bırakınca kopar.
// E tuşu paraşütte kullanıldığı için R seçildi.
public partial class YccTemel
{
    private const string KancaYetki = "@css/generic";
    private const float KancaHizi = 900f, IpUcmaSuresi = 0.15f, KancaMenzil = 3000f;

    private sealed class KancaDurum
    {
        public required Vector Hedef;
        public required CBeam Ip;
        public int Kare;                 // ip fırlatılalı kaç kare oldu
    }

    private readonly HashSet<ulong> _kancaAcik = new();
    private readonly Dictionary<int, KancaDurum> _kanca = new();

    private bool KancaHakki(CCSPlayerController p) => Yetkili(p, KancaYetki) || _vip.ContainsKey(p.SteamID);

    private void KancaKur()
    {
        AddCommand("css_kanca", "Kancayı açar/kapatır (admin ve VIP). Açıkken R basılı tut.", (p, _) =>
        {
            if (!Gercek(p)) return;
            if (!KancaHakki(p!)) { Yaz(p!, "{GOLD}[Kanca]{DEFAULT} Kanca sadece admin ve VIP'lere açık."); return; }
            if (_kancaAcik.Remove(p!.SteamID)) { KancaBirak(p.Slot); Yaz(p, "{GOLD}[Kanca]{DEFAULT} Kanca {RED}kapandı{DEFAULT}."); }
            else { _kancaAcik.Add(p.SteamID); Yaz(p, "{GOLD}[Kanca]{DEFAULT} Kanca {GREEN}açık{DEFAULT}: bir yere bak, {GREEN}R{DEFAULT} tuşunu basılı tut."); }
        });

        RegisterListener<Listeners.OnTick>(() =>
        {
            if (_kancaAcik.Count == 0 && _kanca.Count == 0) return;
            foreach (var p in Utilities.GetPlayers())
            {
                if (!Gercek(p) || !_kancaAcik.Contains(p.SteamID)) { if (p != null && _kanca.ContainsKey(p.Slot)) KancaBirak(p.Slot); continue; }
                var pawn = p.PlayerPawn.Value;
                var basili = p.PawnIsAlive && pawn is { IsValid: true } && (p.Buttons & PlayerButtons.Reload) != 0;
                if (!basili) { KancaBirak(p.Slot); continue; }
                if (!_kanca.TryGetValue(p.Slot, out var d)) { KancaAt(p, pawn!); continue; }
                KancaCek(pawn!, d);
            }
        });

        RegisterEventHandler<EventPlayerDeath>((e, _) => { if (e.Userid != null) KancaBirak(e.Userid.Slot); return HookResult.Continue; });
        RegisterEventHandler<EventPlayerDisconnect>((e, _) =>
        {
            if (e.Userid != null) { KancaBirak(e.Userid.Slot); _kancaAcik.Remove(e.Userid.SteamID); }
            return HookResult.Continue;
        });
        // Raunt yenilenirken oyun varlıkları kendisi temizliyor; ipler ondan önce silinir
        RegisterEventHandler<EventRoundPrestart>((_, _) => { foreach (var s in _kanca.Keys.ToList()) KancaBirak(s); return HookResult.Continue; });
        RegisterListener<Listeners.OnMapEnd>(() => _kanca.Clear());
    }

    private static Vector Goz(CCSPlayerPawn pawn) =>
        new(pawn.AbsOrigin!.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + pawn.ViewOffset.Z);

    private void KancaAt(CCSPlayerController p, CCSPlayerPawn pawn)
    {
        if (pawn.AbsOrigin == null || pawn.EyeAngles == null) return;
        var goz = Goz(pawn);
        var iz = Trace.TraceShape(goz, pawn.EyeAngles, pawn, new TraceOptions { InteractsWith = Masks.Solid });
        if (!iz.DidHit() || Uzaklik(goz, iz.EndPos) > KancaMenzil) return;

        var ip = Utilities.CreateEntityByName<CBeam>("beam");
        if (ip == null || !ip.IsValid) return;
        ip.Render = Color.FromArgb(255, 255, 140, 0);   // YCC turuncusu
        ip.Width = 1.5f;
        ip.EndWidth = 1.5f;
        ip.Teleport(goz, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        ip.EndPos.X = goz.X; ip.EndPos.Y = goz.Y; ip.EndPos.Z = goz.Z;   // ip ağızdan başlar, KancaCek'te hedefe uçar
        ip.DispatchSpawn();
        _kanca[p.Slot] = new KancaDurum { Hedef = new Vector(iz.EndPos.X, iz.EndPos.Y, iz.EndPos.Z), Ip = ip };
    }

    private void KancaCek(CCSPlayerPawn pawn, KancaDurum d)
    {
        if (!d.Ip.IsValid || pawn.AbsOrigin == null) return;
        var goz = Goz(pawn);
        d.Ip.Teleport(new Vector(goz.X, goz.Y, goz.Z - 10), null, null);   // ip elden çıkıyor gibi dursun

        // Animasyon: ip ilk IpUcmaSuresi boyunca gözden hedefe doğru uzar, ancak takılınca çekmeye başlar
        var ucmaKare = Math.Max(1, (int)(IpUcmaSuresi * 64));
        d.Kare++;
        var t = Math.Min(1f, d.Kare / (float)ucmaKare);
        d.Ip.EndPos.X = goz.X + (d.Hedef.X - goz.X) * t;
        d.Ip.EndPos.Y = goz.Y + (d.Hedef.Y - goz.Y) * t;
        d.Ip.EndPos.Z = goz.Z + (d.Hedef.Z - goz.Z) * t;
        Utilities.SetStateChanged(d.Ip, "CBeam", "m_vecEndPos");
        if (t < 1f) return;

        var fark = new Vector(d.Hedef.X - pawn.AbsOrigin.X, d.Hedef.Y - pawn.AbsOrigin.Y, d.Hedef.Z - pawn.AbsOrigin.Z);
        var mesafe = Uzaklik(pawn.AbsOrigin, d.Hedef);
        // Hedefe varınca orada asılı kalır (yerçekimi sıfırlanır), R bırakılınca düşer
        var hiz = mesafe < 40 ? new Vector(0, 0, 0)
            : new Vector(fark.X / mesafe * KancaHizi, fark.Y / mesafe * KancaHizi, fark.Z / mesafe * KancaHizi);
        pawn.Teleport(null, null, hiz);
    }

    private void KancaBirak(int slot)
    {
        if (!_kanca.Remove(slot, out var d)) return;
        if (d.Ip.IsValid) d.Ip.Remove();
    }

    private static float Uzaklik(Vector a, Vector b) =>
        MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z));
}
