namespace Assets.Scripts.SpaceRace.Projects
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Xml;
    using System.Xml.Serialization;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Collections;
    using System;
    using Assets.Scripts.SpaceRace.Formulas;
    using System.Linq;

    public enum ProjectCategory
    {
        Part,
        Stage,
        Integration,
        ExternalIntegration,
        Contractor,
        Construction,
        Funding,
        Budget
    }

    public class ProjectData
    {
        [XmlAttribute]
        public int Id;
        [XmlAttribute]
        public string Name = "New Project";
        [XmlAttribute]
        public int ContractorId = 0;
        [XmlAttribute]
        public bool Completed = false;
        [XmlAttribute]
        public bool Active = true;
        public virtual double TimeToCompletion => Formulas.SRFormulas.SecondsPerDay * (TotalProgress - CurrentProgress) / ProgressPerDay;
        [XmlAttribute]
        public double TotalProgress;
        [XmlAttribute]
        public double CurrentProgress = 0;
        [XmlAttribute]
        public double BaseProgressPerDay; 
        public virtual double ProgressPerDay => BaseProgressPerDay * Efficiency * Formulas.SRFormulas.RushRate(Rush);
        [XmlAttribute]
        public long TotalPrice => (long)(PricePerDay * TotalProgress / ProgressPerDay);
        public virtual long PricePerDay => (long)(BaseProgressPerDay * Formulas.SRFormulas.RushPrice(Rush));
        [XmlAttribute]
        public bool Rush = false;
        public TechList Technologies = new TechList();
        [XmlIgnore]
        public List<EfficiencyFactor> EfficiencyFactors = new List<EfficiencyFactor>();  
        [XmlAttribute]
        public double Efficiency = 1;
    }



    public class DevelopmentData : ProjectData
    {
        [XmlAttribute] 
        public double ProductionCapacity = 0;
        [XmlAttribute] 
        public bool Delayed = false;
    }
    public class PartDevelopmentData : DevelopmentData 
    {
        [XmlAttribute]
        public string PrimaryMod;
        [XmlAttribute]
        public int PartId;
        [XmlAttribute]
        public int PartFamilyId;
        
    }

    public class StageDevelopmentData : DevelopmentData
    {
        [XmlAttribute]
        public List<int> StageIds = new List<int>();  
    }
}