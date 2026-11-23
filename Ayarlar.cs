using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace YccTemel;

// Tüm ayarlar tek dosyada: configs/plugins/YccTemel/YccTemel.json. Renkler {GREEN}, {LIME} gibi yazılır (Metin.Renk).
public class Ayarlar : BasePluginConfig
{
    [JsonPropertyName("Sohbet")] public SohbetAyar Sohbet { get; set; } = new();
    [JsonPropertyName("Discord")] public DiscordAyar Discord { get; set; } = new();
    [JsonPropertyName("Reklam")] public ReklamAyar Reklam { get; set; } = new();
    [JsonPropertyName("Komutlar")] public List<KomutAyar> Komutlar { get; set; } = new();
    [JsonPropertyName("Parasut")] public ParasutAyar Parasut { get; set; } = new();
    [JsonPropertyName("Afis")] public AfisAyar Afis { get; set; } = new();
    [JsonPropertyName("Gizlenme")] public GizlenmeAyar Gizlenme { get; set; } = new();
    [JsonPropertyName("SteamGrup")] public SteamGrupAyar SteamGrup { get; set; } = new();
    // !ip komutunda gösterilen adres
    [JsonPropertyName("SunucuAdresi")] public string SunucuAdresi { get; set; } = "";
    // Doğuşta Deagle alınıp Revolver verilir (Revolver.cs; AWP sunucusunda açık)
    [JsonPropertyName("DeagleYerineRevolver")] public bool DeagleYerineRevolver { get; set; } = false;
    // Sezon VIP'i (Vip.cs): ycc_vip tablosunu okur. Veritabanı bilgileri YccRank'takiyle aynı.
    [JsonPropertyName("Vip")] public VipAyar Vip { get; set; } = new();
}

public class VipAyar
{
    [JsonPropertyName("Acik")] public bool Acik { get; set; } = false;
    [JsonPropertyName("Sunucu")] public string Sunucu { get; set; } = "127.0.0.1";
    [JsonPropertyName("Port")] public int Port { get; set; } = 3306;
    [JsonPropertyName("Kullanici")] public string Kullanici { get; set; } = "cs2";
    [JsonPropertyName("Sifre")] public string Sifre { get; set; } = "";
    [JsonPropertyName("Ad")] public string Ad { get; set; } = "cs2";
}

public class SohbetAyar
{
    // Panel ve ağır çekim bekçisi bu klasördeki dosyaları okuyor; yolu ve satır biçimi değişirse onlar da değişmeli.
    [JsonPropertyName("KayitKlasoru")] public string KayitKlasoru { get; set; } = "plugins/Chat_Logger/logs";
    [JsonPropertyName("KayitGun")] public int KayitGun { get; set; } = 60;
    // Bu karakterlerle başlayan mesajlar (komutlar) kaydedilmez
    [JsonPropertyName("KayitDisi")] public string KayitDisi { get; set; } = "!./";
    [JsonPropertyName("Etiketler")] public List<Etiket> Etiketler { get; set; } = new();
    // Skor tablosunda herkesin klan etiketi (boş = dokunma)
    [JsonPropertyName("SkorEtiketi")] public string SkorEtiketi { get; set; } = "[YCC]";
    // Bu yetkideki adminler karşı takımın takım sohbetini görür; "@" ile başlayan takım mesajı sadece onlara gider
    [JsonPropertyName("AdminYetki")] public string AdminYetki { get; set; } = "@css/chat";
}

public class Etiket
{
    // "@css/chat" gibi bir yetki ya da "herkes"; yukarıdan aşağı ilk uyan kullanılır
    [JsonPropertyName("Kim")] public string Kim { get; set; } = "herkes";
    [JsonPropertyName("On")] public string On { get; set; } = "";
    [JsonPropertyName("IsimRenk")] public string IsimRenk { get; set; } = "";
    [JsonPropertyName("MesajRenk")] public string MesajRenk { get; set; } = "";
    [JsonPropertyName("Genel")] public bool Genel { get; set; } = true;   // genel sohbette uygulansın mı
    [JsonPropertyName("Takim")] public bool Takim { get; set; } = true;   // takım sohbetinde uygulansın mı
}

public class DiscordAyar
{
    [JsonPropertyName("Webhook")] public string Webhook { get; set; } = "";
    [JsonPropertyName("Bicim")] public string Bicim { get; set; } = "[VDS] [{SAAT}] {TAKIM} : {MESAJ}";
    [JsonPropertyName("Renk")] public string Renk { get; set; } = "#FF7A00";
    // Vortexa (YCC TEAM sunucusu) webhook'ları: sohbet Discord'a ek olarak buraya da, giriş-çıkış sadece buraya (IP'siz).
    [JsonPropertyName("VortexaSohbet")] public string VortexaSohbet { get; set; } = "";
    [JsonPropertyName("VortexaGiris")] public string VortexaGiris { get; set; } = "";
    [JsonPropertyName("Etiket")] public string Etiket { get; set; } = "";   // giriş-çıkış satırının başı, örn. [ANA]
}

public class ReklamAyar
{
    // {ISIM} oyuncunun adı
    [JsonPropertyName("Hosgeldin")] public string Hosgeldin { get; set; } = "";
    [JsonPropertyName("HosgeldinGecikme")] public float HosgeldinGecikme { get; set; } = 5;
    // Girişte ekran ortası afişi (CS2'de CS:GO'daki MOTD penceresi yok): ilk doğuşta Sure saniye gösterilir. Resim boşsa kapalı.
    [JsonPropertyName("GirisAfisi")] public string GirisAfisi { get; set; } = "";
    [JsonPropertyName("GirisAfisiYazi")] public string GirisAfisiYazi { get; set; } = "";
    [JsonPropertyName("GirisAfisiSure")] public float GirisAfisiSure { get; set; } = 6;
    // Oyun resmi kendi boyutundan bağımsız küçültüyor; style ile verilen boyut geçerli oluyor (2026-11-16 oyunda denendi)
    [JsonPropertyName("GirisAfisiGenislik")] public int GirisAfisiGenislik { get; set; } = 900;
    [JsonPropertyName("GirisAfisiYukseklik")] public int GirisAfisiYukseklik { get; set; } = 270;
    // Oyun sırasında da herkese bu kadar saniyede bir gösterilir (0 = sadece girişte)
    [JsonPropertyName("AfisAralik")] public float AfisAralik { get; set; } = 0;
    // Her grup kendi aralığında mesajlarını sırayla gösterir
    [JsonPropertyName("Gruplar")] public List<ReklamGrup> Gruplar { get; set; } = new();
}

public class ReklamGrup
{
    [JsonPropertyName("Aralik")] public float Aralik { get; set; } = 40;
    [JsonPropertyName("Mesajlar")] public List<string> Mesajlar { get; set; } = new();
}

public class KomutAyar
{
    // Virgülle birden çok ad: "discord,dc" → !discord ve !dc
    [JsonPropertyName("Ad")] public string Ad { get; set; } = "";
    [JsonPropertyName("Yetki")] public string Yetki { get; set; } = "";
    [JsonPropertyName("Calistir")] public List<string> Calistir { get; set; } = new();
    [JsonPropertyName("Mesaj")] public string Mesaj { get; set; } = "";
    [JsonPropertyName("Herkese")] public bool Herkese { get; set; }
    [JsonPropertyName("Orta")] public string Orta { get; set; } = "";        // ekran ortası yazısı (sadece yazana)
}

// Haritalardaki site afişi panoları (Afis.cs). Yerler oyun içinde !afis ile seçilir, afisler.json'a yazılır.
public class AfisAyar
{
    [JsonPropertyName("Acik")] public bool Acik { get; set; } = true;
    [JsonPropertyName("Yetki")] public string Yetki { get; set; } = "@css/root";
    // ycc_hud Workshop eklentisindeki modeller (üretici github/YccRank/hud/afis_model.py)
    [JsonPropertyName("GenisModel")] public string GenisModel { get; set; } = "models/ycc/afis_genis.vmdl";
    [JsonPropertyName("KareModel")] public string KareModel { get; set; } = "models/ycc/afis_kare.vmdl";
}

public class ParasutAyar
{
    [JsonPropertyName("Acik")] public bool Acik { get; set; } = true;
    [JsonPropertyName("DusmeHizi")] public float DusmeHizi { get; set; } = 100;
    [JsonPropertyName("Yetki")] public string Yetki { get; set; } = "";
    // Kendi paraşüt modelimiz (Workshop 3840387003, MultiAddonManager indirtir; üretici plugin-kaynak/YccParasut). Boş = model yok.
    [JsonPropertyName("Model")] public string Model { get; set; } = "models/ycc/parasut.vmdl";
}

public class GizlenmeAyar
{
    [JsonPropertyName("Yetki")] public string Yetki { get; set; } = "@css/ban";
    [JsonPropertyName("Mesaj")] public string Mesaj { get; set; } = "{GREEN}[YCC]{DEFAULT} Gizli izleyici moduna geçtin.";
}

public class SteamGrupAyar
{
    [JsonPropertyName("GrupId")] public string GrupId { get; set; } = "";
    [JsonPropertyName("ApiAnahtari")] public string ApiAnahtari { get; set; } = "";
    [JsonPropertyName("Komutlar")] public List<string> Komutlar { get; set; } = new();
    [JsonPropertyName("Yasak")] public string Yasak { get; set; } = "";
    [JsonPropertyName("Uye")] public string Uye { get; set; } = "";
    [JsonPropertyName("UyeDegil")] public string UyeDegil { get; set; } = "";
}
