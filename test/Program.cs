using System.Text.RegularExpressions;
using YccTemel;

// YccTemel'in saf yardımcılarının denemesi: dotnet run (bu klasörde)
var hata = 0;
void Esit(object? beklenen, object? gelen, string ne)
{
    if (Equals(beklenen, gelen)) return;
    hata++;
    Console.WriteLine($"HATA {ne}: beklenen [{beklenen}] gelen [{gelen}]");
}

// Kayıt satırı, agir-cekim-izle.sh'deki ayrıştırıcıyla birebir okunabilmeli
var t = new DateTime(2026, 11, 15, 9, 5, 7);
var satir = Metin.KayitSatiri(t, 76561197960287930, "1.2.3.4", true, "Ali Veli", "selam millet");
Esit("[09:05:07] [76561197960287930] 1.2.3.4 [TEAM] (Ali Veli) : selam millet", satir, "kayıt satırı");
var m = Regex.Match(satir, @"\[(\d\d):(\d\d):(\d\d)\] \[(\d+)\] \S+ \[\w+\] \(.*?\) : (.*)");
Esit(true, m.Success, "bekçi ayrıştırıcısı");
Esit("selam millet", m.Groups[5].Value, "bekçi mesaj grubu");
Esit(true, Metin.KayitSatiri(t, 1, "", false, "x", "y").Contains(" - [ALL] "), "IP yoksa tire");
Esit("09:00_11_15_2026.txt".Replace(":", ""), Metin.KayitDosyasi(t), "kayıt dosyası adı");

Esit(true, Metin.Kaydedilmez("!ws", "!./"), "komut kaydedilmez");
Esit(true, Metin.Kaydedilmez("/knife", "!./"), "eğik çizgi kaydedilmez");
Esit(false, Metin.Kaydedilmez("selam", "!./"), "normal mesaj kaydedilir");
Esit("1.2.3.4", Metin.IpYalin("1.2.3.4:27005"), "ip portsuz");
Esit("", Metin.IpYalin(null), "ip yok");

Esit("exec ozelayar.cfg", Metin.IlkArgumanSonrasi("1 \"exec ozelayar.cfg\"", "1"), "exec_after_delay tırnaklı");
Esit("sv_hibernate_when_empty 1", Metin.IlkArgumanSonrasi("45 sv_hibernate_when_empty 1", "45"), "exec_after_delay tırnaksız");
Esit("say a", Metin.Tirnaksiz(" \"say a\" "), "tırnak soyma");

Esit("aim_", Metin.HaritaOnek("aim_redline"), "harita öneki");
Esit(null, Metin.HaritaOnek("lobby"), "öneksiz harita");
Esit("!ws", Metin.IlkKelime("  !ws ak47"), "ilk kelime");

Esit("\u0004yeşil\u0001 normal", Metin.Renk("{GREEN}yeşil{default} normal"), "renk");
Esit(Metin.Renk("{GREY}"), Metin.Renk("{GRAY}"), "GRAY = GREY");
Esit("{BILINMEYEN}", Metin.Renk("{BILINMEYEN}"), "bilinmeyen renk dokunulmaz");
Esit(0xFF7A00, Metin.HexRenk("#FF7A00"), "hex renk");
Esit("[VDS] [10:00] [ALL] : hey", Metin.Doldur("[VDS] [{SAAT}] {TAKIM} : {MESAJ}", ("SAAT", "10:00"), ("TAKIM", "[ALL]"), ("MESAJ", "hey")), "yer tutucu");

var govde = Metin.DiscordGovde("Ali", 7, null, "@everyone selam", 1);
Esit(true, govde.Contains("\"parse\":[]"), "discord etiketleme kapalı");
Esit(false, govde.Contains("icon_url"), "avatar yoksa alan yok");

Console.WriteLine(hata == 0 ? "TAMAM: tüm denemeler geçti" : $"{hata} deneme başarısız");
return hata == 0 ? 0 : 1;
