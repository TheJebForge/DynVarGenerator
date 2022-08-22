using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;

namespace DynVarGenerator
{
    public class WizardSettings
    {
        readonly Wizard _wizard;

        public WizardSettings(Wizard wizard) {
            _wizard = wizard;
        }

        public bool AddAllChildrenOfSlots = false;
        public bool SetCurrentValueAsDefault = false;
        public string DynVarNameFormat = "{0}";

        public void BuildSettings(UIBuilder ui) {
            ui.Text("Add Options").AutoSizeMax.Value = 20;
            ui.Checkbox("Add all children of slots into list", AddAllChildrenOfSlots).State.OnValueChange += field => AddAllChildrenOfSlots = field.Value;
            ui.Empty("Gap");
            ui.Text("DynVar Options").AutoSizeMax.Value = 20;
            ui.ValueRadio("Create Dynamic Fields if possible", _wizard.DynVarMode.Value, 0);
            ui.ValueRadio("Create Dynamic Variables", _wizard.DynVarMode.Value, 1);
            ui.ValueRadio("Create Dynamic Drivers (for fields only)", _wizard.DynVarMode.Value, 2);
            ui.Checkbox("Use current values as default for drivers", SetCurrentValueAsDefault).State.OnValueChange += field => SetCurrentValueAsDefault = field.Value;
            ui.Empty("Gap");
            ui.Text("DynVar Name Format").AutoSizeMax.Value = 20;
            ui.TextField("{0}").Text.Content.OnValueChange += field => DynVarNameFormat = field.Value;
            ui.Text("Format help:\n{0} - Element name\n{1} - Slot name\n{2} - Current value/Element name\n{3} - Current value slot (for refs only)\n{4} - Target slot name", alignment: Alignment.MiddleLeft).AutoSizeMax.Value = 20;
            ui.Empty("Gap");
            ui.Text("Where to create DynVars").AutoSizeMax.Value = 20;
            SyncMemberEditorBuilder.Build(_wizard.DynVarSlot.Reference, null, null, ui);
        }
    }
}