namespace Assets.Scripts.SpaceRace.Hardware
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using UnityEngine;

    using ModApi.Craft.Parts;
    using Assets.Scripts.SpaceRace.Projects;
    using Assets.Scripts.SpaceRace.Collections;

    public interface IHardwareManager
    {   
        HardwareDB DB {get;}

        public Dictionary<int, SRPart> Parts {get; set;}

        public Dictionary<int, SRPartFamily> Families {get; set;}
        public Dictionary<int, SRStage> Stages {get; set;}

        public Dictionary<int, SRCraftConfig> Craft {get; set;}

        public Dictionary<int, Hardware> Hardware {get; set;}
        public void Initialize();
        public void RemoveAllDesignerParts();
        /// <summary>
        /// Gets a text description of the object in the database with id i
        /// </summary>
        /// <param name="i"> the id </param>
        /// <returns> a description string </returns>
        public abstract string Description(int i);
        public abstract string Description(HardwareOccurencePair hop);
        public abstract string Description(HardwareOccurenceList hol);
        public abstract T Get<T>(Predicate<T> condition) where T : class;
        public abstract SRCraftConfig GetCraftConfig(Predicate<SRCraftConfig> match);

        public abstract double SumOverCraftConfigs(Func<SRCraftConfig, double> selector);
        /// <summary>
        /// Returns the id of a part with matching xml in the database
        /// </summary>
        /// <param name="newPart"></param>
        /// <returns>Id if a match is found, 
        /// 0 if not, 
        /// -1 if this is not the type of part that the database contains</returns>
        public abstract int GetPartId(PartData newPart);
        /// <summary>
        /// Returns the id of a part with matching xml in the database
        /// </summary>
        /// <param name="newPart"></param>
        /// <returns>Id if a match is found, 
        /// 0 if not, 
        /// -1 if this is not the type of part that the database contains</returns>
        public abstract int GetPartId(XElement newPart);
        public abstract void DevelopId(int i);
        public abstract void IncrementCraftIntegrations(int i);
        public float GetStartingSimilarIntegrations(SRCraftConfig config);
        public int AddStandaloneFamily(SRPartFamily fam);
        public abstract int AddStage(SRStage stage);
        public string GetDescription(HardwareOccurenceList list);
        public abstract SRCraftConfig AddCraftConfig(VerboseCraftConfiguration vconfig, string name);
        public abstract SRPart AddPart(PartData part, PartDevBid offer);
        public abstract void MultiplyUnitCost(int id, float overrun);
        public abstract SRStage NewStage(string craftname, int number, TechList techs, FamilyOccurenceList fol);
        public void DeletePart(int i);
        public void DeleteStage(int i);
    }

}