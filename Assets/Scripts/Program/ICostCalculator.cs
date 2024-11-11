namespace Assets.Scripts.SpaceRace.Projects
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Serialization;
    using Assets.Scripts.SpaceRace.Formulas;
    using Assets.Scripts.SpaceRace.Collections;
    using UnityEngine;
    using ModApi.Ui.Inspector;
    using System;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Modifiers;
    using ModApi.Craft.Parts;

    public interface ICostCalculator
    {
        public List<StageDevBid> GetStageBids(List<SRStage> stages, List<IContractorScript> contractors, TechOccurenceList techlist, SRStage previous = null, SRStage following= null);
        public List<PartDevBid> GetPartDevBids(PartData part, ISRPartMod mod, List<IContractorScript> contractors, Technology famtech);
        public int GetMaxTechnicians(SRCraftConfig config);
        public double GetIntegrationProduction(SRCraftConfig config, ref string desc);
        public long GetIntegrationCost(SRCraftConfig config);
        public double GetIntegrationRollout(SRCraftConfig config);
        public double GetIntegrationRepair(SRCraftConfig config);
        public virtual List<PartDevBid> AdjustForMarketForces(List<PartDevBid> bids)
        {
            return bids;
        }

        public virtual List<StageDevBid> AdjustForMarketForces(List<StageDevBid> bids)
        {
            return bids;
        }
        public virtual List<EfficiencyFactor> IntegrationEfficiencyFactors(SRCraftConfig config)
        {
            return new List<EfficiencyFactor>();
        }
        public (float, float) GetDevelopmentDelay(List<EfficiencyFactor> factors, ContractorAttitude attitude);
        public double RefurbishProgress(Hardware hardware, ContractorData contractor);
    }
}