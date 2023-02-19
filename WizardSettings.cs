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
        public bool OverrideOnLink = false;
        public string DynVarNameFormat = "{0}";
        public bool DynamicVariableSpaces = false;
        public bool OnlyDirectBinding = false;
        public bool ReferenceFieldInstead = false;
        public string DynVarSpaceNameFormat = "";

        public void BuildSettings(UIBuilder ui) {
            ui.Text("Add Options").AutoSizeMax.Value = 20;
            ui.Checkbox("Add all children of slots into list", AddAllChildrenOfSlots).State.OnValueChange += field => AddAllChildrenOfSlots = field.Value;
            ui.Empty("Gap");
            ui.Text("DynVar Options").AutoSizeMax.Value = 20;
            ui.ValueRadio("Create Dynamic Fields if possible", _wizard.DynVarMode.Value, 0);
            ui.ValueRadio("Create Dynamic Variables", _wizard.DynVarMode.Value, 1);
            ui.Checkbox("Reference the field instead", OverrideOnLink).State.OnValueChange += field => ReferenceFieldInstead = field.Value;
            ui.Checkbox("Override On Link", OverrideOnLink).State.OnValueChange += field => OverrideOnLink = field.Value;
            ui.ValueRadio("Create Dynamic Drivers (for fields only)", _wizard.DynVarMode.Value, 2);
            ui.Checkbox("Use current values as default for drivers", SetCurrentValueAsDefault).State.OnValueChange += field => SetCurrentValueAsDefault = field.Value;
            ui.ValueRadio("Create Dynamically driven ValueCopies", _wizard.DynVarMode.Value, 3);
            ui.Empty("Gap");
            ui.Text("DynVar Name Format").AutoSizeMax.Value = 20;
            ui.TextField(DynVarNameFormat).Text.Content.OnValueChange += field => DynVarNameFormat = field.Value;
            ui.Text("Format help:\n{0} - Element name\n{1} - Slot name\n{2} - Slot tag\n{3} - Current value/Element name\n{4} - Current value slot (for refs only)\n{5} - Current value slot tag (for refs only)\n{6} - Target slot name\n{7} - Target slot tag", alignment: Alignment.MiddleLeft).AutoSizeMax.Value = 20;
            ui.Empty("Gap");
            ui.Text("Where to create DynVars").AutoSizeMax.Value = 20;
            ui.ValueRadio("Under each element's slot", _wizard.DynVarPlacementMode.Value, 0);
            ui.ValueRadio("Under specific slot", _wizard.DynVarPlacementMode.Value, 1);
            SyncMemberEditorBuilder.Build(_wizard.DynVarSlot.Reference, null, null, ui);
            ui.Empty("Gap");
            ui.Text("Dynamic Variable Spaces").AutoSizeMax.Value = 20;
            ui.Checkbox("Create Dynamic Variable Spaces", DynamicVariableSpaces).State.OnValueChange += field => DynamicVariableSpaces = field.Value;
            ui.Checkbox("Only Direct Binding", OnlyDirectBinding).State.OnValueChange += field => OnlyDirectBinding = field.Value;
            ui.Empty("Gap");
            ui.Text("DynVarSpace Name Format").AutoSizeMax.Value = 20;
            ui.TextField(DynVarSpaceNameFormat).Text.Content.OnValueChange += field => DynVarSpaceNameFormat = field.Value;
            ui.Text("(Formatting from above also works here)").AutoSizeMax.Value = 20;
        }
    }
}