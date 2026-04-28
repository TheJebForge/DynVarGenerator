using FrooxEngine;
using ResoniteModLoader;

namespace DynVarGenerator
{
    public class DynVarGenerator : ResoniteMod
    {
        public override string Name => "DynVarGenerator";
        public override string Author => "TheJebForge";
        public override string Version => "1.3.4";
        public override string Link => "https://github.com/TheJebForge/DynVarGenerator/";

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
