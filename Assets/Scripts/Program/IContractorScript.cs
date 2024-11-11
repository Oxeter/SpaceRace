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
    using ModApi.Craft.Parts;

    public interface IContractorScript
    {
        public ContractorData Data {get;}
        public int Id {get;}
        public bool Active {get;}
        public int ActiveDevelopments {get;}
        public void SetBaseProductionRate(double rate);
        public abstract bool DevelopTech(string mod, string tech);
        public abstract bool DevelopTech(Technology tech);
        /// <summary>
        /// Has this tech been developed by this contractor?
        /// </summary>
        /// <param name="tech">the tech</param>
        /// <returns>true if it has been developed</returns>
        public abstract bool IsDeveloped(string mod, string tech);
        public abstract bool IsDeveloped(TechOccurenceTriple triple);
        public void PlaceOrder(HardwareOrder order);
        public void UpdateRates(bool efficiencies);


        public void CancelOrder(int i);
    }

}