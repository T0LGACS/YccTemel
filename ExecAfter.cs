using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace YccTemel;

// cfg dosyalarından kullanılan zamanlı komutlar: exec_after_delay <sn> "<komut>", exec_after_map_start "<komut>" vb.
// ortak_ayarlar.cfg bunu kullanıyor (exec_after_delay 1 "exec ozelayar.cfg"). Oyuncu çalıştıracaksa @css/rcon gerekir.
public partial class YccTemel
{
    // olay adı → (her seferinde, bir kerelik)
    private readonly Dictionary<string, (string? her, string? bir)> _sonra = new()
    {
        ["map_start"] = (null, null), ["map_end"] = (null, null), ["round_start"] = (null, null),
    };

    private void ExecAfterKur()
    {
        AddCommand("exec_after", "exec_after komutlarının listesi", (p, info) => info.ReplyToCommand(
            "exec_after_delay <sn> \"<komut>\" | exec_after_map_start[_once] | exec_after_map_end[_once] | exec_after_round_start[_once] \"<komut>\""));

        AddCommand("exec_after_delay", "Komutu gecikmeli çalıştırır", (p, info) =>
        {
            if (!Rcon(p, info)) return;
            var komut = info.ArgCount >= 3 ? Metin.IlkArgumanSonrasi(info.ArgString, info.GetArg(1)) : "";
            if (!float.TryParse(info.GetArg(1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sn) || sn < 0 || komut == "")
            {
                info.ReplyToCommand("Kullanım: exec_after_delay <saniye> \"<komut>\"");
                return;
            }
            AddTimer(sn, () => Server.ExecuteCommand(komut));
        });

        foreach (var olay in _sonra.Keys.ToList())
            foreach (var bir in new[] { false, true })
            {
                var ad = "exec_after_" + olay + (bir ? "_once" : "");
                AddCommand(ad, "Olayda komut çalıştırır", (p, info) =>
                {
                    if (!Rcon(p, info)) return;
                    var komut = Metin.Tirnaksiz(info.ArgString);
                    var (her, tek) = _sonra[olay];
                    if (komut != "") _sonra[olay] = bir ? (her, komut) : (komut, tek);
                    info.ReplyToCommand($"{ad} = {(bir ? _sonra[olay].bir : _sonra[olay].her)}");
                });
            }

        RegisterListener<Listeners.OnMapStart>(_ => Server.NextFrame(() => Calistir("map_start")));
        RegisterListener<Listeners.OnMapEnd>(() => Calistir("map_end"));
        RegisterEventHandler<EventRoundStart>((_, _) => { Server.NextFrame(() => Calistir("round_start")); return HookResult.Continue; });
    }

    private void Calistir(string olay)
    {
        var (her, bir) = _sonra[olay];
        if (!string.IsNullOrEmpty(her)) Server.ExecuteCommand(her);
        if (!string.IsNullOrEmpty(bir)) { Server.ExecuteCommand(bir); _sonra[olay] = (her, null); }
    }

    private bool Rcon(CCSPlayerController? p, CommandInfo info)
    {
        if (p == null || Yetkili(p, "@css/rcon")) return true;
        info.ReplyToCommand("Bu komut için @css/rcon yetkisi gerekir.");
        return false;
    }
}
