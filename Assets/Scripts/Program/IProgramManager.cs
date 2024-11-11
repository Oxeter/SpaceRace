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
    using ModApi.State;
    using ModApi.Flight;
    using ModApi.Scenes.Events;
    using ModApi.Flight.Events;
    using Assets.Scripts.SpaceRace.History;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Modifiers;
    using Assets.Scripts.Career.Contracts;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.State;
    using ModApi.Craft;
    using Assets.Scripts.SpaceRace.UI;
    using Assets.Scripts.Flight.Sim;

    public interface IProgramManager
    {
        public void Load();
        public void Save(string id);
        public GameState GameState {get; set;}
        public ProgramData Data {get;}
        public IHardwareManager HM {get;}
        public CareerState Career {get;}
        public long CostPerDay {get;}
        public string BalancePerDayString {get;}
        public int AvailableTechnicians {get; set;}
        public int LaunchingId {get; set;}
        public SRCraftConfig LastCraftConfig {get; set;}
        public bool ContractsChanged {get;}
        public void GoToSpaceCenter(bool destroy = false);
        public void SwitchToNearestSpaceCenter(LaunchLocation location);
        public void PreviousSpaceCenter();
        public void NextSpaceCenter();
        public SpaceCenterUiController SpaceCenterUi {get; set;}
        public ICostCalculator CostCalculator {get;}
        public HistoryManager History {get;}
        public IContractorScript Contractor(int i);
        public Dictionary<int,IProgramChild> ActiveChildren {get;}
        //public GroupModel GModel(int i);
        public IIntegrationScript Integration(int i);
        public Dictionary<int, IntegrationScript> Integrations {get;}
        public Dictionary<int,ExternalIntegrationScript> ExternalIntegrations {get;}
        public Dictionary<int,FundingScript> Funding {get;}
        public BudgetScript Budget {get;}   
        public CraftDesigns Designs {get;}    
        public void SetWarp(bool value);
        public bool IsWarp {get;}
        /// <summary>
        /// Adds a contractor.
        /// </summary>
        /// <param name="contractorData">The contractor data</param>
        /// <returns>The Id of the contractor</returns>
        public int AddContractor(ContractorData contractorData);
        /// <summary>
        /// Adds a hardware type to the matching contractors.
        /// </summary>
        /// <param name="type">The type (usually in the form mod.partmod)</param>
        /// <param name="predicate">A predicate to determine which contractors will add the type</param>
        public void AddTypeToContractors(string type, Predicate<ContractorScript> predicate);
        /// <summary>
        /// Called after the program manager does not find saved data and loads the career starting data.
        /// </summary>
        public event SpaceProgramDelgate NewSpaceProgram;
        public void ActivateContractor(int i);
        public PropertyOccurenceList RequiredCrew(Contract contract);
        public void OnContractCompleted(Contract contract);
        public void OnContractAccepted(Contract contract);
        public void OnContractCancelled(Contract contract);
        public void EndProject(int i);
        public void CheckForLaunch();
        public void UpdateAllRates(bool efficiencies);
        public void SetNextCompletion(double time, IProgramChild child);
        public void ReplaceInspectorGroup(int id);
        public void OnSceneLoaded(object sender, SceneEventArgs e);
        public VerboseCraftConfiguration GetVerboseCraftConfig(ICommandPod commandPod, ModApi.Scripts.State.Validation.ValidationResult vr = null);
        
        public Dictionary<string,string> GetDevPanelProperties(PartData part, string primarymod);
        public void NewStageDevelopment(List<SRStage> stages, string devname, StageDevBid offer);
        public void NewPartDevelopment(PartData part, PartDevBid offer);

        /// <summary>
        /// Update programdata to reflect that a tech has been developed
        /// </summary>
        /// <param name="tech">The tech name</param>
        /// <param name="contractorid">The contractor's dI</param>
        
        public void NewIntegrationFromDesigner(VerboseCraftConfiguration vconfig, string name);
        public void NewExternalCraftOrder(SRCraftConfig config, string name, int ordered, double daysper);
        public void RepeatIntegration(SRCraftConfig config, string name, CraftNode recovery= null,  LaunchLocation location = null);
        public void DevelopTech(Technology tech, int contractorid, ProjectCategory category);

        /// <summary>
        /// How many different contractors have developed this tech (max 2)
        /// </summary>
        /// <param name="tech">The tech name</param>
        /// <returns>The number of contractors (max 2)</returns>
        public int TimesDeveloped(Technology tech, ProjectCategory category);
        /// <summary>
        /// Register a part mod so that parts with this mod become part of the SpaceRace development process
        /// </summary>
        /// <param name="data">A PartData class that implements the ISRPartMod interface</param>
        public void RegisterPartModData(ISRPartMod data);
        public List<StageDevBid> GetStageBids(List<SRStage> stages);
        public List<PartDevBid> GetPartBids(PartData part);
        public ISRPartMod GetPrimarySpaceRaceMod(PartData part);
        public string GetPrimarySpaceRaceModName(PartData part);
        public string GetPrimarySpaceRaceModName(XElement partXml);
        public XElement GetData(XElement partXml, string primarysrmod);
        public XElement GetFamData(PartData part, string primanymodname);
        public XElement GetFamData(XElement partXml, string primarysrmod);
        public XElement GetData(PartData part, string primarysrmod);
        public TechList GetTechList(PartData part, string primarysrmod, bool family = false);
        public double GetFamilyStageCost(PartData part, string primarysrmod);
        //public EfficiencyFactor GetEfficiencyFactor(string tech, int contractorId, string primarymod, ProjectCategory category);
        /// <summary>
        /// Get efficiencyfactors for stages and integrations (where the primary mod name is saved as a prefix)
        /// </summary>
        /// <param name="tech">The tech, in the form primarymod:techname</param>
        /// <param name="contractorId">The contractor ID</param>
        /// <param name="category">the type of project</param>
        /// <returns>The efficiencyfactor</returns>
        public EfficiencyFactor GetEfficiencyFactor(Technology tech, int contractorId, ProjectCategory category);
        public int Prioritize();
        public void CloseProgramManager();
        public void ToggleProgramPanel();

        public bool LocationAvailable(string loc);
        public void ReserveLaunchLocation(string loc);
        public void ReleaseLaunchLocation(string loc);
        public bool HighlightLaunchButton {get;}
        public void OnClickCrewButton();
        public void SendCrew(Contract contract);
        public string UpdateCapacities();
    }
}