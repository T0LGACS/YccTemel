using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace YccTemel;

// Sunucunun temel araçları tek eklentide (kullanıcı 2026-11-15: "0'dan hepsini yaz, kendi ürettiğin olsun").
// Eski 13 eklentinin yerine geçer: sohbet kaydı + Discord, sohbet etiketi, admin gözcü/admin sohbeti, reklamlar,
// özel komutlar ve admin kısayolları, !rr, !ip, oyuncu listesi, paraşüt, admin gizlenme, exec_after_*,
// haritaya özel cfg, Steam grubu kısıtı. Her bölüm kendi dosyasında (partial).
public partial class YccTemel : BasePlugin, IPluginConfig<Ayarlar>
{
    public override string ModuleName => "YccTemel";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Claude";
    public override string ModuleDescription => "YCC sunucusunun temel araçları";

    public Ayarlar Config { get; set; } = new();
    public void OnConfigParsed(Ayarlar config) => Config = config;

    public override void Load(bool hotReload)
    {
        // Bir bölüm kurulurken hata verirse diğerleri yine çalışsın
        foreach (var (ad, kur) in new (string, Action)[] {
            ("sohbet", SohbetKur), ("discord", DiscordKur), ("reklam", ReklamKur), ("komutlar", KomutlarKur),
            ("parasut", ParasutKur), ("afiş", AfisKur), ("revolver", RevolverKur), ("gizlenme", GizlenmeKur), ("exec_after", ExecAfterKur),
            ("harita ayarı", HaritaAyarKur), ("steam grubu", SteamGrupKur), ("teşhis", TeshisKur), ("vip", VipKur) })
        {
            try { kur(); }
            catch (Exception e) { Console.WriteLine($"[YccTemel] {ad} kurulamadı: {e.Message}"); }
        }
    }

    public override void Unload(bool hotReload) => DiscordKapat();

    private static bool Gercek(CCSPlayerController? p) => p != null && p.IsValid && !p.IsBot && !p.IsHLTV;

    private static bool Yetkili(CCSPlayerController p, string yetki) =>
        string.IsNullOrEmpty(yetki) || AdminManager.PlayerHasPermissions(p, yetki);

    // Renkli sohbet satırı: başta boşluk olmazsa ilk renk karakteri CS2'de görünmüyor
    private static void Yaz(CCSPlayerController p, string s) => p.PrintToChat(" " + Metin.Renk(s));
    private static void HerkeseYaz(string s) => Server.PrintToChatAll(" " + Metin.Renk(s));
}
