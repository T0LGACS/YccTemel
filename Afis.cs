using System.Globalization;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace YccTemel;

// Haritalara site afişi panoları (sunucu sahibi 2026-11-19: "redline ve dust2'nin bir yerlerine de koy"). Modeller ycc_hud
// Workshop eklentisinde (MultiAddonManager herkese indirtir, üretici github/YccRank/hud/afis_model.py). Yerler oyun içinden
// !afis komutuyla seçilir ve her sunucunun kendi configs/plugins/YccTemel/afisler.json'una yazılır (FFA/AWP overlay'de ayrı).
// Panolar her raunt başında yeniden konur: raunt yeniden başlarken oyun eklentinin koyduğu varlıkları siliyor.
public partial class YccTemel
{
    public class AfisYer
    {
        public string Tur { get; set; } = "genis";
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yon { get; set; }
        public float Olcek { get; set; } = 1;
    }

    private Dictionary<string, List<AfisYer>> _afisler = new();
    private readonly List<CDynamicProp> _afisVarlik = new();
    private readonly Dictionary<int, (AfisYer yer, CDynamicProp prop)> _duzenlenen = new();   // admin slotu → elindeki pano

    private string AfisDosyasi => Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "configs", "plugins", "YccTemel", "afisler.json");
    private string AfisModel(string tur) => tur == "kare" ? Config.Afis.KareModel : Config.Afis.GenisModel;

    private void AfisKur()
    {
        var a = Config.Afis;
        if (!a.Acik) return;
        try { if (File.Exists(AfisDosyasi)) _afisler = JsonSerializer.Deserialize<Dictionary<string, List<AfisYer>>>(File.ReadAllText(AfisDosyasi)) ?? new(); }
        catch (Exception e) { Console.WriteLine("[YccTemel] afisler.json okunamadı: " + e.Message); }

        RegisterListener<Listeners.OnServerPrecacheResources>(m => { m.AddResource(a.GenisModel); m.AddResource(a.KareModel); });
        RegisterListener<Listeners.OnMapEnd>(() => { _afisVarlik.Clear(); _duzenlenen.Clear(); });
        // Raunt başında ve harita açılışında (deathmatch'te raunt yok) panolar konur
        RegisterEventHandler<EventRoundStart>((_, _) => { Server.NextFrame(AfisleriKoy); return HookResult.Continue; });
        RegisterListener<Listeners.OnMapStart>(_ => AddTimer(3, AfisleriKoy, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE));

        AddCommand("css_afis", "Haritaya site afişi panosu koyar/düzenler (!afis yardım)", (p, info) =>
        {
            if (!Gercek(p) || !Yetkili(p!, a.Yetki)) return;
            AfisKomutu(p!, info.ArgCount > 1 ? info.GetArg(1).ToLowerInvariant() : "", info.ArgCount > 2 ? info.GetArg(2) : "");
        });
    }

    private void AfisleriKoy()
    {
        foreach (var v in _afisVarlik) if (v.IsValid) v.Remove();
        _afisVarlik.Clear(); _duzenlenen.Clear();
        if (!_afisler.TryGetValue(Server.MapName, out var liste)) return;
        foreach (var y in liste) { var p = AfisOlustur(y); if (p != null) _afisVarlik.Add(p); }
    }

    private CDynamicProp? AfisOlustur(AfisYer y)
    {
        var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (prop == null || !prop.IsValid) return null;
        prop.Collision.SolidType = SolidType_t.SOLID_NONE;
        prop.SetModel(AfisModel(y.Tur));   // DispatchSpawn'dan önce (Parasut.cs'teki gibi)
        prop.DispatchSpawn();
        AfisTasi(prop, y);
        return prop;
    }

    private static void AfisTasi(CDynamicProp prop, AfisYer y)
    {
        prop.Teleport(new Vector(y.X, y.Y, y.Z), new QAngle(0, y.Yon, 0));
        var sahne = prop.CBodyComponent?.SceneNode;
        if (sahne == null) return;
        sahne.GetSkeletonInstance().Scale = y.Olcek;
        Utilities.SetStateChanged(prop, "CBaseEntity", "m_CBodyComponent");
    }

    private void AfisKaydet()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(AfisDosyasi)!);
        File.WriteAllText(AfisDosyasi, JsonSerializer.Serialize(_afisler, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void AfisKomutu(CCSPlayerController p, string islem, string deger)
    {
        var pawn = p.PlayerPawn.Value;
        if (pawn?.AbsOrigin == null) { Yaz(p, "{RED}Önce doğman lazım."); return; }
        float.TryParse(deger.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var s);
        // Sayı yazılmazsa tek adım (sunucu sahibi 2026-11-19 "!afis ileri" yazıp kımıldamadığını gördü)
        if (deger == "") s = islem switch { "don" => 15, "boyut" => 0, "genis" or "kare" => 0, _ => 10 };
        // Ters yönler (sunucu sahibi "!afis sol 100" yazdı): sol = sag eksi, geri = ileri eksi, asagi = yukari eksi
        (islem, s) = islem switch { "sol" => ("sag", -s), "geri" => ("ileri", -s), "asagi" => ("yukari", -s), _ => (islem, s) };
        var harita = Server.MapName;
        _duzenlenen.TryGetValue(p.Slot, out var d);
        bool elde = d.prop != null && d.prop.IsValid;

        switch (islem)
        {
            case "genis":
            case "kare":
            {
                // Bakılan yönde, göz hizasında, yüzü oyuncuya dönük
                var yaw = pawn.EyeAngles?.Y ?? 0; var r = yaw * MathF.PI / 180;
                var mesafe = s > 0 ? s : 100;
                var y = new AfisYer { Tur = islem, X = pawn.AbsOrigin.X + MathF.Cos(r) * mesafe, Y = pawn.AbsOrigin.Y + MathF.Sin(r) * mesafe,
                                      Z = pawn.AbsOrigin.Z + 64, Yon = (yaw + 180) % 360 };
                var prop = AfisOlustur(y);
                if (prop == null) { Yaz(p, "{RED}Pano oluşturulamadı (model yüklü mü?)."); return; }
                _afisVarlik.Add(prop);
                _duzenlenen[p.Slot] = (y, prop);
                Yaz(p, "{GREEN}Pano kondu (kaydedilmedi).{DEFAULT} Ayarla: !afis sag/sol/ileri/geri/yukari/asagi 20 (sayısız 10 birim), !afis don 90, !afis boyut <1.5>, sonra {LIME}!afis kaydet");
                return;
            }
            case "ileri": case "sag": case "yukari":
            {
                if (!elde) { Yaz(p, "{RED}Elinde pano yok: !afis genis ya da !afis kare"); return; }
                var r = (d.yer.Yon + 180) * MathF.PI / 180;   // panoya bakan yön = oyuncunun ileri yönü
                if (islem == "ileri") { d.yer.X += MathF.Cos(r) * s; d.yer.Y += MathF.Sin(r) * s; }
                else if (islem == "sag") { d.yer.X += MathF.Sin(r) * s; d.yer.Y -= MathF.Cos(r) * s; }
                else d.yer.Z += s;
                break;
            }
            case "don": if (!elde) goto default; d.yer.Yon = ((d.yer.Yon + s) % 360 + 360) % 360; break;
            case "boyut": if (!elde || s <= 0 || s > 10) goto default; d.yer.Olcek = s; break;
            case "kaydet":
            {
                if (!elde) { Yaz(p, "{RED}Kaydedilecek pano yok."); return; }
                if (!_afisler.TryGetValue(harita, out var liste)) _afisler[harita] = liste = new();
                if (!liste.Contains(d.yer)) liste.Add(d.yer);
                AfisKaydet(); _duzenlenen.Remove(p.Slot);
                Yaz(p, $"{{GREEN}}Kaydedildi.{{DEFAULT}} {harita} haritasında {liste.Count} pano var.");
                return;
            }
            case "duzenle":
            {
                // En yakın kayıtlı panoyu eline alır
                var liste = _afisler.GetValueOrDefault(harita) ?? new();
                var yakin = liste.OrderBy(y => Uzaklik(y, pawn.AbsOrigin)).FirstOrDefault();
                var prop = _afisVarlik.FirstOrDefault(v => v.IsValid && yakin != null && v.AbsOrigin != null &&
                    MathF.Abs(v.AbsOrigin.X - yakin.X) < 0.5f && MathF.Abs(v.AbsOrigin.Y - yakin.Y) < 0.5f);
                if (yakin == null || prop == null) { Yaz(p, "{RED}Yakında kayıtlı pano yok."); return; }
                _duzenlenen[p.Slot] = (yakin, prop);
                Yaz(p, "{GREEN}En yakın pano elinde.{DEFAULT} Ayarlayıp !afis kaydet");
                return;
            }
            case "sil":
            {
                var liste = _afisler.GetValueOrDefault(harita) ?? new();
                var hedef = elde ? d.yer : liste.OrderBy(y => Uzaklik(y, pawn.AbsOrigin)).FirstOrDefault();
                if (hedef == null) { Yaz(p, "{RED}Silinecek pano yok."); return; }
                liste.Remove(hedef); AfisKaydet(); _duzenlenen.Remove(p.Slot);
                AfisleriKoy();
                Yaz(p, $"{{GREEN}}Pano silindi.{{DEFAULT}} {harita} haritasında {liste.Count} pano kaldı.");
                return;
            }
            case "liste":
                Yaz(p, $"{harita}: {(_afisler.GetValueOrDefault(harita)?.Count ?? 0)} kayıtlı pano.");
                return;
            default:
                Yaz(p, "{LIME}!afis genis{DEFAULT} / {LIME}!afis kare{DEFAULT} [mesafe] koyar · sag/sol/ileri/geri/yukari/asagi <birim> · don <derece> · boyut <çarpan> · {LIME}kaydet{DEFAULT} · duzenle (en yakını) · sil · liste");
                return;
        }
        AfisTasi(d.prop!, d.yer);
        Yaz(p, $"Pano: konum {d.yer.X:0},{d.yer.Y:0},{d.yer.Z:0} · yön {d.yer.Yon:0} · boyut {d.yer.Olcek:0.##}");
    }

    private static float Uzaklik(AfisYer y, Vector v) => MathF.Sqrt((y.X - v.X) * (y.X - v.X) + (y.Y - v.Y) * (y.Y - v.Y) + (y.Z - v.Z) * (y.Z - v.Z));
}
