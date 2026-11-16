using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace YccTemel;

// Yetkili admin izleyiciye geçince takımsız (gizli) izleyici olur; skor tablosunda izleyicilerde görünmez.
public partial class YccTemel
{
    private void GizlenmeKur()
    {
        AddCommandListener("jointeam", (p, info) =>
        {
            if (!Gercek(p) || info.GetArg(1) != "1" || !Yetkili(p!, Config.Gizlenme.Yetki)) return HookResult.Continue;
            if (p!.PawnIsAlive) p.PlayerPawn.Value?.CommitSuicide(false, true);
            p.ChangeTeam(CsTeam.None);
            Yaz(p, Config.Gizlenme.Mesaj);
            return HookResult.Stop;
        });
    }
}
