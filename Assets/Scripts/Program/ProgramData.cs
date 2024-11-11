namespace Assets.Scripts.SpaceRace.Projects
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Xml;
    using System.Xml.Serialization;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.History;
    using ModApi.State;
    using System.Xml.Linq;

    public class ProgramData
    {
        [XmlAttribute]
        public int NextId = 1;
        [XmlAttribute]
        public int IntegrationsPrioritized = 0;
        [XmlAttribute]
        public double LastUpdateTime = 0.0;
        [XmlAttribute]
        public int LastLaunchConfig = 0;
        [XmlAttribute]
        public double TimeUntilNextCompletion = double.PositiveInfinity;
        [XmlAttribute]
        public int AvailableTechnicians = 10;
        [XmlAttribute]
        public int TotalTechnicians = 10;
        [XmlAttribute]
        public int MaxTechnicians = 20;
        public List<int> ContractCrewsProvided = new List<int>();
        public List<ProjectCategory> VisibleInspectorGroups = new List<ProjectCategory>(){ProjectCategory.Funding, ProjectCategory.Budget};
        public TechOccurenceList PartTechDeveloped = new TechOccurenceList();
        public TechOccurenceList StageTechDeveloped = new TechOccurenceList();
        public TechOccurenceList TechIntegrated = new TechOccurenceList();
        /// <summary>
        /// List of pairs (CraftNodeId, SRCraftConfig.Id).  Can be used to look up original configuration of a craft node (for recovery).
        /// </summary>
        public List<(int,int)> Flights = new List<(int, int)>();
    }

    public class ProgramDB
    {
        public ProgramData Pdata = new ProgramData();
        public List<ContractorData> Contractors = new List<ContractorData>();
        public List<IntegrationData> Integrations = new List<IntegrationData>();
        public List<ExternalIntegrationData> ExternalIntegrations = new List<ExternalIntegrationData>();
        public List<StageDevelopmentData> StageDevelopments = new List<StageDevelopmentData>();
        public List<PartDevelopmentData> PartDevelopments = new List<PartDevelopmentData>();
        public List<ConstructionData> ConstructionProjects = new List<ConstructionData>();
        public List<PersistantFundingData> Funding = new List<PersistantFundingData>();
        public SpaceRaceContractsData ContractsData = new SpaceRaceContractsData();
    }

    
}