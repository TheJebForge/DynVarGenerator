using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DynVarGenerator
{
    public class Wizard
    {
        const string DynVarButtonText = "Create DynVars";

        readonly Slot _wizardRoot;
        readonly WizardSettings _wizardSettings;

        NeosCanvasPanel _canvasPanel;

        ReferenceField<IWorldElement> _newField;
        ReferenceMultiplexer<IWorldElement> _fields;
        public ValueField<int> DynVarMode;
        public ReferenceField<Slot> DynVarSlot;

        public Wizard(Slot slot) {
            _wizardRoot = slot;
            _wizardSettings = new WizardSettings(this);
            PreparePanel();
        }

        void PrepareDataVariables() {
            Slot data = _wizardRoot.AddSlot("Data");

            _newField = data.AttachComponent<ReferenceField<IWorldElement>>();
            _newField.Reference.OnTargetChange += AddFieldToList;

            _fields = data.AttachComponent<ReferenceMultiplexer<IWorldElement>>();
            DynVarMode = data.AttachComponent<ValueField<int>>();
            DynVarSlot = data.AttachComponent<ReferenceField<Slot>>();
        }

        void PreparePanel() {
            _wizardRoot.Tag = "Developer";
            _wizardRoot.PersistentSelf = false;

            _canvasPanel = _wizardRoot.AttachComponent<NeosCanvasPanel>();
            _canvasPanel.Panel.AddCloseButton();
            _canvasPanel.Panel.AddParentButton();
            _canvasPanel.Panel.Title = "DynVar Generator";
            _canvasPanel.Canvas.Size.Value = new float2(1000f, 1000f);

            PrepareDataVariables();
            PrepareInterface();
        }

        void PrepareInterface() {
            Image img = _canvasPanel.Canvas.Slot.AttachComponent<Image>();
            img.Tint.Value = new color(1f, 0.2f);

            UIBuilder ui = new UIBuilder(_canvasPanel.Canvas);
            ui.Canvas.MarkDeveloper();
            ui.Canvas.AcceptPhysicalTouch.Value = false;

            ui.NestInto(ui.Empty("Split"));
            {
                ui.SplitHorizontally(0.6f, out RectTransform left, out RectTransform right);

                left.OffsetMax.Value = new float2(-2f);
                right.OffsetMin.Value = new float2(2f);

                ui.NestInto(left);
                {
                    ui.HorizontalHeader(100f, out RectTransform header, out RectTransform content);

                    ui.NestInto(header);
                    {
                        // Creating a convenient drop area to drop fields/lists into
                        Button dropRef = ui.Button("Drop anything here");
                        dropRef.Slot.GetComponent<RectTransform>().OffsetMin.Value = new float2(0f, 8f);
                        RefEditor refEditor = dropRef.Slot.AttachComponent<RefEditor>();
                        ((RelayRef<ISyncRef>)refEditor.GetSyncMember("_targetRef")).Target = (ISyncRef)_newField.GetSyncMember("Reference");
                        ((SyncRef<Button>)refEditor.GetSyncMember("_button")).Target = dropRef;
                    }


                    ui.NestInto(content);
                    {
                        ui.ScrollArea();
                        {
                            ui.FitContent(SizeFit.Disabled, SizeFit.MinSize);
                            SyncMemberEditorBuilder.Build(_fields.References, "Elements to generate for", null, ui);
                        }
                        ui.NestOut();
                    }
                }

                ui.NestInto(right);
                {
                    ui.Style.MinHeight = 24f;
                    ui.Style.ForceExpandHeight = false;

                    ui.VerticalLayout(4f).VerticalAlign.Value = LayoutVerticalAlignment.Top;
                    {
                        _wizardSettings.BuildSettings(ui);
                        ui.Style.FlexibleHeight = 1;
                        ui.Empty("Gap");
                        ui.Style.FlexibleHeight = -1;
                        ui.Style.MinHeight = 50f;
                        ui.Button(DynVarButtonText).LocalPressed += CreateDynVars;
                    }
                    ui.NestOut();
                }
            }
        }

        void AddFieldToList(SyncRef<IWorldElement> reference) {
            switch (reference.Target) {
                case null:
                    return;
                case ISyncList list:
                    _fields.References.AddRange((IEnumerable<ISyncMember>)list.Elements);
                    break;
                case Slot slot:
                    if (_wizardSettings.AddAllChildrenOfSlots) {
                        AddAllChildren(slot);
                    }
                    else {
                        _fields.References.Add(slot);
                    }
                    break;
                default:
                    _fields.References.Add(reference.Target);
                    break;
            }

            _newField.RunInUpdates(1, () => _newField.Reference.Target = null);
        }

        void AddAllChildren(Slot root) {
            Queue<Slot> slotQueue = new Queue<Slot>();
            slotQueue.Enqueue(root);

            while (slotQueue.Count > 0) {
                Slot current = slotQueue.Dequeue();

                _fields.References.Add(current);

                foreach (Slot child in current.Children) {
                    slotQueue.Enqueue(child);
                }
            }
        }

        string FormatDynVarName(IWorldElement element, Slot targetSlot) {
            string elementName = element.Name;

            if (element.Parent is ISyncList parentList) {
                int index = int.Parse(element.Name.Split('[', ']')[1]);

                elementName = index.ToString();

                if (parentList.Name.Equals("BlendShapeWeights")) {
                    ImplementableComponent comp = element.FindNearestParent<ImplementableComponent>();
                    if (comp is SkinnedMeshRenderer renderer) {
                        elementName = renderer.BlendShapeName(index);
                    }
                }
            }
            
            Slot elementSlot = element.FindNearestParent<Slot>();
            
            string curValueName = "null";
            string curValueSlotName = "null";
            
            switch (element) {
                case ISyncRef syncRef:
                    if (syncRef.Target != null) {
                        curValueName = syncRef.Target.Name;
                        curValueSlotName = syncRef.FindNearestParent<Slot>().Name;
                    }
                    break;
                case IField field:
                    curValueName = field.BoxedValue.ToString();
                    break;
            }

            return string.Format(
                _wizardSettings.DynVarNameFormat,
                elementName,
                elementSlot.Name,
                curValueName,
                curValueSlotName,
                targetSlot.Name);
        }
        bool CreateDynField(IWorldElement element, Slot targetSlot) {
            switch (element) {
                case ISyncRef syncRef:
                    GetType().GetMethod("AttachDynRefField")
                        ?.MakeGenericMethod(syncRef.TargetType)
                        .Invoke(this, new object[] { syncRef, targetSlot });
                    break;
                case IField field:
                    GetType().GetMethod("AttachDynValueField")
                        ?.MakeGenericMethod(field.ValueType)
                        .Invoke(this, new object[] { field, targetSlot });
                    break;
                default:
                    return false;
            }


            return true;
        }

        public void AttachDynRefField<T>(ISyncRef element, Slot targetSlot) where T : class, IWorldElement {
            DynamicReference<T> dynVar = targetSlot.AttachComponent<DynamicReference<T>>();
            dynVar.VariableName.Value = FormatDynVarName(element, targetSlot);
            dynVar.TargetReference.Target = (SyncRef<T>)element;
        }
        
        public void AttachDynValueField<T>(IField element, Slot targetSlot) {
            DynamicField<T> dynVar = targetSlot.AttachComponent<DynamicField<T>>();
            dynVar.VariableName.Value = FormatDynVarName(element, targetSlot);
            dynVar.TargetField.Target = (IField<T>)element;
        }

        void CreateDynVar(IWorldElement element, Slot targetSlot) {
            NeosMod.Msg(element.GetType());

            switch (element) {
                case ISyncRef syncRef:
                    GetType().GetMethod("AttachDynRefVar")
                        ?.MakeGenericMethod(syncRef.TargetType)
                        .Invoke(this, new object[] { syncRef.Target, targetSlot });
                    break;
                case IField field:
                    GetType().GetMethod("AttachDynValueVar")
                        ?.MakeGenericMethod(field.ValueType)
                        .Invoke(this, new object[] { field, targetSlot });
                    break;
                default:
                    GetType().GetMethod("AttachDynRefVar")
                        ?.MakeGenericMethod(element.GetType())
                        .Invoke(this, new object[] { element, targetSlot });
                    break;
            }
        }

        public void AttachDynRefVar<T>(IWorldElement value, Slot targetSlot) where T : class, IWorldElement {
            DynamicReferenceVariable<T> dynVar = targetSlot.AttachComponent<DynamicReferenceVariable<T>>();
            dynVar.VariableName.Value = FormatDynVarName(value, targetSlot);
            dynVar.Reference.Target = (T)value;
        }
        
        public void AttachDynValueVar<T>(IField field, Slot targetSlot) {
            DynamicValueVariable<T> dynVar = targetSlot.AttachComponent<DynamicValueVariable<T>>();
            dynVar.VariableName.Value = FormatDynVarName(field, targetSlot);
            dynVar.Value.Value = (T)field.BoxedValue;
        }

        bool CreateDynDriver(IWorldElement element, Slot targetSlot) {
            switch (element) {
                case ISyncRef syncRef:
                    GetType().GetMethod("AttachDynRefDriver")
                        ?.MakeGenericMethod(syncRef.TargetType)
                        .Invoke(this, new object[] { syncRef, targetSlot });
                    break;
                case IField field:
                    GetType().GetMethod("AttachDynValueDriver")
                        ?.MakeGenericMethod(field.ValueType)
                        .Invoke(this, new object[] { field, targetSlot });
                    break;
                default:
                    return false;
            }
            
            return true;
        } 
        
        public void AttachDynRefDriver<T>(ISyncRef element, Slot targetSlot) where T : class, IWorldElement {
            DynamicReferenceVariableDriver<T> dynVar = targetSlot.AttachComponent<DynamicReferenceVariableDriver<T>>();
            dynVar.VariableName.Value = FormatDynVarName(element, targetSlot);

            if (_wizardSettings.SetCurrentValueAsDefault)
                dynVar.DefaultTarget.Target = (T)element.Target;

            dynVar.Target.Target = (SyncRef<T>)element;
        }
        
        public void AttachDynValueDriver<T>(IField element, Slot targetSlot) {
            DynamicValueVariableDriver<T> dynVar = targetSlot.AttachComponent<DynamicValueVariableDriver<T>>();
            dynVar.VariableName.Value = FormatDynVarName(element, targetSlot);

            if (_wizardSettings.SetCurrentValueAsDefault)
                dynVar.DefaultValue.Value = (T)element.BoxedValue;
            
            dynVar.Target.Target = (IField<T>)element;
        }
        
        void CreateDynVars(IButton button, ButtonEventData eventData) {
            Slot targetSlot = DynVarSlot.Reference.Target;

            if (targetSlot != null) {
                int mode = DynVarMode.Value.Value;
                int countProcessed = 0;

                foreach (IWorldElement element in _fields.References) {
                    if (mode < 2) {
                        if (mode == 0 && CreateDynField(element, targetSlot)) {
                            countProcessed++;
                        }
                        else {
                            CreateDynVar(element, targetSlot);
                            countProcessed++;
                        }
                    }
                    else {
                        if (CreateDynDriver(element, targetSlot)) {
                            countProcessed++;
                        }
                    }
                }

                button.LabelText = $"Done! Processed: {countProcessed}";
            }
            else {
                button.LabelText = "Target Slot was not provided";
            }

            button.RunInSeconds(3f, () => button.LabelText = DynVarButtonText);
        }
    }
}