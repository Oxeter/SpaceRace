using UI.Xml;
namespace Assets.Scripts.SpaceRace.Projects
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using System.IO;
    using UnityEngine;

    using ModApi.Craft.Parts;
    using ModApi.Common.Extensions;
    using ModApi.Flight;
    using ModApi.Scenes.Events;
    using ModApi.Flight.Events;
    using Assets.Scripts.SpaceRace.Hardware;
    using ModApi.Ui.Inspector;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.Modifiers;
    using Assets.Scripts.SpaceRace.Formulas;
    using ModApi.Scenes.Parameters;
    using Assets.Scripts.Career.Contracts.Requirements;
    using ModApi.Craft;
    using Assets.Scripts.Design.Staging;
    using ModApi.Flight.Sim;
    using ModApi.Planet;
    using ModApi.Design;
    using ModApi.Math;
    using Assets.Scripts.SpaceRace.Ui;
    using Assets.Scripts.State;
    using Assets.Scripts.SpaceRace.UI;
    using ModApi.Ui;
    using ModApi.Settings.Core;
    using Assets.Scripts.SpaceRace.History;
    using ModApi.State;
    using Unity.Mathematics;
    using Assets.Scripts.Flight.Sim;
    using Assets.Scripts.Craft.Fuel;
    using Assets.Scripts.Flight;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using Assets.Scripts.Career.Contracts;
    using Assets.Scripts.Craft;
    using ModApi.Craft.Parts.Modifiers;
    using HarmonyLib;
    using Assets.Scripts.Craft.Parts.Modifiers.Eva;
    using Assets.Scripts.SpaceRace.Program;
    using ModApi.Planet;
    using Assets.Scripts.Design;

    public class ProgramManager : MonoBehaviour, IProgramManager
    {   
        private const double _updateDelay = 3600.0;
        public CareerState Career => _gs.Career;
        public long CostPerDay => ActiveChildren.Values.Sum(ch => ch.Completed? 0 : ch.PricePerDay);
        public string BalancePerDayString => CostPerDay <= 0? "+" + Units.GetMoneyString(-CostPerDay): "-" + Units.GetMoneyString(CostPerDay);
        private GameState _gs;
        private ProgramData _data;
        private IHardwareManager _hm;
        private ICostCalculator _costCalculator;
        public GameState GameState {get {return _gs;} set {_gs=value;}}
        private HistoryManager _hist;
        public HistoryManager History {get {return _hist;}}
        public SpaceCenterManager SpaceCenter {get; private set;}
        private GameObject _spaceCenterUiGameObject;
        private SpaceCenterUiController _spaceCenterUi;
        public SpaceCenterUiController SpaceCenterUi {get {return _spaceCenterUi;} set{_spaceCenterUi = value;}}
        public SpaceCenterLocation CurrentSpaceCenterLocation;
        public int SpawningSpaceCenter {get; set;} = 0;
        private List<string> _occupiedLauchLocations = new List<string>();
        public int LaunchingId {get; set;}= 0;
        public SRCraftConfig LastCraftConfig {get; set;}
        public ICostCalculator CostCalculator {get{return _costCalculator;}}
        public CraftDesigns Designs {get; private set;}
        public IHardwareManager HM => _hm;
        public ProgramData Data {get {return _data;}}
        public SpaceRaceContractsScript SRContracts;

        private IFlightScene fs;
        public bool ContractsChanged {get; private set;} = false;
        private bool _warp = false;
        public bool IsWarp {get {return _warp;}}
        private bool _editingStaff = false;
        public bool HighlightLaunchButton => Integrations.Values.Any(i => i.HighlightLauchButton);
        private XmlSerializer _programSerializer;
        private XmlSerializer _hardwareSerializer;
        private XmlSerializer _historySerializer;
        private XmlSerializer _staticEventSerializer;
        private XmlSerializer _fundingSerializer;
        private XmlSerializer _spaceRaceContractsSerializer;
        private XmlSerializer _structuresSerializer;
        private string _programFileName => _gs.RootPath + "/Program.xml"; 
        private string _hardwareFileName => _gs.RootPath + "/Hardware.xml"; 
        private string _careerHardwareFileName => _gs.Career.ResourcesAbsolutePath + "/Hardware.xml"; 
        private string _historyFileName => _gs.RootPath + "/PastEvents.xml"; 
        private string _staticEventsFileName => _gs.Career.ResourcesAbsolutePath + "/StaticEvents.xml";
        private string _fundingFileName => _gs.Career.ResourcesAbsolutePath + "/Funding.xml";
        private string _defaultProgramFileName => _gs.Career.ResourcesAbsolutePath + "/Program.xml";
        private string _spaceCentersFileName => _gs.Career.ResourcesAbsolutePath + "/SpaceCenterLocations.xml";
        private string _structuresFileName => _gs.Career.ResourcesAbsolutePath + "/Structures.xml";
        private string _spaceCenterCraftFile => "__space-center__";
        private IInspectorPanel _panel = null;
        private InspectorModel _inspectorModel;
        private IconButtonRowModel _iconButtonRowModel = new IconButtonRowModel();
        private TextModel _ttnc;
        public static ProgramManager Instance {get; private set;}
        public int AvailableTechnicians
        {
            get{return _data.AvailableTechnicians;} 
            set{ _data.AvailableTechnicians= value;}
        }
        public BudgetScript Budget {get; private set;}
        public Dictionary<int,PartDevelopmentScript> PartDevelopments {get; private set;} = new Dictionary<int, PartDevelopmentScript>();
        public Dictionary<int,StageDevelopmentScript> StageDevelopments {get; private set;} = new Dictionary<int, StageDevelopmentScript>();
        public Dictionary<int,IntegrationScript> Integrations {get; private set;} = new Dictionary<int, IntegrationScript>();
        public Dictionary<int,ExternalIntegrationScript> ExternalIntegrations {get; private set;} = new Dictionary<int, ExternalIntegrationScript>();
        public Dictionary<int,ContractorScript> Contractors {get; private set;} = new Dictionary<int, ContractorScript>();
        public Dictionary<int,ConstructionScript> Construction {get; private set;} = new Dictionary<int, ConstructionScript>();
        public Dictionary<int,FundingScript> Funding {get; private set;} = new Dictionary<int, FundingScript>();
        public Dictionary<string,FundingScript> FundingByContractId {get; private set;} = new Dictionary<string, FundingScript>();
        public Dictionary<int,IProgramChild> ActiveChildren {get; private set;} = new Dictionary<int, IProgramChild>();
        public Dictionary<int,GroupModel> InspectorGroups {get; private set;} = new Dictionary<int, GroupModel>();
        public Dictionary<string, ISRPartMod> PartMods {get; private set;} = new Dictionary<string, ISRPartMod>();
        /// <summary>
        /// Called after the program manager does not find saved data and loads the career starting data.
        /// </summary>
        public event SpaceProgramDelgate NewSpaceProgram;
  
        void Awake()
        {
            Instance = this;
            _programSerializer = new XmlSerializer(typeof(ProgramDB));
            _hardwareSerializer = new XmlSerializer(typeof(HardwareDB));
            _historySerializer = new XmlSerializer(typeof(List<HistoricalEvent>));
            _staticEventSerializer = new XmlSerializer(typeof(List<StaticHistoricalEvent>));
            _fundingSerializer = new XmlSerializer(typeof(List<FundingData>));
            _spaceRaceContractsSerializer = new XmlSerializer(typeof(List<SpaceRaceContractsData>));
            _structuresSerializer = new XmlSerializer(typeof(List<SpaceCenterStructure>));
            _costCalculator = new CostCalculator(this);
        }
        // Start is called before the first frame update
        void Start()
        {
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            if (Game.IsCareer)
            {
                Load();
                UpdateAllRates(true);
            }
        }
        public void RegisterPartModData(ISRPartMod data)
        {
            if (data.GetType().IsSubclassOf(typeof(PartModifierData)))
            {
                PartMods[data.TypeId]=data;
                Debug.Log("Spacerace modifier "+data.TypeId+" registered");
            }
            else Debug.LogWarning("Spacerace modifier "+data.TypeId+" is not PartModifierData");
        }

        void Update()
        {   
            if (!Game.InFlightScene) return;
            if (Game.Instance.GameState.Mode != ModApi.State.GameStateMode.Career) return;
            if (Game.Instance.GameState.Type != ModApi.State.GameStateType.Default) return;
            if (fs.TimeManager.CurrentMode.TimeMultiplier > 10.0 && Career.Money < 0L && CostPerDay > 0L)
            {
                fs.FlightSceneUI.ShowMessage("You have negative cashflow and no reserves to draw upon.  Attain positive cashlow before engaging timewarp.");
                SetWarp(false);
            }         
            if (fs.FlightState.Time >= _data.LastUpdateTime + Math.Min(_data.TimeUntilNextCompletion, _updateDelay))
            {
                UpdateProgress();
                _hist.FireEventsByDate(fs.FlightState.Time);
                if (_spaceCenterUi != null) _spaceCenterUi.UpdateStats();
            }
            if (_data.TimeUntilNextCompletion <= 0.0) 
            {
                Debug.Log(string.Format("Time until next change is {0:F0}, Recalculating",_data.TimeUntilNextCompletion));
                UpdateAllRates(false);
            }
            if (_warp)
            {
                if (_data.TimeUntilNextCompletion == Double.PositiveInfinity)
                {
                    fs.FlightSceneUI.ShowMessage("No upcoming completion to warp to.");
                    SetWarp(false);
                }
                if (_data.TimeUntilNextCompletion < 5.0 * fs.TimeManager.DeltaTime)
                {
                    fs.TimeManager.DecreaseTimeMultiplier();
                } 
            }
        }
        private void UpdateProgress()
        {
            double now = Game.InFlightScene? fs.FlightState.Time : Game.Instance.GameState.GetCurrentTime();
            double dT = now - _data.LastUpdateTime;
            _data.TimeUntilNextCompletion -= dT;
            _data.LastUpdateTime = now;
            foreach (IProgramChild child in ActiveChildren.Values.Where(child=>!child.Completed)) child.UpdateProgress(dT);
            Career.SpendMoney((long)(CostPerDay * dT * Formulas.SRFormulas.DaysPerSecond));

        }
        public int AddContractor(ContractorData contractorData)
        {
            int i = _data.NextId;
            _data.NextId += 1;
            contractorData.Id = i;
            if (contractorData.Temporary) Contractors[i] = new TemporaryContractorScript(contractorData, this);
            else Contractors[i] = new ContractorScript(contractorData, this);
            if (Contractors[i].Active)
            {   
                ActiveChildren[i] = Contractors[i];
                if (_panel != null)
                {
                    _panel.Model.AddGroup(ActiveChildren[i].GetGroupModel(), i);
                    _panel.RebuildModelElements();
                }
            }
            Debug.Log($"Contractor {contractorData.Name} added");
            return i;

        }
        public void AddTypeToContractors(string type, Predicate<ContractorScript> predicate)
        {
            IEnumerable<ContractorScript> list = Contractors.Values.Where(con => predicate(con));
            if (list.Count() == 0) Debug.Log("No contractor satisfies this condition.");
            foreach (ContractorScript contractor in list)
            {
                contractor.Data.HardwareTypes.Add(type);
                Debug.Log($"Contractor {contractor.Name} now produces hardware of type {type}.");
            }
        }
        /// <summary>
        /// Gets a list of required crew
        /// </summary>
        /// <param name="contract">the contract</param>
        /// <returns>List of payload IDs from payload requirements with partcount requirements as parents</returns>
        public PropertyOccurenceList RequiredCrew(Contract contract)
        {
            PropertyOccurenceList pol = new PropertyOccurenceList();
            foreach (ContractRequirement req in contract.Requirements)
            {
                if (req is ISupportsPayload isp && req is PayloadRequirement pr)
                {
                    if (!string.IsNullOrEmpty(isp.PayloadId) && Traverse.Create(pr).Field("_mode").GetValue().ToString()  == "Recover")
                    {
                        pol.IncreaseTo(isp.PayloadId, isp.NumPayloadParts);
                    }
                }
            }
            return pol;
        }
        public void OnContractAccepted(Career.Contracts.Contract contract)
        {
            if (Game.InFlightScene)
            {
                ContractsChanged = true;
                Career.GenerateXml();
                Career.OnFlightStart(FlightSceneScript.Instance, false);
            }
            _hist.FireEventsByContractsAccepted(contract);
            foreach (string locationname in contract.UnlockLocations)
            {
                NewLaunchpadConstruction(contract, Game.Instance.GameState.LaunchLocations.First(loc => loc.Name == locationname));
            }
            if (FundingByContractId.TryGetValue(contract.Id, out FundingScript script))
            {

                AddContractFunding(contract, script);
            }
        }
        public void AddContractFunding(Career.Contracts.Contract contract, FundingScript script)
        {
            script.AddContract(contract);
            if (script.Active)
            {
                ActiveChildren[script.Id] = script;
                ReplaceInspectorGroup(script.Id);
            }
        }
        public void OnContractCancelled(Career.Contracts.Contract contract)
        {
            Debug.Log(contract.Id + " Cancelled");
            ContractsChanged = true;
            if (contract.UnlockLocations != null)
            {
                foreach (int i in Construction.Keys)
                if (Construction[i].Data.ContractId == contract.Id)
                {
                    EndProject(i);
                }
            }
            if (FundingByContractId.TryGetValue(contract.Id, out FundingScript script))
            {
                script.CancelContract(contract);
                if (script.Active) 
                {
                    ReplaceInspectorGroup(script.Id);
                    ReplaceInspectorGroup(0);
                }
                else 
                {
                    EndProject(script.Id);
                }
                
            }
            if (_spaceCenterUi != null) _spaceCenterUi.UpdateStats();
            _data.ContractCrewsProvided.Remove(contract.ContractNumber);
        }

        public void OnContractCompleted(Career.Contracts.Contract contract)
        {
            ContractsChanged = true;
            _data.ContractCrewsProvided.Remove(contract.ContractNumber);
            SRContracts.OnContractCompleted(contract);
            SpaceCenter.OnContractCompleted(contract);
            Budget.UpdateRates(false);
            if (FundingByContractId.TryGetValue(contract.Id, out FundingScript script))
            {
                Career.SpendMoney(contract.RewardMoney);
                script.CompleteContract(contract);
                if (script.Active) 
                {
                    ReplaceInspectorGroup(script.Id);
                }
                else 
                {
                    EndProject(script.Id);
                }
            }
            ReplaceInspectorGroup(0);
            if (_spaceCenterUi != null) _spaceCenterUi.UpdateStats();
            History.OnContractCompleted(contract); // this still hits errors for missing images so should go last until fixed
        }
        
        public void ActivateContractor(int i)
        {
            if (Contractors[i].Active) return;
            Contractors[i].Data.Active = true;
            ActiveChildren[i] = Contractors[i];
            InspectorGroups[i] = _inspectorModel.AddGroup(ActiveChildren[i].GetGroupModel());
            _panel.RebuildModelElements();
        }

        public IContractorScript Contractor(int i)
        {
            return Contractors[i];
        }
        public IStageDevelopmentScript StageDevelopment(int i)
        {
            return StageDevelopments[i];
        }
        public IHardwareDevelopmentScript PartDevelopment(int i)
        {
            return PartDevelopments[i];
        }
        public IIntegrationScript Integration(int i)
        {
            return Integrations[i];
        }
        public IProgramChild Child(int i)
        {
            return ActiveChildren[i];
        }
        public ISRPartMod GetPrimarySpaceRaceMod(PartData part)
        {
            List<PartModifierData> SRMods = part.Modifiers.Where(mod => PartMods.Keys.Contains(mod.TypeId)).ToList();
            ISRPartMod selected = null;
            int priority = -10000;
            foreach (PartModifierData mod in SRMods)
            {
                ISRPartMod modinterface = mod as ISRPartMod;
                if (modinterface.SRPriority > priority)
                {
                    priority=modinterface.SRPriority;
                    selected=modinterface;
                }
            }
            return selected;
        }
        public string GetPrimarySpaceRaceModName(PartData part)
        {
            List<PartModifierData> SRMods = part.Modifiers.Where(mod => PartMods.Keys.Contains(mod.TypeId)).ToList();
            PartModifierData selected = null;
            int priority = -10000;
            foreach (PartModifierData mod in SRMods)
            {
                ISRPartMod modinterface = mod as ISRPartMod;
                if (modinterface.SRPriority > priority)
                {
                    priority=modinterface.SRPriority;
                    selected=mod;
                }
            }
            return selected?.TypeId;
        }

        public string GetPrimarySpaceRaceModName(XElement partXml)
        {
            string selected = null;
            int priority = -10000;
            foreach (XElement element in partXml.Elements())
            {
                if (PartMods.TryGetValue(element.Name.LocalName, out ISRPartMod value))
                {
                    if (value.SRPriority > priority)
                    {
                        selected = element.Name.LocalName;
                        priority = value.SRPriority;
                    }
                }
            }
            return selected;
        }
        public XElement GetData(PartData part, string primanymodname)
        {
            XElement partXml = part.GenerateXml(null, false);
            return GetData(partXml, primanymodname);
        }
        public XElement GetData(XElement partXml, string primanymodname)
        {
            if (PartMods.TryGetValue(primanymodname, out ISRPartMod primarysrmod))
            {
                XElement partdata = new XElement("Part");
                foreach (string modname in primarysrmod.PartInfo.Keys)
                {
                    XElement newmodxml = new XElement(modname);
                    XElement partmodxml = partXml.Element(modname);
                    foreach (string attr in primarysrmod.PartInfo[modname])
                    {
                        try
                        {
                            List<string> value = new List<string>(){partmodxml.GetStringAttributeOrNullIfEmpty(attr)};
                            // is the list really necessary?  SetAttribute does not want to find the method in the base class
                            newmodxml.SetAttribute(attr, value);
                            //Debug.Log(string.Format("Copied {0} in {1}", attr, modname));
                        }
                        catch
                        {
                            throw new ArgumentException("Could not find attribute "+attr+ " in element " +modname);
                        }
                    }
                    partdata.Add(newmodxml);
                }
                //Debug.Log("Produced partdata with elements: " + partdata.Descendants().Count().ToString());
                return partdata;
            }
            else return null;
        }

        public XElement GetFamData(PartData part, string primanymodname)
        {
            return GetFamData(part.GenerateXml(null, false), primanymodname);
        }

        public XElement GetFamData(XElement partxml, string primanymodname)
        {
            if (PartMods.TryGetValue(primanymodname, out ISRPartMod primarysrmod))
            {
                XElement famdata = new XElement("Part");
                foreach (string modname in primarysrmod.FamilyInfo.Keys)
                {
                    XElement newmodxml = new XElement(modname);
                    XElement partmodxml = partxml.Element(modname);
                    foreach (string attr in primarysrmod.FamilyInfo[modname])
                    {
                        try
                        {
                            List<string> value = new List<string>(){partmodxml.GetStringAttributeOrNullIfEmpty(attr)};
                            // is the list really necessary?  SetAttribute does not want to find the method in the base class
                            newmodxml.SetAttribute(attr, value);
                        }
                        catch
                        {
                            throw new ArgumentException("Could not find attribute "+attr+ " in element " +modname);
                        }
                        
                    }
                    famdata.Add(newmodxml);
                }
                //Debug.Log("Produced famdata with elements: " + famdata.Descendants().Count().ToString());
                return famdata;
            }
            else return null;
        }
        public double GetFamilyStageCost(PartData part, string primarysrmod)
        {
            if (PartMods.TryGetValue(primarysrmod, out ISRPartMod mod))
            {
                return mod.FamilyStageCost(part);
            }
            throw new ArgumentException("No partmod registered with name " +primarysrmod);
        }
        /// <summary>
        /// Generates a verbose craft configuration from the current craft in the designer. 
        /// Used for determining which parts/stages need to be developed and to generate a craft config for integration.
        /// Logs any undeveloped stages/parts to a validation result.
        /// </summary>
        /// <param name="commandPod">The commandpod of the craft (stages are calculated from here)</param>
        /// <param name="vr">A validation result, null if none</param>
        /// <returns>The verbose craft configuration. </returns>
        /// <exception cref="NullReferenceException"></exception>
        public VerboseCraftConfiguration GetVerboseCraftConfig(ICommandPod commandPod, ModApi.Scripts.State.Validation.ValidationResult vr = null)
        {
            if (commandPod == null) throw new NullReferenceException("Craft does not have a primary command pod.");
            VerboseCraftConfiguration config = new VerboseCraftConfiguration();
            StageCalculator stageCalculator = new StageCalculator(commandPod);
            //Debug.Log("Declared Stage Calculator");
            StagingData stagingData = stageCalculator.GetStages();
            int n = stagingData.Stages.Count;
            List<PartConnection> list = new List<PartConnection>();
            PartGraph partGraph = new PartGraph(commandPod.Part, list);
            List<PartData> currentParts = new List<PartData>(partGraph.Parts);
            FamilyOccurenceList fol = new FamilyOccurenceList();
            TechList famtech = new TechList();
            Dictionary <int, long> priceList = new Dictionary <int, long>();
            string primarymodname = string.Empty;
            bool flightStage = false;
            for (int i = 0; i <= n; i++)
            {
                if (i < n)
                {
                    ActivationStage activationStage = stagingData.Stages[i];
                    foreach (PartData part in activationStage.Parts)
                    {
                        if (part.Config.StageActivationType == StageActivationType.Detacher)
                        {
                            foreach (PartConnection partConnection in part.PartConnections)
                            {
                                list.Add(partConnection);
                            }
                        }
                        else
                        {
                            if (part.Config.StageActivationType != StageActivationType.Fairing)
                            {
                                continue;
                            }

                            foreach (PartConnection partConnection2 in part.PartConnections)
                            {
                                list.Add(partConnection2);
                            }
                        }
                    }
                    partGraph = new PartGraph(commandPod.Part, list);
                }
                if (i < n)
                {
                    currentParts = currentParts.Except(partGraph.Parts).ToList();
                }

                //Debug.Log("Stage" + i.ToString() + "part count = " + currentParts.Count.ToString());
                Dictionary<int, SRPartFamily> currentFamilies = new Dictionary<int, SRPartFamily>();
                fol = new FamilyOccurenceList();
                TechList techlist = new TechList();
                foreach (PartData part in currentParts)
                {
                    if (part.GetModifier<EvaData>() != null)
                    {
                        config.Crew.Add(part.GetModifier<EvaData>().CrewMember);
                        vr?.AddMessage("crew", "Crew are added at launch, not in the designer.", part);
                    }
                    config.MassWet += part.Mass;
                    config.MassDry += part.Mass;
                    List<FuelTankData> fuelTanks = new List<FuelTankData>();
                    part.GetModifiers<FuelTankData>(fuelTanks);
                    foreach (FuelTankData data in fuelTanks)
                    {
                        config.MassDry -= data.MassWet - data.MassDry;
                        if (config.Fuels.TryGetValue(data.FuelType, out double value))
                        {
                            config.Fuels[data.FuelType] += data.Fuel;
                        }
                        else
                        {
                            config.Fuels[data.FuelType] = data.Fuel;
                        }
                    }
                    primarymodname = GetPrimarySpaceRaceModName(part);
                    if (primarymodname != null)
                    {
                        //Debug.Log("Found part with primarymod "+primarymodname);
                        XElement partxml = part.GenerateXml(null, false);
                        XElement famdata = GetFamData(partxml, primarymodname);
                        SRPartFamily fammatch = _hm.Get<SRPartFamily>(fam => XNode.DeepEquals(famdata, fam.Data)) ?? currentFamilies.Values.FirstOrDefault(fam => XNode.DeepEquals(famdata, fam.Data));
                        if (fammatch != null) 
                        {
                            currentFamilies[part.Id] = fammatch;
                            if (fol.AddPair(new HardwareOccurencePair(fammatch.Id, 1)))
                            techlist.Merge(fammatch.Technologies);
                        }
                        else 
                        {
                            currentFamilies[part.Id] = new SRPartFamily()
                            {
                                Id = 0,
                                PrimaryMod = primarymodname,
                                Name = PartMods[primarymodname].UndevelopedFamilyName(part),
                                Data = famdata,
                                DesignerCategory = PartMods[primarymodname].DesignerCategory,
                                Technologies = PartMods[primarymodname].Technologies(part),
                                StageProduction = PartMods[primarymodname].FamilyStageCost(part)
                            };
                            Debug.Log($"Stage production required for {part.Name}: {currentFamilies[part.Id].StageProduction}");
                            fol.AddPair(new HardwareOccurencePair(0, 1));
                            techlist.Merge(currentFamilies[part.Id].Technologies);
                        }
                        XElement partdata = GetData(partxml, primarymodname);
                        SRPart partmatch = _hm.Get<SRPart>(p => XNode.DeepEquals(partdata, p.Data));
                        if (partmatch != null) 
                        {
                            config.Parts[part.PartScript] = partmatch;
                            if (!partmatch.Developed)
                            {
                                vr?.AddMessage("partnotyetdev", "Part " + partmatch.Name + " has not completed development." , part);
                            }
                            if (partmatch.DesignerCategory != null)
                            {
                                //Debug.Log("Part belongs to designer category " +partmatch.DesignerCategory);
                                HardwareOrder match = config.Orders.Find(or => or.ContractorId == partmatch.ContractorId);
                                if (match == null)
                                {
                                    //Debug.Log("No matching order");
                                    match = new HardwareOrder()
                                    {
                                        ContractorId = partmatch.ContractorId,
                                        ContractorName = Contractors[partmatch.ContractorId].Name,
                                        TotalProduction = partmatch.ProductionRequired,
                                        Price = partmatch.Price,
                                    };
                                    match.HardwareRequired.AddPair(partmatch.Id, 1);
                                    config.Orders.Add(match);
                                }
                                else
                                {
                                    //Debug.Log("found matching order: "+ match.ToString());
                                    match.Price += partmatch.Price;
                                    match.TotalProduction += partmatch.ProductionRequired;
                                    match.HardwareRequired.AddPair(partmatch.Id, 1);
                                }
                                //Debug.Log("Order Added to config");
                            }
                        } 
                        else
                        {
                            config.Parts[part.PartScript] = new SRPart()
                            {
                                Id = 0,
                                FamilyId = currentFamilies[part.Id].Id,
                                Name = PartMods[primarymodname].UndevelopedPartName(part),
                                PrimaryMod = primarymodname,
                                Data = partdata,
                                DesignerCategory = PartMods[primarymodname].DesignerCategory
                            };
                            if (PartMods[primarymodname].DesignerCategory != null)
                            {
                                vr?.AddMessage("partnotdev", "Part " + part.Name + " needs to be developed." , part);
                            }
                            
                        }
                    }
                    else 
                    {
                        priceList[part.Id]=part.Price;  
                        if (part.GetModifier<CommandPodData>() != null && part.GetModifier<CrewCompartmentData>() == null)
                        {
                            vr?.AddMessage("nonsrcommand", "Command pods require avionics.  Use the included avionics parts instead." , part);
                        }
                    } 
                }
                //Debug.Log(currentFamilies.Count.ToString() +" spacerace parts found. Family Occurence List: " +fol.ToString());
                if (currentFamilies.Count > 0) 
                {
                    flightStage = true;
                    SRStage stage = _hm.Get<SRStage>(st => fol.IsSame(st.PartFamilies));
                    if (stage == null)
                    {
                        stage = _hm.NewStage(commandPod.Part.PartScript.CraftScript.Data.Name, config.Stages.Count, techlist, fol);
                        stage.FamiliesOverride = currentFamilies.Values.GroupBy(v => v)
                            .Where(group => group != null && group.Count() != 0)
                            .Select(group => (group.Key, group.Count())).ToList();
                        stage.ProductionRequired = currentFamilies.Sum(pair => pair.Value.StageProduction);
                    }
                    //Debug.Log("Stage added with techlist: "+techlist.ToString());
                    config.Stages[stage]=currentFamilies;
                    if (stage.Id == 0) vr?.AddMessage("stagnotdev", "A stage needs to be developed.");
                    else
                    {
                        if (!stage.Developed) vr?.AddMessage("stagenotyetdev", $"{stage.Name} has not finished development yet.");
                        HardwareOrder match = config.Orders.Find(or => or.ContractorId == stage.ContractorId);
                        if (match == null)
                        {
                            match = new HardwareOrder()
                            {
                                ContractorId = stage.ContractorId,
                                ContractorName =  Contractors[stage.ContractorId].Name,
                                CurrentProduction = 0,
                                TotalProduction = stage.ProductionRequired,
                                Price = stage.Price,
                            };
                            match.HardwareRequired.AddPair(stage.Id, 1);
                            config.Orders.Add(match);
                        }
                        else
                        {
                            match.Price += stage.Price;
                            match.TotalProduction += stage.ProductionRequired;
                            match.HardwareRequired.AddPair(stage.Id, 1);
                        }
                    }
                }
                if (!flightStage)
                {
                    config.MassDry = 0F;
                    config.MassWet = 0F;
                    vr?.Messages.RemoveAll(m => currentParts.Any(p => p.Id == m.PartID));
                }
                currentParts = new List<PartData>(partGraph.Parts);
            }
            config.UndevelopedPartPrice = priceList.Values.Sum();
            CraftData craftData = commandPod.Part.PartScript.CraftScript.Data;
            config.Height = craftData.Size.y;
            config.Width = Mathf.Max(craftData.Size.x, craftData.Size.z);
            LastCraftConfig = new SRCraftConfig(config, "designer"); // maybe this can be moved to the play button?
            return config;
        }
        /// <summary>
        /// Gets the part properties to display in the devpanel flyout
        /// </summary>
        /// <param name="part">the part</param>
        /// <param name="primarymod">the primary spacerace mod for the part</param>
        /// <returns>A dictionary of property names and their values</returns>
        public Dictionary<string,string> GetDevPanelProperties(PartData part, string primarymod)
        {
            return PartMods[primarymod].DevPanelDisplay(part);
        }
        /// <summary>
        /// Gets the list of technologies related to a part
        /// </summary>
        /// <param name="part">The part</param>
        /// <param name="primarysrmod">The primary spacerace mod of the part</param>
        /// <param name="family">Whether to restrict to technologies that are relevant to the family</param>
        /// <returns>The list of technologies</returns>
        public TechList GetTechList(PartData part, string primarysrmod, bool family = false)
        {
            if (PartMods.TryGetValue(primarysrmod, out ISRPartMod mod))
            {
                return mod.Technologies(part,family);
            }
            Debug.LogWarning("GetTechList() could not find an entry for mod: "+primarysrmod);
            return new TechList();
        }
        /// <summary>
        /// The number of times a technology has been developed 
        /// (distinct contractors for parts and stages, number of integrations for integrations)
        /// </summary>
        /// <param name="tech">The tech name</param>
        /// <param name="category">The project category</param>
        /// <returns>The number of times</returns>
        public int TimesDeveloped(Technology tech, ProjectCategory category)
        {
            switch (category)
            {
                case ProjectCategory.Part: 
                    return _data.PartTechDeveloped.Find(tot => tot.Mod ==tech.Mod && tot.Tech == tech.Tech)?.Occurences ?? 0;
                case ProjectCategory.Stage:
                    return _data.StageTechDeveloped.Find(tot => tot.Mod ==tech.Mod && tot.Tech == tech.Tech)?.Occurences ?? 0;
                case ProjectCategory.Integration:
                    return _data.TechIntegrated.Find(tot => tot.Mod ==tech.Mod && tot.Tech == tech.Tech)?.Occurences ?? 0;
                default : return 0;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tech">The tech occurence triple (mod name, tech name, occurences</param>
        /// <param name="contractorId">The contractor id</param>
        /// <param name="category">The category of project</param>
        /// <returns>The efficiencyfactor (description and penalty)</returns>
        public EfficiencyFactor GetEfficiencyFactor(Technology tech, int contractorId, ProjectCategory category)
        {
            switch (category)
            {
                case ProjectCategory.Integration:
                    return PartMods[tech.Mod].EfficiencyFactor(tech.Tech, TimesDeveloped(tech, category), true, category);
                default: 
                    return PartMods[tech.Mod].EfficiencyFactor(tech.Tech, TimesDeveloped(tech, category), Contractors[contractorId].Data.DevelopedTechs.Contains(tech), category);
            }
            
        }

        /// <summary>
        /// Generates a list of bids to develop a set of stages
        /// </summary>
        /// <param name="stages">A list of stages</param>
        /// <returns>A list of bids</returns>
        public List<StageDevBid> GetStageBids(List<SRStage> stages)
        {
            TechList techlist = new TechList(stages.Select(st => st.Technologies));
            List<PartDevBid> offers = new List<PartDevBid>();
            List<IContractorScript> contractors = Contractors.Values
                .Where(con => con.Data.HardwareTypes.Contains("Stage"))
                .Select(con => con as IContractorScript)
                .ToList();
            return _costCalculator.GetStageBids(stages, contractors, _data.StageTechDeveloped);
        }
        /// <summary>
        /// Generates a list of bids to develop a part
        /// </summary>
        /// <param name="part">The part</param>
        /// <returns>A list of bids</returns>
        /// <exception cref="ArgumentException"></exception>
        public List<PartDevBid> GetPartBids(PartData part)
        {
            string primanymodname = GetPrimarySpaceRaceModName(part);
            XElement famdata = GetFamData(part, primanymodname);
            SRPartFamily fam = _hm.Get<SRPartFamily>(fam => XNode.DeepEquals(fam.Data, famdata));
            if (PartMods.TryGetValue(primanymodname, out ISRPartMod primarysrmod))
            {
                Technology famtech = new Technology(primanymodname, string.Format("PartFamily:{0:D}",fam?.Id ?? 0), fam?.Name ?? primarysrmod.FamilyName(part));
                List<IContractorScript> contractors = Contractors.Values
                .Where(con => con.Data.HardwareTypes.Contains(primanymodname))
                .Select(con => con as IContractorScript)
                .ToList();

                //Debug.Log("Found " +contractors.Count.ToString() + " contractors for " + primanymodname);
                return _costCalculator.GetPartDevBids(part, primarysrmod, contractors, famtech);
            }
            else throw new ArgumentException(primanymodname + " was not found in the ProgramManager.PartMods dictionary");
        }
        /// <summary>
        /// Begins a project to develop a list of stages
        /// </summary>
        /// <param name="stages">The stages</param>
        /// <param name="devname">The name of the project</param>
        /// <param name="bid">The bid accepted</param>
        /// <exception cref="ArgumentException"></exception>
        public void NewStageDevelopment(List<SRStage> stages, string devname, StageDevBid bid)
        {
            if (stages.Count == 0) throw new ArgumentException("No stages. You shouldn't have gotten this far.");
            List<int> stageIds = new List<int>();
            for(int i = 0; i < stages.Count; i++)
            {
                stages[i].ContractorId = bid.ContractorId;
                stages[i].ContractorMarkup = bid.Markup;
                // AddStage returns the assigned id
                stageIds.Add(_hm.AddStage(stages[i]));
            }
            int id = _data.NextId;
            _data.NextId += 1;
            StageDevelopmentData sdata = new StageDevelopmentData
            {
                    Id = id,
                    Name = bid.Name ?? devname,
                    TotalProgress = bid.BaseDevTime * bid.DevCostPerDay * Formulas.SRFormulas.DaysPerSecond,
                    StageIds = stageIds,
                    ContractorId = bid.ContractorId,
                    Technologies = bid.Technologies,
                    BaseProgressPerDay = bid.DevCostPerDay,
                    ProductionCapacity = bid.ProductionCapacity,
                };
            StageDevelopments[id] = new StageDevelopmentScript(sdata, this);
            ActiveChildren[id] = StageDevelopments[id];
            ActiveChildren[id].Initialize();
            ActiveChildren[id].UpdateRates(true);
            ReplaceInspectorGroup(id);
            ActiveChildren[id].UpdateProgress(0.0);
        }
        /// <summary>
        /// Begins a project to develop a new part.
        /// </summary>
        /// <param name="part">The part</param>
        /// <param name="bid">The bid accepted</param>
        /// <exception cref="ArgumentException"></exception>
        public void NewPartDevelopment(PartData part, PartDevBid bid)
        {
            int id =_hm.GetPartId(part);
            if (id == 0)
            {
                id = _data.NextId;
                _data.NextId += 1;
                SRPart srpart = _hm.AddPart(part, bid);
                //Debug.Log("AddPart with techlist: "+srpart.Technologies.ToString());
                PartDevelopmentData pdata = new PartDevelopmentData
                {
                        Id = id,
                        Name = bid.Name ?? part.Name,
                        TotalProgress = bid.BaseDevTime * bid.DevCostPerDay * Formulas.SRFormulas.DaysPerSecond,
                        PartId = srpart.Id,
                        PartFamilyId = srpart.FamilyId,
                        PrimaryMod = srpart.PrimaryMod,
                        ContractorId = bid.ContractorId,
                        Technologies = srpart.Technologies,
                        BaseProgressPerDay = bid.DevCostPerDay,
                        ProductionCapacity = bid.ProductionCapacity
                    };
                PartDevelopments[id] = new PartDevelopmentScript(pdata, this);
                ActiveChildren[id] = PartDevelopments[id];
                ActiveChildren[id].Initialize();
                ActiveChildren[id].UpdateRates(true);
                ReplaceInspectorGroup(id); 
                ActiveChildren[id].UpdateProgress(0.0); // immediately complete any 0 progress projects
            }
            else throw new ArgumentException("You tried to add a part that was already in the DB. This shouldn't have gotten past the earlier checks.");
        }
        public void NewLaunchpadConstruction(Career.Contracts.Contract contract, LaunchLocation loc)
        {
            int id = _data.NextId;
            _data.NextId += 1;
            ConstructionData pdata = new ConstructionData
            {
                Id = id,
                Name = loc.Name,
                TotalProgress = 10000.0 * loc.MaxCraftMass,
                ContractorId = 0,
                BaseProgressPerDay = 2000.0 * math.sqrt(loc.MaxCraftMass),
                ContractId = contract.Id,
                AdditionalTechnicians = (int)Math.Round(loc.MaxCraftMass) / 20
            };
            Construction[id] = new ConstructionScript(pdata, this);
            ActiveChildren[id] = Construction[id];
            ActiveChildren[id].Initialize(); 
            ActiveChildren[id].UpdateRates(true);
            ReplaceInspectorGroup(id);
            ActiveChildren[id].UpdateProgress(0.0); // immediately complete any 0 progress projects
        }
        public void SetMaxTechnicians()
        {
            int i = 0;
            foreach (LaunchLocation loc in _gs.LaunchLocations.Where(l => Career.UnlockedLocations.Contains(l.Name)))
            {
                //Debug.Log($"{loc.Name} gives {(int)(Math.Round(loc.MaxCraftMass) / 20)} technicians");
                i += (int)(Math.Round(loc.MaxCraftMass) / 20);
            }
            _data.MaxTechnicians = i;
            _data.TotalTechnicians = Math.Min(_data.MaxTechnicians, _data.TotalTechnicians);
        }
        public void NewIntegrationFromDesigner(VerboseCraftConfiguration vconfig, string name)
        {
            SRCraftConfig config = _hm.AddCraftConfig(vconfig,name);
            string desc = string.Empty;
            config.ProductionRequired = _costCalculator.GetIntegrationProduction(config, ref desc);
            config.ProductionDescription = desc;
            DesignerScript designer = Game.Instance.Designer as DesignerScript;
            XElement xe = designer.GenerateCraftXml(false, true);
            Designs.SaveCraft(string.Format("Integration{0:D}", config.Id), xe);
            NewIntegration(config, name, false);
        }
        public void RepeatIntegration(SRCraftConfig config, string name, CraftNode recovery= null,  LaunchLocation location = null)
        {
            NewIntegration(config, name, true, recovery, location);
        }
        /// <summary>
        /// Creates a new integration project to integrate a craft
        /// </summary>
        /// <param name="config">The config in the hardware manager</param>
        /// <param name="name">A name for the craft</param>
        /// <param name="repeat"> Whether this is a repeat of an earlier integration</param>
        public void NewIntegration(SRCraftConfig config, string name, bool repeat, CraftNode recovery = null, LaunchLocation location = null)
        {
            if (recovery == null)
            {
                if (Career.Money < config.UndevelopedPartPrice) 
                {
                    string moneyerror = string.Format("Cannot afford {0} for parts", Units.GetMoneyString(config.UndevelopedPartPrice));
                    if (Game.InDesignerScene) Game.Instance.Designer.DesignerUi.ShowMessage(moneyerror);
                    if (Game.InFlightScene) Game.Instance.FlightScene.FlightSceneUI.ShowMessage(moneyerror);
                    return;
                }
                Career.SpendMoney(config.UndevelopedPartPrice);
            }
            int id = _data.NextId;
            _data.NextId += 1;
            IntegrationData data = new IntegrationData()
            {
                Id = id,
                ConfigId = config.Id,
                Name = name,
                Status = recovery == null ? IntegrationStatus.Waiting : IntegrationStatus.Recovery,
                TotalProgress = recovery == null? config.ProductionRequired : _costCalculator.GetIntegrationRollout(config),
                BaseMaxTechnicians = _costCalculator.GetMaxTechnicians(config),
                Technologies = config.Technologies,
                Orders = recovery == null ?  new List<HardwareOrder>(config.Orders.Select(c => c.NewOrder())) : new List<HardwareOrder>(),
                Repeat = repeat,
                Recovered = recovery != null,
                RolloutProgress = _costCalculator.GetIntegrationRollout(config),
                RepairProgress = _costCalculator.GetIntegrationRepair(config),
                LaunchLocation = location?.Name,
                NodeId = recovery?.NodeId ?? 0,
            };
            Integrations[id]=new IntegrationScript(data, this);
            ActiveChildren[id] = Integrations[id];
            ActiveChildren[id].Initialize();
            if (recovery != null)
            {
                ReserveLaunchLocation(location.Name);
                Integrations[id].SetTechnicians(data.BaseMaxTechnicians);
            }
            ActiveChildren[id].UpdateRates(true);
            ReplaceInspectorGroup(id);
            ActiveChildren[id].UpdateProgress(0.0);
        }
        public void NewExternalCraftOrder(SRCraftConfig config, string name, int ordered, double daysper)
        {
            ExternalIntegrationScript script = ExternalIntegrations.Values.FirstOrDefault(sc => sc.Config.Similarity(config) >= 0.65);
            if (script == null)
            {
                Debug.Log("New external integration");
                NewExternalIntegration(config, name, ordered, daysper);
            }
            else
            {
                Debug.Log($"Adding to order {script.Name}");
                script.Data.Ordered += ordered;
            }
        }
        private void NewExternalIntegration(SRCraftConfig config, string name, int ordered, double daysper)
        {
            int id = _data.NextId;
            _data.NextId += 1;
            ExternalIntegrationData data = new ExternalIntegrationData()
            {
                Id = id,
                ConfigId = config.Id,
                Name = name,
                TotalProgress = config.ProductionRequired,
                BaseProgressPerDay = config.ProductionRequired / daysper,
                Technologies = config.Technologies,
                Orders = new List<HardwareOrder>(config.Orders.Select(c => c.NewFreeOrder())),
                Ordered = ordered
            };
            ExternalIntegrations[id]=new ExternalIntegrationScript(data, this);
            ActiveChildren[id] = ExternalIntegrations[id];
            ActiveChildren[id].Initialize();
            ActiveChildren[id].UpdateRates(true);
            ReplaceInspectorGroup(id);
            ActiveChildren[id].UpdateProgress(0.0);
        }
        public bool LocationAvailable(string loc)
        { 
            return !_occupiedLauchLocations.Contains(loc);
        }
        public void ReserveLaunchLocation(string loc)
        {
            if (!LocationAvailable(loc)) 
            throw new ArgumentException("Location is occupied");
            _occupiedLauchLocations.Add(loc);
        }
        public void ReleaseLaunchLocation(string loc)
        {
            if (LocationAvailable(loc))
            throw new ArgumentException("Released location was not occupied");
            _occupiedLauchLocations.Remove(loc);
        }
        private void CopySpaceCenterXml()
        {
            Debug.Log("Copying space center xml");
            string scPath = Path.Combine(CareerState.CheckOverridePath(Career.Contracts.ResourcesPath, "Crafts/"), (ModSettings.Instance?.VisibleSpaceCenters ?? false) ? "Space-Center-Visible.xml" : "Space-Center.xml");
            Game.Instance.CraftDesigns.SaveCraft(_spaceCenterCraftFile, XDocument.Load(scPath).Root);
            Debug.Log("Copied space center xml");
        }
        public void GoToSpaceCenter(bool destroy = false)
        {
            if (Game.InFlightScene)
            {
                IFlightScene flightSceneScript = Game.Instance.FlightScene;
                CraftNode craft = flightSceneScript.CraftNode as CraftNode;
                ICraftNode spacecenter = fs.FlightState.CraftNodes.FirstOrDefault(n => n.NodeId == -1);
                if (spacecenter?.CraftScript != null && flightSceneScript.ChangePlayersActiveCommandPodImmediate(spacecenter.CraftScript.ActiveCommandPod, spacecenter))
                {
                    string text = $"Returned to Space Center.";
                    flightSceneScript.FlightSceneUI.ShowMessage(text);
                    if (destroy)
                    {
                        craft.DestroyOnExitFlightScene = true;
                        craft.Enabled = false;
                        craft.DestroyCraft();
                    }
                }
                else if (spacecenter != null)
                {
                    flightSceneScript.CraftNode.DestroyOnExitFlightScene = destroy;
                    flightSceneScript.ChangePlayersActiveCraftNode(spacecenter);
                    Debug.Log("Reloading scene to return to Space Center.");
                }
                else 
                {
                    FlightSceneScript.Instance.ExitFlightScene(saveFlightState: true, FlightSceneExitReason.SaveAndRecover, "Menu");
                }
                return;
            }
            if (Game.InDesignerScene)
            {
                IDesigner designer = Game.Instance.Designer;
                designer.SaveCraft(CraftDesigns.EditorCraftId);
                designer.PerformanceAnalysis.Visible = false; 
                CraftScript craft = designer.CraftScript as CraftScript;
                craft.Unload();
            }
            FlightSceneLoadParameters parameters = null;
            if (Game.Instance.GameState.LoadFlightStateData().CraftNodes.Any(n => n.NodeId == -1))
            {
                parameters = FlightSceneLoadParameters.ResumeCraft(-1);
            }
            if (parameters == null) 
            {
                SpaceCenterLocation loc = SpaceCenter.UnlockedLocations.LastOrDefault();
                if (loc == null) throw new ArgumentException($"No unlocked space center found.");
                CopySpaceCenterXml(); 
                parameters = FlightSceneLoadParameters.NewCraft(_spaceCenterCraftFile, "Space Center", loc.Location, 0);
                SpawningSpaceCenter = 1;
            }   
            Game.Instance.StartFlightScene(parameters);
        }

        public void PreviousSpaceCenter()
        {
            int i = CurrentSpaceCenterLocation == null ? 0 : SpaceCenter.UnlockedLocations.IndexOf(CurrentSpaceCenterLocation);
            SwitchToSpaceCenter(SpaceCenter.UnlockedLocations[(i - 1) % SpaceCenter.UnlockedLocations.Count()]);
        }

        public void NextSpaceCenter()
        {
            int i = CurrentSpaceCenterLocation == null ? 0 : SpaceCenter.UnlockedLocations.IndexOf(CurrentSpaceCenterLocation);
            SwitchToSpaceCenter(SpaceCenter.UnlockedLocations[(i + 1) % SpaceCenter.UnlockedLocations.Count()]);
        }


        public void SwitchToNearestSpaceCenter(LaunchLocation location)
        {
            if (!Game.InFlightScene)
            {
                return;
            }
            SpaceCenterLocation loc = SpaceCenter.Locations.FirstOrDefault(l => l.LaunchPads.Contains(location.Name));
            if (loc == null)
            {
                return;
            }
            SwitchToSpaceCenter(loc);
        }

        public void SwitchToSpaceCenter(SpaceCenterLocation loc)
        {
            ICraftNode node = FlightSceneScript.Instance.CraftNode;
            if (node.NodeId == -1)
            {
                //Debug.Log($"Switching space center to {loc.Location.Name}");
                Quaterniond heading = Quaterniond.identity;
                IPlanetNode planetNode = fs.FlightState.RootNode.FindPlanet(loc.Location.PlanetName);
                Vector3d surfacePosition = planetNode.GetSurfacePosition(loc.Location.Latitude * Mathd.PI / 180.0, loc.Location.Longitude * Mathd.PI / 180.0, AltitudeType.AboveGroundLevel, loc.Location.AltitudeAboveGroundLevel, 0F);
                Vector3d position = planetNode.SurfaceVectorToPlanetVector(surfacePosition);
                Vector3d velocity = planetNode.Rotation * planetNode.CalculateSurfaceVelocity(surfacePosition.normalized * planetNode.PlanetData.Radius);
                heading = planetNode.Rotation * loc.Location.Rotation;
                //Debug.Log($"Position = {position}");
                //Debug.Log($"Velocity = {velocity}");
                node.SetStateVectorsAtDefaultTime(position, velocity);
                node.RecalculateFrameState(fs.ViewManager.GameView.ReferenceFrame);
                CurrentSpaceCenterLocation = loc;
                //Debug.Log("Node moved?");
            }
        }
        
        /// <summary>
        /// Removes the project from the program manager
        /// </summary>
        /// <param name="id">The project id</param>
        public void EndProject(int id)
        {
            PartDevelopments.Remove(id);
            StageDevelopments.Remove(id);
            Integrations.Remove(id);
            ExternalIntegrations.Remove(id);
            Construction.Remove(id);
            ActiveChildren.Remove(id);
            if (_panel != null)
            {   
                ReplaceInspectorGroup(0); // the budget group
                if (InspectorGroups.Keys.Contains(id)) 
                {
                    _panel.Model.RemoveGroup(InspectorGroups[id]);
                    InspectorGroups.Remove(id);
                    _panel.RebuildModelElements();
                }
            }
            Debug.Log("Ended project "+id.ToString());
        }
        /// <summary>
        /// Update the progress rate of every project.  Also sets the time until the next change (completion, delay or delivery).
        /// </summary>
        public void UpdateAllRates(bool efficiencies)
        {
            _data.TimeUntilNextCompletion = double.PositiveInfinity;
            
            foreach (IProgramChild child in ActiveChildren.Values.Where(ch => ch.Active))
            {
                child.UpdateRates(efficiencies);
            }
            SetWarp(_warp); // if warp is true, start warping to the next change
        }     
        /// <summary>
        /// Give an integration the highest priority for receiving hardware
        /// </summary>
        /// <returns>The priority for the integration to use</returns>
        public int Prioritize()
        {
            _data.IntegrationsPrioritized +=1;
            return _data.IntegrationsPrioritized;
        }
        /// <summary>
        /// Sets the program's time until the next change (delivery, delay or completion)
        /// equal to the project's time if the project's time is sooner.
        /// </summary>
        /// <param name="time">The time of the next change in the project</param>
        public void SetNextCompletion(double time, IProgramChild child)
        {
            if(time < _data.TimeUntilNextCompletion) 
            {
                _data.TimeUntilNextCompletion = time;
                _ttnc?.Update();
                if (_ttnc != null)
                {
                    _ttnc.Tooltip = child.Tooltip;
                }
            }
        }
        /// <summary>
        /// Loads the program data and projects from disk.
        /// </summary>
        public void Load()
        {
            _panel = null;
            _hm?.RemoveAllDesignerParts();
            _gs = Game.Instance.GameState;
            if (_gs.Career.Path == "SpaceRace" && _gs.LoadFlightState().PlanetarySystem.Name != "Solar System" && _gs.LoadFlightState().PlanetarySystem.Name != "RSS")
            {
                Game.Instance.UserInterface.CreateMessageDialog("The SpaceRace career is designed for the Solar System.  You do not appear to have chosen the Solar System for this career.");
            }
            if (File.Exists(_hardwareFileName))
            {   
                FileStream stream = new FileStream(_hardwareFileName, FileMode.Open); 
                HardwareDB hdb = _hardwareSerializer.Deserialize(stream) as HardwareDB;
                stream.Close();  
                _hm = new HardwareManager(this, hdb);
            }
            else if (File.Exists(_careerHardwareFileName))
            {
                FileStream stream = new FileStream(_careerHardwareFileName, FileMode.Open);  
                HardwareDB hdb = _hardwareSerializer.Deserialize(stream) as HardwareDB;
                stream.Close();
                _hm = new HardwareManager(this, hdb);
            }
            else _hm=new HardwareManager(this, new HardwareDB());
            Debug.Log("Hardware db loaded");
            _hm.Initialize();
            List<HistoricalEvent> hevents = new List<HistoricalEvent>();
            if (File.Exists(_historyFileName))
            {
                FileStream stream = new FileStream(_historyFileName, FileMode.Open);
                hevents = _historySerializer.Deserialize(stream) as List<HistoricalEvent> ?? new List<HistoricalEvent>(){};
                stream.Close(); 
            }
            _hist = new HistoryManager(this, hevents);
            if (File.Exists(_historyFileName))
            {
                FileStream stream = new FileStream(_staticEventsFileName, FileMode.Open);
                _hist.RegisterEvents(_staticEventSerializer.Deserialize(stream) as List<StaticHistoricalEvent>);
                Debug.Log(_hist.Events.Count.ToString());
                stream.Close(); 
            }
            Debug.Log("History loaded");
            _hist.AdjustTechCosts(_gs.GetCurrentTime());
            List<FundingData> funding;
            if (File.Exists(_fundingFileName))
            {   
                FileStream stream = new FileStream(_fundingFileName, FileMode.Open); 
                funding = _fundingSerializer.Deserialize(stream) as List<FundingData>;
                stream.Close();  
                Debug.Log("Funding loaded");
            }
            else 
            {
                funding = new List<FundingData>();
                Debug.Log("Funding not found.  Blank list loaded");
            }
            if (File.Exists(_spaceCentersFileName) && File.Exists(_structuresFileName))
            {   
                XDocument scdoc = XDocument.Load(_spaceCentersFileName);
                List<SpaceCenterLocation> spacecenters = new List<SpaceCenterLocation>();
                foreach (XElement sclxml in scdoc.Element("SpaceCenterLocations").Elements())
                {
                    spacecenters.Add(new SpaceCenterLocation(sclxml));
                }
                Debug.Log("SpaceCenters.xml loaded");
                FileStream stream = new FileStream(_structuresFileName, FileMode.Open); 
                List<SpaceCenterStructure> structures = _structuresSerializer.Deserialize(stream) as List<SpaceCenterStructure>;
                stream.Close();
                Debug.Log("Structures.xml loaded");
                SpaceCenter = new SpaceCenterManager(this, spacecenters, structures);
            }
            else throw new Exception("cannot find "+ _structuresFileName + " or "+_spaceCentersFileName);
            System.IO.Directory.CreateDirectory(_gs.RootPath + "/../Integrations/");
            Designs = new CraftDesigns(_gs.RootPath + "/../Integrations/");
            PartDevelopments.Clear();
            StageDevelopments.Clear();
            Integrations.Clear();
            ExternalIntegrations.Clear();
            Contractors.Clear();
            Funding.Clear();
            FundingByContractId.Clear();
            ActiveChildren.Clear();
            ProgramDB db = null;
            bool newflag = false;
            if (File.Exists(_programFileName))
            {
                FileStream stream = new FileStream(_programFileName, FileMode.Open);
                db = _programSerializer.Deserialize(stream) as ProgramDB;
                stream.Close(); 
            }
            else if (File.Exists(_defaultProgramFileName))
            {
                FileStream stream = new FileStream(_defaultProgramFileName, FileMode.Open);
                db = _programSerializer.Deserialize(stream) as ProgramDB;
                stream.Close(); 
            }
            else
            {
                throw new Exception(_defaultProgramFileName+" not found");
            }
            Debug.Log("Pdata loaded");
            _data = db.Pdata;
            _occupiedLauchLocations.Clear();
            SRContracts = new SpaceRaceContractsScript(this, db.ContractsData);
            Budget = new BudgetScript(this);
            ActiveChildren[0] = Budget;
            // contractors must go first since projects need them in order to initialize
            foreach (ContractorData data in db.Contractors)
            {
                if (data.BaseProductionRate < 0)
                {
                    Debug.Log($"{data.Name} has production rate {data.BaseProductionRate}, setting to 0.");
                    data.BaseProductionRate = 0;
                }
                if (data.Inventory < 0)
                {
                    Debug.Log($"{data.Name} has inventory {data.Inventory}, setting to 0.");
                    data.Inventory= 0;
                }
                if (data.Temporary) Contractors[data.Id] = new TemporaryContractorScript(data, this);
                else Contractors[data.Id] = new ContractorScript(data, this);
                if (data.Active) ActiveChildren[data.Id] = Contractors[data.Id];
            }
            foreach (IntegrationData data in db.Integrations)
            {   
                if (data.CurrentProgress < 0)
                {
                    Debug.Log($"{data.Name} has progress {data.CurrentProgress}, setting to 0.");
                    data.CurrentProgress = 0;
                }
                _occupiedLauchLocations.Add(data.LaunchLocation);
                Integrations[data.Id] = new IntegrationScript(data, this);
                ActiveChildren[data.Id] = Integrations[data.Id];
            }
            SetMaxTechnicians();
            CorrectAvailableTechnicians();
            foreach (ExternalIntegrationData data in db.ExternalIntegrations)
            {   
                if (data.CurrentProgress < 0)
                {
                    Debug.Log($"{data.Name} has progress {data.CurrentProgress}, setting to 0.");
                    data.CurrentProgress = 0;
                }
                ExternalIntegrations[data.Id] = new ExternalIntegrationScript(data, this);
                ActiveChildren[data.Id] = ExternalIntegrations[data.Id];
            }
            foreach (StageDevelopmentData data in db.StageDevelopments)
            {
                if (data.CurrentProgress < 0)
                {
                    Debug.Log($"{data.Name} has progress {data.CurrentProgress}, setting to 0.");
                    data.CurrentProgress = 0;
                }
                StageDevelopments[data.Id] = new StageDevelopmentScript(data, this);
                ActiveChildren[data.Id] = StageDevelopments[data.Id];
            }
            foreach (PartDevelopmentData data in db.PartDevelopments)
            {
                if (data.CurrentProgress < 0)
                {
                    Debug.Log($"{data.Name} has progress {data.CurrentProgress}, setting to 0.");
                    data.CurrentProgress = 0;
                }
                PartDevelopments[data.Id] = new PartDevelopmentScript(data, this);
                ActiveChildren[data.Id] = PartDevelopments[data.Id];
            }    
            foreach (ConstructionData data in db.ConstructionProjects)
            {
                if (data.CurrentProgress < 0)
                {
                    Debug.Log($"{data.Name} has progress {data.CurrentProgress}, setting to 0.");
                    data.CurrentProgress = 0;
                }
                Construction[data.Id] = new ConstructionScript(data, this);
                ActiveChildren[data.Id] = Construction[data.Id];
            } 
            foreach (FundingData data in funding)
            {
                if (data.CurrentProgress < 0)
                {
                    Debug.Log($"{data.Name} has progress {data.CurrentProgress}, setting to 0.");
                    data.CurrentProgress = 0;
                }
                Funding[data.Id] = new FundingScript(data, this, db.Funding.FirstOrDefault(f => f.Name == data.Name));
                foreach (string contractid in data.ContractIds)
                {
                    FundingByContractId[contractid] = Funding[data.Id];
                }
            }
            foreach (IProgramChild child in ActiveChildren.Values)
            {
                child.Initialize();
            }
            foreach (Career.Contracts.Contract contract in _gs.Career.Contracts.Active)
            {
                if (FundingByContractId.TryGetValue(contract.Id, out FundingScript script))
                {
                    AddContractFunding(contract, script);
                }
            }
            foreach (CrewMember orig in _gs.Crew.Members.Where(mem=> mem.Name == "Yuri G." || mem.Name== "Sally R."))
            {
                orig.State = CrewMemberState.Deceased;
            }
            if (newflag) NewSpaceProgram?.Invoke(this);
            Debug.Log("Loaded SpaceRace Data");
        }
        /// <summary>
        /// Saves the spacerace data to disk.  
        /// Checks for a gamestate match so that it does not fire in the middle of constructing a new gamestate.
        /// </summary>
        /// <param name="id">id of the gamestate</param>
        public void Save(string id)
        {
            if (_gs.Id != id) return;
            ProgramDB db = new ProgramDB(){Pdata = _data};
            foreach (IntegrationScript script in Integrations.Values)
            {
                db.Integrations.Add(script.Data);
            }
            foreach (ExternalIntegrationScript script in ExternalIntegrations.Values)
            {
                db.ExternalIntegrations.Add(script.Data);
            }
            foreach (PartDevelopmentScript script in PartDevelopments.Values)
            {
                db.PartDevelopments.Add(script.Data);
            }
            foreach (StageDevelopmentScript script in StageDevelopments.Values)
            {
                db.StageDevelopments.Add(script.Data);
            }
            foreach (ContractorScript script in Contractors.Values)
            {
                db.Contractors.Add(script.Data);
            }
            foreach (ConstructionScript script in Construction.Values)
            {
                db.ConstructionProjects.Add(script.Data);
            }
            Debug.Log("wrote construction");
            foreach (FundingScript script in Funding.Values)
            {
                db.Funding.Add(script.GetPersistantFundingData());
            }
            Debug.Log("wrote funding");
            db.ContractsData = SRContracts.Data;
            Debug.Log("wrote contractsdata");
            FileStream stream = new FileStream(_programFileName, FileMode.Create);
            _programSerializer.Serialize(stream, db);
            stream.Close();
            Debug.Log("wrote program data");
            FileStream stream2 = new FileStream(_historyFileName, FileMode.Create);
            _historySerializer.Serialize(stream2, _hist.PastEvents);
            stream2.Close();
            Debug.Log("wrote past events");
            FileStream stream3 = new FileStream(_hardwareFileName, FileMode.Create);
            _hardwareSerializer.Serialize(stream3, _hm.DB);
            stream3.Close();
            Debug.Log("wrote hardware");
            Debug.Log("Saved SpaceRace Data");
        }

        public void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            _panel = null;
            _gs= Game.Instance.GameState;
            if (e.Scene == ModApi.Scenes.SceneNames.Designer)
            {
                CreateInspector();
            }
            if (e.Scene == ModApi.Scenes.SceneNames.Flight)
            {  
                ContractsChanged = false;
                fs = Game.Instance.FlightScene;
                fs.FlightEnded += OnFlightEnded;
                fs.TimeManager.TimeMultiplierModeChanged += OnTimeMultiplierModeChanged;
                CheckForLaunch();
                if (Game.Instance.GameState.Type == ModApi.State.GameStateType.Default)
                {
                    CreateInspector();
                    //Debug.Log("LastUpdatedTime = " + _data.LastUpdateTime.ToString());
                    //Debug.Log("Time Now is " + fs.FlightState.Time.ToString());
                    if (_data.LastUpdateTime < 0.0) 
                    {
                        _data.LastUpdateTime = fs.FlightState.Time;
                        Debug.Log("Setting last update time to now");
                    }
                }
                SpaceCenter.OnFlightStart();
            }
        }
        private void OnTimeMultiplierModeChanged(TimeMultiplierModeChangedEvent e)
        {
            if (e.EnteredWarpMode)
            {
                Game.Instance.FlightScene.FlightSceneUI.SetNavSphereVisibility(false, false);
                return;
            }
            if (e.ExitedWarpMode && fs.CraftNode.NodeId > 0)
            {
                Game.Instance.FlightScene.FlightSceneUI.RestoreNavSphereVisibility();
                return;
            }
        }
        public void CheckForLaunch()
        {
            if (Game.InFlightScene)
            {
                CraftNode craft = FlightSceneScript.Instance.CraftNode as CraftNode;
                CraftData craftData = craft.CraftScript.Data;
                bool flag = Game.Instance.GameState.Type == ModApi.State.GameStateType.Simulation;
                if (SpawningSpaceCenter > 0) 
                {
                    craft.NodeId = -SpawningSpaceCenter;
                    CurrentSpaceCenterLocation = SpaceCenter.UnlockedLocations.Last();
                    Debug.Log("Set Space Center NodeId to "+craft.NodeId.ToString());
                }
                else if (Integrations.TryGetValue(LaunchingId, out IntegrationScript integration) && craft.NodeId == integration.Data.NodeId)
                {
                    Debug.Log("Launching integration "+integration.Id.ToString());
                    _data.Flights.Add((craft.NodeId, integration.Data.ConfigId));
                    flag = true;
                    ReleaseLaunchLocation(integration.Data.LaunchLocation);
                    Data.LastLaunchConfig = integration.Data.ConfigId;
                    LastCraftConfig = _hm.GetCraftConfig(c => c.Id == integration.Data.ConfigId);
                    foreach (PartData part in craftData.Assembly.Parts)
                    {
                        GetPrimarySpaceRaceMod(part)?.UpdateInfo(this);
                        FuelTankData tank = part.GetModifier<FuelTankData>();
                        if (tank != null) 
                        {
                            tank.Fuel = tank.Capacity;
                            part.PartScript.BodyScript.RecalculateMass();
                        }
                        FlightProgramData program = part.GetModifier<FlightProgramData>();
                        if (program != null)
                        {
                            program.ProcessXml = null;
                            program.Script.StartProgram();
                        }
                    }
                    if (!integration.Data.Recovered)
                    {   
                        _hm.IncrementCraftIntegrations(integration.Data.ConfigId);
                        foreach (Technology tech in integration.Data.Technologies) DevelopTech(tech, integration.Data.ContractorId, ProjectCategory.Integration);
                    }
                    LaunchLocation loc =  Game.Instance.GameState.LaunchLocations.FirstOrDefault(loc => loc.Name == integration.Data.LaunchLocation);
                    craft.SetInitialCraftNodeData(loc, FlightSceneScript.Instance.FlightState.Time);
                    CraftFuelSources fuel = craft.CraftScript.FuelSources as CraftFuelSources;
                    fuel.Rebuild(craft.CraftScript);     
                    EndProject(integration.Id);
                    UpdateAllRates(true);
                }
                else if (LaunchingId != 0)
                {
                    Debug.Log("Craft node does not match the launching integration.  Clearing.");
                }
                SRContracts.OnFlightStart(craft);
                Career.GenerateXml();
                Career.OnFlightStart(FlightSceneScript.Instance, flag); 
                _spaceCenterUi.OnCraftChanged(craft);
            }
            SpawningSpaceCenter = 0;
            LaunchingId = 0;
                                
        }
        private void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {   
            fs.TimeManager.TimeMultiplierModeChanged -= OnTimeMultiplierModeChanged;
            if (_spaceCenterUi != null)
            {
                Destroy(_spaceCenterUiGameObject);
                _spaceCenterUi = null;
            }
            _panel= null;
            UpdateProgress();
            fs = null;
            switch (e.ExitReason)
            {
                case FlightSceneExitReason.SaveAndDestroy:
                    goto case FlightSceneExitReason.SaveAndExit;
                case FlightSceneExitReason.SaveAndRecover:
                    goto case FlightSceneExitReason.SaveAndExit;
                case FlightSceneExitReason.UndoAndExit:
                    goto case FlightSceneExitReason.Retry;
                case FlightSceneExitReason.QuickLoad:
                    break;
                case FlightSceneExitReason.Retry:
                    break;
                case FlightSceneExitReason.CraftNodeChanged:
                    goto case FlightSceneExitReason.SaveAndExit;
                case FlightSceneExitReason.SaveAndExit:
                    //double gameTime = fs.FlightState.Time;
                    //bool updated = IncrementProjects(gameTime - _srdb.LastUpdateTime);
                    Save(_gs.Id);
                    break;
                default:
                    break;
            }
        }

        public void DevelopTech (Technology tot, int contractorid, ProjectCategory category)
        {
            switch (category)
            {
                case ProjectCategory.Part: 
                    if (Contractors[contractorid].DevelopTech(tot))
                    _data.PartTechDeveloped.IncrementTech(tot);
                    return;
                case ProjectCategory.Stage: 
                    if (Contractors[contractorid].DevelopTech(tot))
                    _data.StageTechDeveloped.IncrementTech(tot);
                    return;
                case ProjectCategory.Integration: 
                    _data.TechIntegrated.IncrementTech(tot);
                    return;
                default: 
                    throw new ArgumentException("Invalid project category");
            }  
        }
        public void DevelopStageTech (Technology tech, int contractorid)
        {
            if (Contractors[contractorid].DevelopTech(tech))
            {
                _data.StageTechDeveloped.IncrementTech(tech);
            }
        }
        public void OnClickCrewButton()
        {
            Game.Instance.UserInterface.CreateListView(new CrewListViewModel(this));
        }
        public void SendCrew(Contract contract)
        {
            if (_data.ContractCrewsProvided.Contains(contract.ContractNumber))
            {
                fs.FlightSceneUI.ShowMessage("The contract already has a crew.");
                return;
            }
            PropertyOccurenceList roster = RequiredCrew(contract);
            if (roster.TotalOccurences() > _gs.Crew.Members.Where(x => x.State == CrewMemberState.Available).Count())
            {
                fs.FlightSceneUI.ShowMessage("Not enough available astronauts.  Hire more.");
                return;
            }
            string locationname = FlightSceneScript.Instance.CraftNode.InitialCraftNodeData.First().LaunchLocationName;
            LaunchLocation ll = Game.Instance.GameState.LaunchLocations.First(loc => loc.Name == locationname);
            if (ll == null) 
            {
                throw new ArgumentException("Cannot find launch location matching this craft node.");
            }
            string astroPath = Path.Combine(CareerState.CheckOverridePath(Career.Contracts.ResourcesPath, "Crafts/"), "Astronaut.xml");
            double offset = 0.0003;
            CraftNode node;
            foreach (PropertyOccurencePair pop in roster)
            {
                CraftData astroData = Game.Instance.CraftLoader.LoadCraftImmediate(XDocument.Load(astroPath).Root);
                astroData.Assembly.Parts.First().Payload.ContractNumber = contract.ContractNumber;
                astroData.Assembly.Parts.First().Payload.PayloadId = pop.Id;
                for (int i = 0; i < pop.Occurences; i++)
                {
                    Career.SpendMoney(SRFormulas.SpawnCrewPrice);
                    LaunchLocation newlaunchLocation = new LaunchLocation(ll.Name, ll.LocationType, ll.PlanetName, ll.Latitude, ll.Longitude - offset, Vector3d.zero, 90.0, ll.AltitudeAboveGroundLevel);
                    node = FlightSceneScript.Instance.SpawnCraft(pop.Id, astroData, newlaunchLocation);
                    node.SetPhysicsEnabled(false, ModApi.Flight.GameView.PhysicsChangeReason.Warp);
                    node.ContractTrackingId = pop.Id;
                    offset += 0.00005;
                    //fs.FlightSceneUI.ShowMessage($"{node.CraftScript.ActiveCommandPod.Part.GetModifier<EvaData>().Name} is designated to complete {contract.Name}.");
                    //FlightSceneScript.Instance.ChangePlayersActiveCommandPodImmediate(node.CraftScript.ActiveCommandPod, node);
                }
            }
            if (contract.Id != "extra-crew")
            {
                _data.ContractCrewsProvided.Add(contract.ContractNumber);
                contract.ResetStatus();
            }
            else 
            {
                contract.Name = "Extra Crew";
            }
        }

        public void SetGroupsVisible(List<ProjectCategory> list)
        {
            _data.VisibleInspectorGroups = list;
            foreach (int i in InspectorGroups.Keys)
            {
                InspectorGroups[i].Visible = _data.VisibleInspectorGroups.Contains(ActiveChildren[i].Category);
            }
            InspectorGroups[0].Visible = _data.VisibleInspectorGroups.Contains(ProjectCategory.Budget);
            
        }

        public static List<ProjectCategory> categoriesDevelopment = new List<ProjectCategory>(){
            ProjectCategory.Part, ProjectCategory.Stage
        };

        public static List<ProjectCategory> categoriesIntegration = new List<ProjectCategory>(){
            ProjectCategory.Integration, ProjectCategory.ExternalIntegration
        };
        public static List<ProjectCategory> categoriesContractor = new List<ProjectCategory>(){
            ProjectCategory.Contractor
        };

        public static List<ProjectCategory> categoriesConstruction = new List<ProjectCategory>(){
            ProjectCategory.Construction
        };      

        public static List<ProjectCategory> categoriesBudget = new List<ProjectCategory>(){
            ProjectCategory.Budget, ProjectCategory.Funding
        };


        public void CreateInspector()
        {
            InspectorGroups.Clear();
            _iconButtonRowModel = new IconButtonRowModel();
            _iconButtonRowModel.Add(new IconButtonModel("Ui/Sprites/Common/IconButtonContracts", delegate (IconButtonModel x)            {
                SetGroupsVisible(categoriesBudget);
            }, "Show Funding"));
            _iconButtonRowModel.Add(new IconButtonModel("Ui/Sprites/MapView/IconAutoBurn", delegate (IconButtonModel x)
            {
                SetGroupsVisible(categoriesDevelopment);
            }, "Show Developments"));
            _iconButtonRowModel.Add(new IconButtonModel("Ui/Sprites/Design/IconButtonViewSide", delegate (IconButtonModel x)
            {
                SetGroupsVisible(categoriesIntegration);
            }, "Show Integrations"));
            _iconButtonRowModel.Add(new IconButtonModel("Ui/Sprites/PlanetStudio/IconAddSubStructure", delegate (IconButtonModel x)            {
                SetGroupsVisible(categoriesContractor);
            }, "Show Contractors"));
            _iconButtonRowModel.Add(new IconButtonModel("Ui/Sprites/PlanetStudio/IconAddLaunchLocation", delegate (IconButtonModel x)            {
                SetGroupsVisible(categoriesConstruction);
            }, "Show Construction"));
            


            _inspectorModel = new InspectorModel("Projects", "Projects");
            _ttnc = _inspectorModel.Add(new TextModel("Next Completion", () => Units.GetRelativeTimeString(_data.TimeUntilNextCompletion)));
            _inspectorModel.Add(new ToggleModel("Time Warp", ()=> _warp, (f) => SetWarp(f)));
            _inspectorModel.Add(new TextModel("Money", () => Units.GetMoneyString(Career.Money)));
            _inspectorModel.Add(new TextModel("Daily Balance", () => BalancePerDayString));
            _inspectorModel.Add(new TextButtonModel("Repeat Integration", OnRepeatIntegrationClicked,null,()=>_hm.DB.CraftConfigs.Count() > 0));
            _inspectorModel.Add(new TextButtonModel("External Integration", OnExternalIntegrationClicked,null,()=> Career.TechTree.GetItemValue("Cheats.SpaceRaceCheats")?.ValueAsBool ?? false));
            _inspectorModel.Add(new TextButtonModel("More Money", GiveMagicMoney,null,()=> Career.TechTree.GetItemValue("Cheats.SpaceRaceCheats")?.ValueAsBool ?? false));
            _inspectorModel.Add(new TextButtonModel("Unlock Staffing", OnUnlockStaffingClicked));
            _inspectorModel.Add(new TextModel("Available Technicians", () => _data.AvailableTechnicians.ToString()));
            _inspectorModel.Add(new TextModel("Total Technicians", ()=> _data.TotalTechnicians.ToString(), null, null, ()=> !_editingStaff));
            _inspectorModel.Add(new TextModel("Total Astronauts", ()=> _gs.Crew.Members.Count(member => member.State != CrewMemberState.Deceased).ToString(), null, null, ()=> !_editingStaff && _gs.Validator.ItemValue("Crew")>0F));
            _inspectorModel.Add(new SliderModel("Total Technicians", ()=> _data.TotalTechnicians, UpdateTechnicians, 0, _data.MaxTechnicians, true){
                ValueFormatter = (f) => ((int)f).ToString(),
                DetermineVisibility = () => _editingStaff,
            });
            _inspectorModel.Add(new SliderModel("Total Astronauts", ()=> _gs.Crew.Members.Count(member => member.State != CrewMemberState.Deceased), UpdateAstronauts, 0, _gs.Validator.ItemValue("Crew"), true){
                ValueFormatter = (f) => ((int)f).ToString(),
                DetermineVisibility = () => _editingStaff && _gs.Validator.ItemValue("Crew")>0F,
            });
            
            _inspectorModel.Add(_iconButtonRowModel);

            foreach (int i in ActiveChildren.Keys)
            {
                InspectorGroups[i] = _inspectorModel.AddGroup(ActiveChildren[i].GetGroupModel());
            }
            InspectorPanelCreationInfo ipci = new InspectorPanelCreationInfo()
            {
                PanelWidth = 400,
                Resizable = true,
            };
            _panel = Game.Instance.UserInterface.CreateInspectorPanel(_inspectorModel, ipci);
            SetGroupsVisible(_data.VisibleInspectorGroups);
            //_panel.Closed += (e) => _panel = null;
        }
        /// <summary>
        /// Toggles the program inspector panel
        /// </summary>
        public void ToggleProgramPanel()
        {
            try
            {
                _panel.Visible = !_panel.Visible;
            }
            catch
            {
                CreateInspector();
            }
        }

        public void CloseProgramManager()
        {
            Game.Instance.SceneManager.SceneLoaded -= OnSceneLoaded;
            _hm?.RemoveAllDesignerParts();
        }

        public void OnRepeatIntegrationClicked(TextButtonModel model)
        {
            if (Game.InFlightScene) Game.Instance.GameState.Career.OnFlightStart(FlightSceneScript.Instance, true);  
            Game.Instance.UserInterface.CreateListView(new PreviousCraftViewModel(_hm.DB.CraftConfigs.Where(config => config.Active).ToList(),RenameRepeatCraft,this));
        }

        public void OnExternalIntegrationClicked(TextButtonModel model)
        {
            if (Game.InFlightScene) Game.Instance.GameState.Career.OnFlightStart(FlightSceneScript.Instance, true);  
            Game.Instance.UserInterface.CreateListView(new PreviousCraftViewModel(_hm.DB.CraftConfigs.Where(config => config.Active).ToList(),QuickExternal,this));
        }
        public void GiveMagicMoney(TextButtonModel model)
        {
            Career.ReceiveMoney(Career.Money < 0 ? -2L*Career.Money : Career.Money);
            _spaceCenterUi?.UpdateStats();
        }
        public void OnUnlockStaffingClicked(TextButtonModel model)
        {
            _editingStaff = !_editingStaff;
            model.Style = _editingStaff? ButtonModel.ButtonStyle.Primary : ButtonModel.ButtonStyle.Default;
        }

        public void QuickExternal(SRCraftConfig config)
        {
            NewExternalIntegration(config, "test", 30 ,20.0);
        }

        private void CorrectAvailableTechnicians()
        {
            _data.AvailableTechnicians = _data.TotalTechnicians - Integrations.Values.Sum(integ => integ.Data.Technicians);
        }
        public void UpdateTechnicians(float f)
        {
            int prev = _data.TotalTechnicians;
            if (f < _data.TotalTechnicians - _data.AvailableTechnicians)
            {
                _data.TotalTechnicians -= _data.AvailableTechnicians;
            }
            else if (f > _data.MaxTechnicians || f > _data.TotalTechnicians + Career.Money/SRFormulas.TechHire)
            {
                _data.TotalTechnicians = math.min(_data.MaxTechnicians, _data.TotalTechnicians + math.max((int)(Career.Money/SRFormulas.TechHire),0));
            }
            else 
            {
                _data.TotalTechnicians = (int)f;
            }
            _data.AvailableTechnicians += _data.TotalTechnicians - prev;
            if (_data.TotalTechnicians > prev)
            {
                Career.SpendMoney(SRFormulas.TechHire * (_data.TotalTechnicians-prev));
            }
            Budget.UpdateRates(false);
            if (_spaceCenterUi != null) _spaceCenterUi.UpdateStats();
        }

        public void UpdateAstronauts(float f)
        {
            int prev = _gs.Crew.Members.Where(x => x.State != CrewMemberState.Deceased).Count();
            if (f< _gs.Crew.Members.Where(x => x.State != CrewMemberState.Available).Count())
            {
                f = _gs.Crew.Members.Where(x => x.State != CrewMemberState.Available).Count();
            }
            else if (f > _gs.Validator.ItemValue("Crew") || f > prev + Career.Money/SRFormulas.AstroHire)
            {
                f = math.min(_gs.Validator.ItemValue("Crew"), prev + math.max((int)(Career.Money/SRFormulas.AstroHire),0));
            }
            int next = (int)f;
            if (next > prev)
            {
                Career.SpendMoney(SRFormulas.AstroHire * (next-prev));
                HireAstronauts(next-prev);
            }
            if (next < prev)
            {
                RemoveAstronauts(prev - next);
            }
            Budget.UpdateRates(false);
            if (_spaceCenterUi != null) _spaceCenterUi.UpdateStats();
        }

        private void RemoveAstronauts(int num)
        {
            List<CrewMember> fire = new List<CrewMember>(_gs.Crew.Members.Where(mem => mem.State == CrewMemberState.Available).ToList().Take(num));
            foreach(CrewMember crew in fire)
            {
                crew.State = CrewMemberState.Deceased;
            }
        } 

        private void HireAstronauts(int num)
        {
            int j = _gs.Crew.Members.Count()-2;
            CrewMember member;
            for (int i =0; i < num; i ++)
            {
                member = _gs.Crew.CreateCrewMember();
                if (SRFormulas.AstroNames.Count > j+i)
                {
                    member.Name = SRFormulas.AstroNames[j+i];
                }
            }
        } 
        public void RenameRepeatCraft(SRCraftConfig config)
        {
            ModApi.Ui.InputDialogScript script = Game.Instance.UserInterface.CreateInputDialog(Game.Instance.UserInterface.Transform);
            script.MessageText = "Would you like to rename this craft?";
            script.InputText = config.Name;
            script.InputPlaceholderText = "Craft name";
            script.OkayClicked += dialog =>
            {
                dialog.Close();
                RepeatIntegration(config, dialog.InputText);
            };
        }
        public void ReplaceInspectorGroup(int id)
        {
            if (_panel != null)
            {   
                if (InspectorGroups.TryGetValue(id, out GroupModel group))
                {
                    int j = _panel.Model.IndexOfGroup(group);
                    _panel.ReplaceGroup(group, ActiveChildren[id].GetGroupModel());
                    InspectorGroups[id] = _panel.Model.Groups[j];
                }
                else 
                {
                    InspectorGroups[id]=_panel.Model?.AddGroup(ActiveChildren[id].GetGroupModel());
                    ReplaceInspectorGroup(0);
                    _panel.RebuildModelElements();
                }
                InspectorGroups[id].Visible = _data.VisibleInspectorGroups.Contains(ActiveChildren[id].Category);
            }
        }
        public void SetWarp(bool value)
        {
            if (Game.InFlightScene)
            {
                if (value && _data.TimeUntilNextCompletion != double.PositiveInfinity)
                {
                    int i = 0;
                    string failReason;
                    foreach (var timeMode in fs.TimeManager.Modes)
                    {
                        if (_data.TimeUntilNextCompletion / timeMode.TimeMultiplier < 10.0 || timeMode == fs.TimeManager.Modes.Last())
                        {
                            if (Career.Money < 0L && CostPerDay > 0L)
                            {
                                failReason ="You have negative cashflow and no reserves to draw upon.  Attain positive cashlow before engaging timewarp.";
                            }
                            else if (fs.TimeManager.CanSetTimeMultiplierMode(i, out failReason))
                            {
                                _warp = true;
                                fs.TimeManager.SetMode(timeMode, false);
                                return;
                            }
                            fs.FlightSceneUI.ShowMessage(failReason);
                            break;
                        }
                        i++;
                    }
                }
                fs.TimeManager.SetMode(fs.TimeManager.RealTime, false);
            }
            _warp = false;
        }
        public string UpdateCapacities()
        {
            string reports = string.Empty;
            foreach (ContractorScript contractor in Contractors.Values)
            {
                if (contractor.Data.AverageRecentRush > 0.2)
                {
                    contractor.Data.BaseProductionRate *= 1.0 + contractor.Data.AverageRecentRush * (1.0 + contractor.Data.Attitude.Optimism);
                    reports += $"{contractor.Name} has increased base capacity by {Units.GetPercentageString((float)contractor.Data.AverageRecentRush)} in response to the rush orders they received this year.\n";
                }
                else if (contractor.Data.AverageRecentInventory > 0.5)
                {
                    double factor = 1.0 - 0.5 * math.min(1, contractor.Data.AverageRecentInventory) * (1.0 - 0.6 * (double)contractor.Data.Attitude.Optimism);
                    contractor.Data.BaseProductionRate *= factor;
                    contractor.Data.Inventory = math.max(contractor.Data.Inventory, 0.99 * contractor.Data.InventoryLimit);
                    contractor.Data.AverageRecentInventory = 0.0;
                    reports += $"{contractor.Name} has reduced base capacity by {Units.GetPercentageString((float)factor)} in response to high levels of unused inventory.\n";
                }
            }
            return reports;
        }
    }

    public delegate void SpaceProgramDelgate(IProgramManager pm);


}



