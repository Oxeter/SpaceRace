using System.Collections.Generic;
using ModApi.Craft.Parts;
using ModApi.Design;
using ModApi.Ui;
using UnityEngine;
using UI.Xml;
using UnityEngine.Events; 
using UnityEditor;
using System.Linq;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using System.Collections.ObjectModel;
using System;

using System.Xml.Linq;
using Unity.Mathematics;
using Assets.Scripts.SpaceRace.Modifiers;
using Assets.Scripts.SpaceRace.Hardware;
using Assets.Scripts.SpaceRace;
using Assets.Scripts.SpaceRace.Collections;
using Assets.Scripts.Design;
using Assets.Scripts.SpaceRace.Projects;
using Assets.Scripts.Craft;
using ModApi.Craft;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Ui;
using Assets.Scripts.State.Validation;

namespace Assets.Scripts.SpaceRace.Ui
{
    //
    // Summary:
    //     Script for the view panel.
    public class DevelopmentPanelScript : DesignerFlyoutPanelScript
    {
        private VerboseCraftConfiguration _craftConfiguration;
        private ICraftScript _craft => DesignerUi.Designer.CraftScript;
        private SandboxValidator _validator;
        private ICommandPod _commandPod = null;
        private XmlElement _stageListLayout;
        private XmlElement _partListLayout;
        private XmlElement _fopTemplate;
        private XmlElement _stagePanelTemplate;
        private XmlElement _partPanelTemplate;
        private XmlElement _propTemplate;        
        private List<StageDevPanel> stagePanels = new List<StageDevPanel>();
        private List<PartDevPanel> partPanels = new List<PartDevPanel>();
        private PartDevPanel _selected = null;

        private IProgramManager _pm;

        private IHardwareManager _hm => _pm.HM;
        private Dictionary<int, string> _stageNames = new Dictionary<int, string>();
        //
        // Summary:
        //     Initializes the flyout panel.
        //
        // Parameters:
        //   designerUi:
        //     The designer UI script reference.
        public override void Initialize(DesignerUiScript designerUi)
        {
            base.Initialize(designerUi);
            _pm = SRManager.Instance.pm;
            _validator = new SandboxValidator();
            base.Flyout.Opened += OnFlyoutOpened;
            designerUi.Designer.CraftStructureChanged += OnCraftStructureChanged;
            designerUi.Designer.SelectedPartChanged += OnSelectedPartChanged;
            designerUi.Designer.CraftLoaded += OnCraftLoaded;
        }

        //
        // Summary:
        //     Layouts the rebuilt.
        //
        // Parameters:
        //   parseResult:
        //     The parse result.
        public override void LayoutRebuilt(ParseXmlResult parseResult)
        {
            base.LayoutRebuilt(parseResult);
            _stageListLayout = base.xmlLayout.GetElementById("stage-list");
            _partListLayout = base.xmlLayout.GetElementById("part-list");
            _fopTemplate = base.xmlLayout.GetElementById("template-fop-element");
            _stagePanelTemplate = base.xmlLayout.GetElementById("template-stage-panel");
            _partPanelTemplate = base.xmlLayout.GetElementById("template-part-panel");
            _propTemplate = base.xmlLayout.GetElementById("template-prop-element");  
            //base.xmlLayout.GetElementById("craft-name-input").SetAndApplyAttribute("text", Game.Instance.Designer.CraftScript.Data.Name); 
        }

        //
        // Summary:
        //     Called when a new craft is loaded.
        private void OnCraftLoaded()
        {
            _stageNames.Clear();
        }

        //
        // Summary:
        //     Called when the flyout is opened.
        //
        // Parameters:
        //   flyout:
        //     The flyout.
        private void OnFlyoutOpened(IFlyout flyout)
        {
            BuildFlyoutElements();
        }
        private void OnCraftStructureChanged()
        {
            if (Flyout.IsOpen)
            {
                BuildFlyoutElements();
            }
        }
        private void BuildFlyoutElements()
        {
            ICommandPod commandPod =_craft.PrimaryCommandPod;
            if (commandPod!= null) 
            {
                _commandPod = commandPod;
            }
            _craftConfiguration = _pm.GetVerboseCraftConfig(_commandPod);
            GenerateStageUi();
            GeneratePartsUi();
        }       
        public void RenameCraft(string value)
        {
            Game.Instance.Designer.CraftScript.Data.Name = value;
        }
        public void OnIntegrateButtonPressed()
        {
            ICommandPod commandPod = _craft.PrimaryCommandPod;
            if (commandPod!= null) 
            {
                _commandPod = commandPod;
            }
            ModApi.Scripts.State.Validation.ValidationResult vr = _validator.ValidateCraft(_craft, null, false);
            _craftConfiguration = _pm.GetVerboseCraftConfig(_commandPod, vr);
            if (vr.Messages.Where(m => m.MessageType == ModApi.Scripts.State.Validation.ValidationMessageType.Error).Count() >0)
            {
                Game.Instance.UserInterface.CreateMessageDialog(string.Join("\n", vr.Messages.Select(ms => ms.Message)), null, Game.Instance.Designer.DesignerUi.Transform);
                return;
            }
            Game.Instance.UserInterface.CreateListView(new LaunchListViewModel(_pm.Integrations.Values.ToList<ILaunchable>(), _pm.HM.Craft.Values.Where(c => c.Active).ToList<ILaunchable>(), _pm, _craftConfiguration));
            //ModApi.Ui.InputDialogScript script = Game.Instance.UserInterface.CreateInputDialog(Game.Instance.Designer.DesignerUi.Transform);
            //script.MessageText = "Please name this craft";
            //script.InputText = _craft.Data.Name;
            //script.InputPlaceholderText = "Craft name";
            //script.OkayClicked += dialog =>
            //{
            //    dialog.Close();
            //    _pm.NewIntegrationFromDesigner(_craftConfiguration, dialog.InputText);
            //};
        } 

        public void OnNameCraftButtonPressed()
        {
            ModApi.Ui.InputDialogScript script = Game.Instance.UserInterface.CreateInputDialog(Game.Instance.Designer.DesignerUi.Transform);
            script.MessageText = "Please name this craft";
            script.InputText = _craft.Data.Name;
            script.InputPlaceholderText = "Craft name";
            script.OkayClicked += dialog =>
            {
                dialog.Close();
                RenameCraft(dialog.InputText);
                BuildFlyoutElements();  
            };
        }

        public void OnNameButtonPressed()
        {
            List<StageDevPanel> sp = stagePanels.Where(s => s.Selected).ToList();
            if (sp.Count ==0) return;
            RenameStage(sp[0]);
        }

        public void RenameStage(StageDevPanel panel)
        {
            List<StageDevPanel> sp = stagePanels.Where(s => s.Selected).ToList();
            ModApi.Ui.InputDialogScript script = Game.Instance.UserInterface.CreateInputDialog(Game.Instance.Designer.DesignerUi.Transform);
            script.MessageText = "Name this stage";
            script.InputText = panel.Stage.Name;
            script.InputPlaceholderText = "Stage name";
            script.OkayClicked += dialog =>
            {
                dialog.Close();
                _stageNames[panel.StageNumber] = dialog.InputText;
                if (sp.Contains(panel) && panel != sp.Last()) 
                {
                    RenameStage(sp[sp.IndexOf(panel)+1]);
                }
                else GenerateStageUi();
            };
        }

        public void OnPreviousStagesButtonPressed()
        {
            Game.Instance.UserInterface.CreateListView(new StagesViewModel(_pm.HM.Stages.Values.ToList(), null, _pm));
        }

        public void OnLoadButtonPressed()
        {
            PreviousCraftViewModel model = new PreviousCraftViewModel(_hm.DB.CraftConfigs.Where(config => config.Active).ToList(),LoadPreviousCraft,_pm);
            model.PrimaryButtonText = "Load";
            Game.Instance.UserInterface.CreateListView(model);
        }
        public void LoadPreviousCraft(Assets.Scripts.SpaceRace.Hardware.SRCraftConfig config)
        {
            XElement craft = _pm.Designs.GetCraftDesign("Integration"+config.Id.ToString());
            DesignerUi.Designer.CraftLoader.LoadCraftInteractive(craft, true, true, config.Name+" Loaded",null, null);
        }

        private void GeneratePartsUi()
        {
            foreach (PartDevPanel component in partPanels)
            {
                _partListLayout.RemoveChildElement(component.Panel, true);
            }
            partPanels.Clear();
            foreach (IPartScript script in _craftConfiguration.Parts.Keys)
            {
                if (_craftConfiguration.Parts[script].Id ==0 && _craftConfiguration.Parts[script].DesignerCategory != null) 
                {
                    PartDevPanel match = partPanels.Find(p => XNode.DeepEquals(p.SRPart.Data, _craftConfiguration.Parts[script].Data));
                    if (match == null) GeneratePartsPanel(script);
                    else 
                    {
                        match.Occurences += 1;
                        match.Panel.GetElementByInternalId("occurences").SetText("x "+match.Occurences.ToString());
                    }
                }
            }
            int selected = Game.Instance.Designer.SelectedPart?.Data.Id ?? 0;
            PartDevPanel panel = partPanels.Find(p => p.PartId == selected);
            if (panel != null)
            {
                panel.Selected = true;
                panel.Panel.GetElementByInternalId("part-button").AddClass("selected");
            }
        }

        private void GeneratePartsPanel(IPartScript script)
        {
            XmlElement listItem = Instantiate(_partPanelTemplate);
            XmlElement component = listItem.GetComponent<XmlElement>();

            component.Initialise(_stageListLayout.xmlLayoutInstance, (RectTransform)listItem.transform, _stagePanelTemplate.tagHandler);
            component.RemoveAttribute("id");
            component.GetElementByInternalId("part-button").SetAndApplyAttribute("onClick", "OnPartPressed(" + script.Data.Id.ToString() +")");
            component.GetElementByInternalId("part-name").SetText(_craftConfiguration.Parts[script].Name);


            partPanels.Add(new PartDevPanel{
                Part = script,
                PartId = script.Data.Id,
                Panel = component,
                SRPart = _craftConfiguration.Parts[script],
                Occurences = 1
            });

            _partListLayout.AddChildElement(component);
            XmlElement propList = component.GetElementByInternalId("property-list");
            Dictionary<string, string> properties = _pm.GetDevPanelProperties(script.Data, _craftConfiguration.Parts[script].PrimaryMod);
            foreach (string key in properties.Keys)
            {
                XmlElement propItem = Instantiate(_propTemplate);
                XmlElement propComponent = propItem.GetComponent<XmlElement>();
//
                propComponent.Initialise(propList.xmlLayoutInstance, (RectTransform)propItem.transform, _propTemplate.tagHandler);
                propComponent.RemoveAttribute("id");
                propComponent.ApplyAttributes();
                propList.AddChildElement(propComponent);

                propComponent.GetElementByInternalId("label").SetText(key);
                propComponent.GetElementByInternalId("value").SetText(properties[key]);
            }
        }

        //
        // Summary:
        //     Called when the selected part has changed.
        //
        // Parameters:
        //   oldPart:
        //     The old part.
        //
        //   newPart:
        //     The new part.
        private void OnSelectedPartChanged(IPartScript oldPart, IPartScript newPart)
        {
            ICommandPod commandPod = Game.Instance.Designer.CraftScript.PrimaryCommandPod;
            if (commandPod!= null) 
            {
                _commandPod = commandPod;
            }
            if (Flyout.IsOpen)
            {
                PartDevPanel panel = partPanels.Find(p => p.PartId == newPart?.Data.Id);
                if (panel != null){
                    DeselectAllParts();
                    panel.Selected = true;
                    panel.Panel.GetElementByInternalId("part-button").AddClass("selected");
                }
            }
        }



        private void GenerateStageUi()
        {
            foreach (StageDevPanel component in stagePanels)
            {
                _stageListLayout.RemoveChildElement(component.Panel, true);
            }
            stagePanels.Clear();
            int i = 0;
            foreach (SRStage stage in _craftConfiguration.Stages.Keys)
            {

                GenerateStagePanel(stage,i);
                i ++;
            }
        }
        private void GenerateStagePanel(SRStage stage, int i)
        {
            XmlElement listItem = Instantiate(_stagePanelTemplate);
            XmlElement component = listItem.GetComponent<XmlElement>();

            component.Initialise(_stageListLayout.xmlLayoutInstance, (RectTransform)listItem.transform, _stagePanelTemplate.tagHandler);
            component.RemoveAttribute("id");
            component.GetElementByInternalId("stage-button").SetAndApplyAttribute("onClick", "OnStagePressed(" + i.ToString() +")");
            StageDevPanel stagePanel = new StageDevPanel{
                StageNumber = i,
                Panel = component,
                Stage = stage,
            };
            if (!stage.Developed && _stageNames.Keys.Contains(i)) stagePanel.Stage.Name = _stageNames[i];
            component.GetElementByInternalId("stage-name").SetText(stagePanel.Stage.Name);
            if (stagePanel.Stage.Id != 0)
            {
                component.GetElementByInternalId("stage-button").AddClass("btn-success");
                stagePanel.Developed = true;
            }
            _stageListLayout.AddChildElement(component);
            XmlElement fopList = component.GetElementByInternalId("fop-list");

            stagePanels.Add(stagePanel);
            if (stagePanel.Stage.FamiliesOverride != null)
            {
                foreach ((SRPartFamily, int) pair in stagePanel.Stage.FamiliesOverride)
                {
                    XmlElement fopItem = Instantiate(_fopTemplate);
                    XmlElement fopComponent = fopItem.GetComponent<XmlElement>();

                    fopComponent.Initialise(fopList.xmlLayoutInstance, (RectTransform)fopItem.transform, _fopTemplate.tagHandler);
                    fopComponent.RemoveAttribute("id");
                    fopComponent.ApplyAttributes();
                    fopList.AddChildElement(fopComponent);

                    string label = pair.Item1.Name;
                    if (label == string.Empty) label = "Undeveloped Part";
                    fopComponent.GetElementByInternalId("label").SetText(label);
                    fopComponent.GetElementByInternalId("value").SetText("x "+pair.Item2.ToString());
                }
            }
            else 
            {
                foreach (HardwareOccurencePair hop in stagePanel.Stage.PartFamilies)
                {
                    XmlElement fopItem = Instantiate(_fopTemplate);
                    XmlElement fopComponent = fopItem.GetComponent<XmlElement>();

                    fopComponent.Initialise(fopList.xmlLayoutInstance, (RectTransform)fopItem.transform, _fopTemplate.tagHandler);
                    fopComponent.RemoveAttribute("id");
                    fopComponent.ApplyAttributes();
                    fopList.AddChildElement(fopComponent);

                    string label = _pm.HM.Families[hop.Id].Name;
                    if (label == string.Empty) label = "Undeveloped Part";
                    fopComponent.GetElementByInternalId("label").SetText(label);
                    fopComponent.GetElementByInternalId("value").SetText("x "+hop.Occurences.ToString());
                }
            }
        }
        public void OnStagePressed(int i)
        {
            if (stagePanels[i].Selected) DeselectAllStages();
            else if ((i != stagePanels.Count -1 && stagePanels[i+1].Selected) || (i != 0 && stagePanels[i-1].Selected))
            {
                stagePanels[i].Selected = true;
                stagePanels[i].Panel.GetElementByInternalId("stage-button").AddClass("selected");
            }
            else 
            {
                DeselectAllStages();
                stagePanels[i].Selected = true;
                stagePanels[i].Panel.GetElementByInternalId("stage-button").AddClass("selected");
            }
        }
        public void OnDevelopStagesButtonPressed()
        {
            List<StageDevPanel> sp = stagePanels.Where(s => s.Selected).ToList();
            if (sp.Where(s => s.Developed).Any())
            {
                Game.Instance.Designer.DesignerUi.ShowMessage("Stage already developed or in development");
                return;
            }
            if (sp.Where(s => !s.ReadyToDevelop()).Any())
            {
                Game.Instance.Designer.DesignerUi.ShowMessage("Begin developing the parts on this stage first");
                return;
            }
            //List<int> stagenumbers = sp.Select(s => s.StageNumber).ToList();
            string stagesDescription = (sp.Count > 1)? 
                string.Format("{0} Stages {1:D}-{2:D}", Game.Instance.Designer.CraftScript.Data.Name, sp[0].StageNumber+1, sp.Last().StageNumber+1): 
                sp[0].Stage.Name;
            List<SRStage> stages = sp.Select(s => s.Stage).ToList();
            StageDevContractListViewModel viewModel = new StageDevContractListViewModel(
                _pm.GetStageBids(stages),
                offer => 
                {
                    //for (int i=0; i< _craftConfiguration.Count; i++ ) Debug.Log("Source AfterListViewModel "+_craftConfiguration[i].ToString());
                    //foreach (SRStage stage in stages) Debug.Log("AfterListViewModel "+stage.ToString());
                    foreach (StageDevPanel sdp in sp)
                    {
                        sdp.Stage.PartFamilies.Clear();
                        foreach((SRPartFamily, int) pair in sdp.Stage.FamiliesOverride)
                        {
                            SRPartFamily fam = pair.Item1;
                            if (pair.Item1.Id == 0) 
                            {
                                if (pair.Item1.DesignerCategory == null) pair.Item1.Id = _pm.HM.AddStandaloneFamily(pair.Item1);
                                else throw new ArgumentException("Stage contains undeveloped parts that must be developed. This should have been caught earlier.");
                            }
                            sdp.Stage.PartFamilies.AddPair(pair.Item1.Id, pair.Item2);
                        }
                        sdp.Stage.FamiliesOverride = null;
                    }
                    _pm.NewStageDevelopment(stages, stagesDescription, offer);
                    BuildFlyoutElements();
                }
            );
            Game.Instance.UserInterface.CreateListView(viewModel);
        }

        public void DeselectAllStages()
        {
            foreach (StageDevPanel sdp in stagePanels)
            {
                sdp.Selected = false;
                sdp.Panel.GetElementByInternalId("stage-button").RemoveClass("selected");
            }
        }
        public void OnPartPressed(int i)
        {
            PartDevPanel panel = partPanels.Find(p => p.PartId == i);
            if (panel != null){
                if (!panel.Selected)
                {
                    Game.Instance.Designer.SelectPart(panel.Part,null,false); 
                    //onselectedpartchanged will do the rest                
                }
            }
        }
        public void DeselectAllParts()
        {
            foreach (PartDevPanel pdp in partPanels)
            {
                pdp.Selected = false;
                pdp.Panel.GetElementByInternalId("part-button").RemoveClass("selected");
            }
        }
        public void OnDevelopPartButtonPressed()
        {
            Game.Instance.Designer.DesignerUi.ShowMessage("click");
            PartData part = partPanels.Find(f => f.Selected).Part.Data;
            PartDevContractListViewModel viewModel = new PartDevContractListViewModel(
                _pm.GetPartBids(part),
                offer => 
                {
                    ModApi.Ui.InputDialogScript namescript = Game.Instance.UserInterface.CreateInputDialog();
                    namescript.MessageText = "Rename this part?";
                    namescript.InputText = offer.Name;
                    namescript.OkayClicked += (s) =>
                    {
                        offer.Name = s.InputText;
                        s.Close();
                        ModApi.Ui.InputDialogScript descscript = Game.Instance.UserInterface.CreateInputDialog();
                        descscript.MessageText = "Edit designer description?";
                        descscript.InputText = offer.DesignerDescription;
                        descscript.OkayClicked += (s) =>
                        {
                            offer.DesignerDescription = s.InputText;
                            s.Close();
                            _pm.NewPartDevelopment(part, offer);
                            BuildFlyoutElements();  
                        };
                    };
                }
            );
            Game.Instance.UserInterface.CreateListView(viewModel);
        }
    }
    public class StageDevPanel
    {
        public int StageNumber;
        public SRStage Stage;
        public XmlElement Panel;
        public bool Selected;
        public bool ReadyToDevelop()
        {
            return !Stage.FamiliesOverride.Any(pair => pair.Item1.Id <=0 && pair.Item1.DesignerCategory != null);
        }
        public bool Developed;
    }
    public class PartDevPanel
    {
        public XmlElement Panel;
        public SRPart SRPart;
        public int Occurences;
        public int PartId;
        public bool Selected;
        public IPartScript Part;
    }
}