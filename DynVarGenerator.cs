using NeosModLoader;
using FrooxEngine;

namespace DynVarGenerator
{
    public class DynVarGenerator : NeosMod
    {
        public override string Name => "DynVarGenerator";
        public override string Author => "TheJebForge";
        public override string Version => "1.2.1";

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
