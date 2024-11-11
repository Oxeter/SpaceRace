namespace Assets.Scripts.SpaceRace.Projects
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Serialization;
    using Assets.Scripts.SpaceRace.Formulas;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.Modifiers;
    using ModApi.Craft.Parts;
    using Assets.Scripts.SpaceRace.Hardware;    
    using UnityEngine;
    using ModApi.Ui.Inspector;
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using ModApi;
    using Unity.Mathematics;

    public abstract class DevBid
    {
        /// <summary>
        /// The programmanager.  The partdata class should pass the programmanager in the constructor
        /// </summary>
        protected IProgramManager _pm;
        public string Name;
        public abstract ProjectCategory Category {get;}
        public int ContractorId;
        public long TotalProjectedCost => (long)(DevCostPerDay * ProjectedDevelopmentTime * Formulas.SRFormulas.DaysPerSecond);  
        /// <summary>
        /// The technologies.
        /// </summary>
        public TechList Technologies {get; set;}
        /// <summary>
        /// Comments.  These will be shown in the bid listview.
        /// </summary>
        public List<string> Comments {get; set;}
        /// <summary>
        /// The base time to complete development in seconds
        /// </summary>
        public double BaseDevTime {get; set;}
        public IContractorScript Contractor => _pm.Contractor(ContractorId);
        public List<EfficiencyFactor> EfficiencyFactors => Technologies
                .Select(tech => _pm.GetEfficiencyFactor(tech, ContractorId, Category))
                .Where(y => y!= null)
                .ToList();
        public double ProjectedDevelopmentTime => BaseDevTime * (1+ math.max(EfficiencyFactors.Sum(fac => fac.Penalty), -0.99));

        public long DevCostPerDay {get; set;} 
        public string Sprite => _pm.Contractor(ContractorId).Data.Sprite;
        public double Markup = 1.0;
        public double ProductionCapacity = 0.0;
        public double NewProductionCapacity => ProductionCapacity - Contractor.Data.BaseProductionRate;
        public abstract double ProductionRequired {get;}
        public long ProductionCost => (long)(ProductionRequired * Markup);
        public double ProjectedProductionTime => ProductionRequired * Formulas.SRFormulas.SecondsPerDay/ ProductionCapacity;
        public double ProjectedProductionTimeNew => ProductionRequired * Formulas.SRFormulas.SecondsPerDay/ NewProductionCapacity;
    }

    public class PartDevBid : DevBid
    {
        public override ProjectCategory Category {get {return ProjectCategory.Part;}}
        /// <summary>
        /// Passed to the costcalculator to tell it that contractors should be making these parts faster (because more are needed on a typical craft).
        /// Currently only used for reacton control thrusters.
        /// </summary>
        public int BatchSize = 1;
        public string DesignerCategory{get; private set;} = string.Empty;
        public string DesignerDescription = string.Empty;
        public string FamilyName;
        /// <summary>
        /// Partmods should pass themselves.
        /// </summary>
        public ISRPartMod PartMod{get; private set;}
        /// <summary>
        /// The number of production units that the contractor must amass to build this part
        /// </summary>
        public double UnitProductionRequired {get; set;}
        public override double ProductionRequired  => UnitProductionRequired;
        /// <summary>
        /// inaccessible
        /// </summary>
        private PartDevBid(){}
        /// <summary>
        ///  Construct a new bid for the listview
        /// </summary>
        /// <param name="pm">The program manager</param>
        /// <param name="mod">The partmod (the mod should use "this")</param>
        /// <param name="contractorid">The contractor ID</param>
        /// <param name="name">Override the name of the part</param>
        /// <param name="familyname">Override the familyname if one must be generated</param>
        public PartDevBid(IProgramManager pm, ISRPartMod mod, string designerCategory = null){
            _pm = pm;
            PartMod = mod;
            DesignerCategory = designerCategory ?? mod.DesignerCategory;
        }
    }

    public class StageDevBid : DevBid
    {
        public override ProjectCategory Category {get {return ProjectCategory.Stage;}}

        private StageDevBid(){}
        /// <summary>
        /// Constructs a new stagedevbid
        /// </summary>
        /// <param name="pm">The project manager</param>
        /// <param name="contractorId">The contractor id</param>
        /// <param name="name">Optional override of the name of the project</param>
        public StageDevBid(IProgramManager pm, int contractorId, string name = null){
            _pm = pm;
            ContractorId = contractorId;
            Name = name;
            Comments = new List<string>();
        }
        public List<long> ProductionCosts => ProductionsRequired.Select(prod => (long)(Markup * prod)).ToList();
        /// <summary>
        /// The number of production units that the contractor must amass to build each stage
        /// </summary>
        public List<double> ProductionsRequired;
        public string ProductionsDescription;
        public override double ProductionRequired => ProductionsRequired.Sum();
    }
}

