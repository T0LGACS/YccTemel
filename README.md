# YccTemel

Sunucunun temel araçları tek eklentide: sohbet kaydı, Discord'a sohbet gönderimi, sohbet ve skor tablosu etiketleri, admin sohbeti ve gözcü, hoş geldin ve reklam mesajları, özel komutlar, paraşüt, gizli admin izleyici, haritaya özel cfg'ler, exec_after komutları, Steam grubu kısıtı.

> Core toolkit for a CS2 server: chat log, Discord relay, chat/scoreboard tags, admin chat, adverts, custom commands, parachute, map configs, Steam group gate.

[YCC TEAM](https://yccteam.com.tr) CS2 sunucusu için sıfırdan yazıldı ve sunucuda her gün çalışıyor. Başka eklentilerden kod alınmadı.

## Gereken

- CS2 sunucusu, [Metamod:Source](https://www.sourcemm.net/) ve [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) (1.0.371 ve üstü)

## Kurulum

1. Sunucuda [Metamod:Source](https://www.sourcemm.net/downloads.php?branch=dev) ve [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp/releases) (with-runtime sürümü) kurulu olmalı.
2. [.NET 10 SDK](https://dotnet.microsoft.com/download) ile derle: `dotnet build -c Release`
3. `bin/Release/net10.0/` içindeki her şeyi sunucuda `addons/counterstrikesharp/plugins/YccTemel/` klasörüne kopyala.
4. Sunucuyu başlat. Ayar dosyası ilk açılışta `addons/counterstrikesharp/configs/plugins/YccTemel/YccTemel.json` olarak kendiliğinden oluşur; düzenleyip sunucuyu yeniden başlat.

## Komutlar

Sohbette `!` ile, konsolda `css_` ile yazılır.

| Komut | Ne yapar |
|---|---|
| `!dc / !discord, !steam / !grup` | Discord ve Steam grubu bağlantısı (ayardan) |
| `!ip` | Sunucu adresi |
| `!rr` | Admin: oyunu yeniden başlatır |
| `!group_check` | Steam grubu üyeliğini yeniden sorar |
| `exec_after_delay <sn> "<komut>"` | Konsol: komutu gecikmeli çalıştırır |

## Notlar

Discord webhook adresi ve Steam API anahtarı ayar dosyasına yazılır; ikisi de boşsa o bölümler kapalı kalır.

## Lisans

MIT. Kullanabilir, değiştirebilir, paylaşabilirsin; lisans dosyasını koruman yeterli.

Sunucu: **yccteam.com.tr** · Site: https://yccteam.com.tr
