using FrooxEngine;
using ResoniteModLoader;

namespace DynVarGenerator
{
    public class DynVarGenerator : ResoniteMod
    {
        public override string Name => "DynVarGenerator";
        public override string Author => "TheJebForge";
        public override string Version => "1.3.2";

        public override void OnEngineInit()
        {
            Engine.Current.RunPostInit(
                () => DevCreateNewForm.AddAction(
                    "Editor", 
                    "DynVar Generator (Mod)", 
                    x => new Wizard(x)));
        }
    }
}
