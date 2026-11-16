using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace YccTemel;

// Havadayken E (kullan) basılı tutulursa düşüş hızı DusmeHizi'nde sınırlanır ve oyuncunun üstünde paraşüt görünür.
// Paraşüt her karede oyuncunun konumuna taşınır; yere inince, E bırakılınca ya da ölünce silinir.
// Oyuncuya bağlamak (SetParent) daha akıcıydı ama oyuncu kendi paraşütünü göremiyordu: CS2 birinci şahısta kendi
// karakterini ve ona bağlı her şeyi çizmiyor (sunucu sahibi 2026-11-15: "kendi üstümde göremiyorum").
public partial class YccTemel
{
    private const uint YerdeBayragi = 1;   // FL_ONGROUND
    private readonly Dictionary<int, CDynamicProp> _parasut = new();
    private DateTime _sonParasutKaydi = DateTime.MinValue;
    private readonly Dictionary<int, int> _kapanis = new();   // slot → kaç karedir kapanma koşulunda
    private const int KapanmaKaresi = 40;                     // ~0,6 sn (64 tick)

    private void ParasutKur()
    {
        var a = Config.Parasut;
        if (!a.Acik) return;
        if (a.Model != "")
            RegisterListener<Listeners.OnServerPrecacheResources>(m => m.AddResource(a.Model));
        RegisterListener<Listeners.OnMapEnd>(() => _parasut.Clear());   // harita değişince varlıklar zaten silinir

        RegisterListener<Listeners.OnTick>(() =>
        {
            foreach (var p in Utilities.GetPlayers())
            {
                if (!Gercek(p)) continue;
                var pawn = p.PlayerPawn.Value;
                var acik = p.PawnIsAlive && pawn != null && pawn.IsValid && (p.Buttons & PlayerButtons.Use) != 0
                           && (pawn.Flags & YerdeBayragi) == 0 && Yetkili(p, a.Yetki);
                // Paraşüt koşul bozulur bozulmaz silinince kısa zıplamalarda bir an görünüp kayboluyordu (sunucu sahibinin
                // videosu 2026-11-15: ~0,25 sn). Yere inince / E bırakılınca KapanmaKaresi kadar daha açık kalır.
                if (!acik)
                {
                    if (!_parasut.ContainsKey(p.Slot)) continue;
                    var n = _kapanis.GetValueOrDefault(p.Slot) + 1;
                    if (n > KapanmaKaresi || pawn == null || !pawn.IsValid || !p.PawnIsAlive) { ParasutKaldir(p.Slot); continue; }
                    _kapanis[p.Slot] = n;
                }
                else
                {
                    _kapanis.Remove(p.Slot);
                    var hiz = pawn!.AbsVelocity;
                    if (hiz.Z < -a.DusmeHizi) hiz.Z = -a.DusmeHizi;
                    if (a.Model == "") continue;
                    if (hiz.Z < 0) ParasutAc(p.Slot, pawn, a.Model);
                }
                if (_parasut.TryGetValue(p.Slot, out var prop) && prop.IsValid && pawn!.AbsOrigin != null)
                    prop.Teleport(pawn.AbsOrigin, new QAngle(0, pawn.EyeAngles?.Y ?? 0, 0));
            }
        });
        RegisterEventHandler<EventPlayerDisconnect>((e, i) => { if (e.Userid != null) ParasutKaldir(e.Userid.Slot); return HookResult.Continue; });

        // Admin tek başına denesin diye (kullanıcı 2026-11-15: "kendim göremiyorum, deneyecek arkadaşım yok"):
        // paraşüt modeli oyuncunun 250 birim önüne, yere 20 sn konur; dışarıdan görünüşü, boyu ve yönü kontrol edilir.
        AddCommand("css_parasutgoster", "Paraşüt modelini önüne koyar (deneme)", (p, info) =>
        {
            if (!Gercek(p) || !Yetkili(p!, "@css/cvar") || a.Model == "") return;
            var pawn = p!.PlayerPawn.Value;
            if (pawn?.AbsOrigin == null) return;
            var yon = (pawn.EyeAngles?.Y ?? 0) * MathF.PI / 180;
            var yer = new Vector(pawn.AbsOrigin.X + MathF.Cos(yon) * 250, pawn.AbsOrigin.Y + MathF.Sin(yon) * 250, pawn.AbsOrigin.Z);
            var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
            if (prop == null || !prop.IsValid) { Yaz(p, "{RED}Paraşüt oluşturulamadı."); return; }
            prop.Collision.SolidType = SolidType_t.SOLID_NONE;
            prop.SetModel(a.Model);
            prop.DispatchSpawn();
            prop.Teleport(yer, new QAngle(0, 0, 0));
            Yaz(p, "{GREEN}Paraşüt önüne kondu, 20 saniye duracak. Yukarı bak.");
            Console.WriteLine($"[YccTemel] parasutgoster: {a.Model} @ {yer.X:0},{yer.Y:0},{yer.Z:0}");
            AddTimer(20, () => { if (prop.IsValid) prop.Remove(); });
        });
    }

    private void ParasutAc(int slot, CCSPlayerPawn pawn, string model)
    {
        if (_parasut.TryGetValue(slot, out var eski) && eski.IsValid) return;
        var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (prop == null || !prop.IsValid) return;
        prop.Collision.SolidType = SolidType_t.SOLID_NONE;
        prop.SetModel(model);   // DispatchSpawn'dan önce: yoksa "prop_dynamic has no model name" (2026-11-15)
        prop.DispatchSpawn();
        prop.Teleport(pawn.AbsOrigin, new QAngle(0, pawn.AbsRotation?.Y ?? 0, 0));
        _parasut[slot] = prop;
        if (DateTime.Now - _sonParasutKaydi > TimeSpan.FromSeconds(30))   // açıldığını görmek için, yarım dakikada en fazla bir satır
        {
            _sonParasutKaydi = DateTime.Now;
            Console.WriteLine($"[YccTemel] paraşüt açıldı: slot {slot} @ {pawn.AbsOrigin?.X:0},{pawn.AbsOrigin?.Y:0},{pawn.AbsOrigin?.Z:0}");
        }
    }

    private void ParasutKaldir(int slot)
    {
        _kapanis.Remove(slot);
        if (!_parasut.Remove(slot, out var prop)) return;
        if (prop.IsValid) prop.Remove();
    }
}
