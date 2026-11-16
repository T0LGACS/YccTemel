using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace YccTemel;

// Haritaya özel ayarlar (panelin "harita ayarları" bölümü bu dosyaları düzenliyor), cfg/ klasörüne göre:
//   mapconfigs/prefixes/aim_.cfg   → adı aim_ ile başlayan her haritada, açılıştan 1.1 sn sonra
//   mapconfigs/<harita>.cfg        → o haritada, açılıştan 1.2 sn sonra (önek ayarını ezer)
//   mapconfigs/forced/<harita>.cfg → her raunt donma süresi bitince (haritanın kendi ayarlarını ezmek için)
public partial class YccTemel
{
    private void HaritaAyarKur()
    {
        RegisterListener<Listeners.OnMapStart>(harita =>
        {
            var onek = Metin.HaritaOnek(harita);
            if (onek != null) AddTimer(1.1f, () => Server.ExecuteCommand($"execifexists mapconfigs/prefixes/{onek}.cfg"), TimerFlags.STOP_ON_MAPCHANGE);
            AddTimer(1.2f, () => Server.ExecuteCommand($"execifexists mapconfigs/{harita}.cfg"), TimerFlags.STOP_ON_MAPCHANGE);
        });
        RegisterEventHandler<EventRoundFreezeEnd>((_, _) =>
        {
            Server.ExecuteCommand($"execifexists mapconfigs/forced/{Server.MapName}.cfg");
            return HookResult.Continue;
        });
    }
}
