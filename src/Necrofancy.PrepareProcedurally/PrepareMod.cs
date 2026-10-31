using Necrofancy.PrepareProcedurally.Interface;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public class PrepareMod : Mod
{
    public PrepareMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<PrepareModSettings>();
    }
    
    internal static PrepareModSettings Settings { get; private set; }
    
    public override void DoSettingsWindowContents(Rect rect)
    {
        PrepareSettingsDialog.DrawModSettings(rect);
    }

    public override string SettingsCategory()
    {
        return "Button".PpTranslate();
    }
}