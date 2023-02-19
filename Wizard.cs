using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DynVarGenerator
{
    public class Wizard
    {
        const string DynVarButtonText = "Create DynVars";

        readonly Slot _wizardRoot;
        readonly WizardSettings _wizardSettings;
        
        readonly Regex _invalidPattern = new Regex(@"[^A-Za-z0-9 ._\/]");
        readonly Regex _checkForMultipleSlashes = new Regex(@"\/.*\/");

        private readonly Dictionary<char, string> _replacementMap = new Dictionary<char, string>
        {
            {'+', "p"},
            {'/', "."},
            {'!', "exc"},
            {'\'', ""}
        };

        NeosCanvasPanel _canvasPanel;

        ReferenceField<IWorldElement> _newField;
        ReferenceMultiplexer<IWorldElement> _fields;
        public ValueField<int> DynVarMode;
        public ValueField<int> DynVarPlacementMode;
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
            
            DynVarPlacementMode = data.AttachComponent<ValueField<int>>();
            DynVarPlacementMode.Value.Value = 1;
            
            DynVarSlot = data.AttachComponent<ReferenceField<Slot>>();
        }

        void PreparePanel() {
            _wizardRoot.Tag = "Developer";
            _wizardRoot.PersistentSelf = false;

            _canvasPanel = _wizardRoot.AttachComponent<NeosCanvasPanel>();
            _canvasPanel.Panel.AddCloseButton();
            _canvasPanel.Panel.AddParentButton();
            _canvasPanel.Panel.Title = "DynVar Generator (Mod)";
            _canvasPanel.Canvas.Size.Value = new float2(1000f, 1100f);

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
                ui.SplitHorizontally(0.55f, out RectTransform left, out RectTransform right);

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
                        ui.Text("Operations").AutoSizeMax.Value = 20;
                        ui.Button("Clear List").LocalPressed += (button, data) => _fields.References.Clear();
                        ui.Empty("Gap");
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

        bool AllowedCharacter(char character)
        {
            switch ((int)character)
            {
                case int a when (a >= 65 && a <= 90):
                case int b when (b >= 97 && b <= 122):
                case int c when (c == 32 || c == 46 || c == 95):
                case int d when (d >= 48 && d <= 57):
                    return true;
                default:
                    return false;
            }
        }
        
        string ReplaceForbiddenCharacters(string variableName)
        {
            if (_checkForMultipleSlashes.IsMatch(variableName) ||
                _invalidPattern.IsMatch(variableName))
            {
                var result = "";
                var slashProcessed = false;

                foreach (var character in variableName.ToCharArray())
                {
                    if (character == 47)
                    {
                        result += slashProcessed ? '.' : '/';
                        slashProcessed = true;
                    } else if (AllowedCharacter(character))
                    {
                        result += character;
                    }
                    else
                    {
                        if (_replacementMap.ContainsKey(character))
                        {
                            result += _replacementMap[character];
                        }
                        else
                        {
                            result += '_';
                        }
                    }
                }

                return result;
            }

            return variableName;
        }

        string FormatName(string format, IWorldElement element, Slot targetSlot) {
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
            
            string curValueName = "";
            string curValueSlotName = "";
            string curValueSlotTag = "";
            
            switch (element) {
                case ISyncRef syncRef:
                    if (syncRef.Target != null) {
                        curValueName = syncRef.Target.Name;
                        Slot valueSlot = syncRef.FindNearestParent<Slot>();
                        curValueSlotName = valueSlot.Name;
                        curValueSlotTag = valueSlot.Tag;
                    }
                    break;
                case IField field:
                    curValueName = field.BoxedValue.ToString();
                    break;
            }

            return ReplaceForbiddenCharacters(string.Format(
                format,
                elementName,
                elementSlot.Name,
                elementSlot.Tag,
                curValueName,
                curValueSlotName,
                curValueSlotTag,
                targetSlot.Name,
                targetSlot.Tag));
        }

        void CreateDynVarSpace(IWorldElement element, Slot targetSlot) {
            if (targetSlot.GetComponent<DynamicVariableSpace>() != null) return;
            
            DynamicVariableSpace space = targetSlot.AttachComponent<DynamicVariableSpace>();
            space.SpaceName.Value = FormatName(_wizardSettings.DynVarSpaceNameFormat, element, targetSlot);
            space.OnlyDirectBinding.Value = _wizardSettings.OnlyDirectBinding;
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
            dynVar.VariableName.Value = FormatName(_wizardSettings.DynVarNameFormat, element, targetSlot);
            dynVar.OverrideOnLink.Value = _wizardSettings.OverrideOnLink;
            dynVar.TargetReference.Target = (SyncRef<T>)element;
        }
        
        public void AttachDynValueField<T>(IField element, Slot targetSlot) {
            DynamicField<T> dynVar = targetSlot.AttachComponent<DynamicField<T>>();
            dynVar.VariableName.Value = FormatName(_wizardSettings.DynVarNameFormat, element, targetSlot);
            dynVar.OverrideOnLink.Value = _wizardSettings.OverrideOnLink;
            dynVar.TargetField.Target = (IField<T>)element;
        }

        void CreateDynVar(IWorldElement element, Slot targetSlot) {
            switch (element) {
                case ISyncRef syncRef:
                    GetType().GetMethod("AttachDynRefVar")
                        ?.MakeGenericMethod(
                            _wizardSettings.ReferenceFieldInstead ? syncRef.GetType() : syncRef.TargetType
                        ).Invoke(this, new object[]
                        {
                            _wizardSettings.ReferenceFieldInstead ? syncRef : syncRef.Target, 
                            targetSlot
                        });
                    break;
                case IField field:
                    if (_wizardSettings.ReferenceFieldInstead)
                    {
                        GetType().GetMethod("AttachDynRefVar")
                            ?.MakeGenericMethod(typeof(IField<>).MakeGenericType(field.ValueType))
                            .Invoke(this, new object[] { field, targetSlot });
                    }
                    else
                    {
                        GetType().GetMethod("AttachDynValueVar")
                            ?.MakeGenericMethod(
                                field.ValueType
                            ).Invoke(this, new object[] { field, targetSlot });
                    }
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
            dynVar.VariableName.Value = FormatName(_wizardSettings.DynVarNameFormat, value, targetSlot);
            dynVar.OverrideOnLink.Value = _wizardSettings.OverrideOnLink;
            dynVar.Reference.Target = (T)value;
        }
        
        public void AttachDynValueVar<T>(IField field, Slot targetSlot) {
            DynamicValueVariable<T> dynVar = targetSlot.AttachComponent<DynamicValueVariable<T>>();
            dynVar.VariableName.Value = FormatName(_wizardSettings.DynVarNameFormat, field, targetSlot);
            dynVar.OverrideOnLink.Value = _wizardSettings.OverrideOnLink;
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
            dynVar.VariableName.Value = FormatName(_wizardSettings.DynVarNameFormat, element, targetSlot);

            if (_wizardSettings.SetCurrentValueAsDefault)
                dynVar.DefaultTarget.Target = (T)element.Target;

            dynVar.Target.Target = (SyncRef<T>)element;
        }
        
        public void AttachDynValueDriver<T>(IField element, Slot targetSlot) {
            DynamicValueVariableDriver<T> dynVar = targetSlot.AttachComponent<DynamicValueVariableDriver<T>>();
            dynVar.VariableName.Value = FormatName(_wizardSettings.DynVarNameFormat, element, targetSlot);

            if (_wizardSettings.SetCurrentValueAsDefault)
                dynVar.DefaultValue.Value = (T)element.BoxedValue;
            
            dynVar.Target.Target = (IField<T>)element;
        }
        
        bool CreateDynDrivenValueCopy(IWorldElement element, Slot targetSlot) {
            switch (element) {
                case ISyncRef syncRef:
                    GetType().GetMethod("AttachDynDrivenRefCopy")
                        ?.MakeGenericMethod(syncRef.TargetType)
                        .Invoke(this, new object[] { syncRef, targetSlot });
                    break;
                case IField field:
                    GetType().GetMethod("AttachDynDrivenValueCopy")
                        ?.MakeGenericMethod(field.ValueType)
                        .Invoke(this, new object[] { field, targetSlot });
                    break;
                default:
                    return false;
            }
            
            return true;
        } 
        
        public void AttachDynDrivenRefCopy<T>(ISyncRef element, Slot targetSlot) 
            where T : class, IWorldElement
        {
            DynamicReferenceVariableDriver<SyncRef<T>> dynVar = targetSlot.AttachComponent<DynamicReferenceVariableDriver<SyncRef<T>>>();
            dynVar.VariableName.Value = FormatName(_wizardSettings.DynVarNameFormat, element, targetSlot);

            ReferenceCopy<T> refCopy = targetSlot.AttachComponent<ReferenceCopy<T>>();
            dynVar.Target.Value = refCopy.Source.ReferenceID;

            refCopy.Target.Target = (SyncRef<T>)element;
            refCopy.WriteBack.Value = _wizardSettings.WriteBack;
        }
        
        public void AttachDynDrivenValueCopy<T>(IField element, Slot targetSlot) {
            DynamicReferenceVariableDriver<IField<T>> dynVar = targetSlot.AttachComponent<DynamicReferenceVariableDriver<IField<T>>>();
            dynVar.VariableName.Value = FormatName(_wizardSettings.DynVarNameFormat, element, targetSlot);

            ValueCopy<T> valueCopy = targetSlot.AttachComponent<ValueCopy<T>>();
            dynVar.Target.Value = valueCopy.Source.ReferenceID;

            valueCopy.Target.Target = (IField<T>)element;
            valueCopy.WriteBack.Value = _wizardSettings.WriteBack;
        }
        
        void CreateDynVars(IButton button, ButtonEventData eventData) {
            Slot targetSlot = DynVarSlot.Reference.Target;

            if (targetSlot != null || DynVarPlacementMode.Value.Value == 0) {
                int mode = DynVarMode.Value.Value;
                int countProcessed = 0;

                foreach (IWorldElement element in _fields.References) {
                    Slot whereToCreate = targetSlot;

                    if (DynVarPlacementMode.Value.Value == 0) {
                        if (element is Slot slot) {
                            whereToCreate = slot;
                        }
                        else {
                            whereToCreate = element.FindNearestParent<Slot>();
                        }
                    }

                    if (whereToCreate == null)
                    {
                        continue;
                    }

                    if (_wizardSettings.DynamicVariableSpaces) {
                        CreateDynVarSpace(element, whereToCreate);
                    }

                    try
                    {
                        if (mode < 2)
                        {
                            if (mode == 0 && CreateDynField(element, whereToCreate))
                            {
                                countProcessed++;
                            }
                            else
                            {
                                CreateDynVar(element, whereToCreate);
                                countProcessed++;
                            }
                        }
                        else
                        {
                            switch (mode)
                            {
                                case 2 when CreateDynDriver(element, whereToCreate):
                                case 3 when CreateDynDrivenValueCopy(element, whereToCreate):
                                    countProcessed++;
                                    break;
                            }
                        }
                    }
                    catch (ArgumentException e)
                    {
                        NeosMod.Msg(e);
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