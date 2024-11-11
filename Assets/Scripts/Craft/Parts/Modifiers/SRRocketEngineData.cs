using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using ModApi.Common.Attributes;
using ModApi.Craft.Parts;
using ModApi.Design.PartProperties;
using ModApi.Craft.Parts.Attributes;
using UnityEditor;
using UnityEngine;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using HarmonyLib;
using Assets.Scripts.SpaceRace.Hardware;
using Assets.Scripts.SpaceRace.Projects;
using Assets.Scripts.SpaceRace.Collections;
using ModApi;
using ModApi.Math;
using Unity.Mathematics;
using Assets.Scripts.SpaceRace.Formulas;
using ModApi.Craft.Parts.Modifiers.Propulsion;


namespace Assets.Scripts.SpaceRace.Modifiers
{

    [Serializable]
    [DesignerPartModifier("Engine Capabilities")]
    [PartModifierTypeId("SpaceRace.SRRocketEngine")]
    public class SRRocketEngineData : PartModifierData<SRRocketEngineScript>, ISRPartMod
    {
        /// <summary>
        /// This is only used for the instance registered to the program manager.  
        /// It will be null for actual parts.
        /// </summary>
        private IProgramManager _pm = null;
        public string DesignerCategory {get {return "Propulsion";}}
        public SRRocketEngineData(){}

        public SRRocketEngineData(IProgramManager pm)
        {
            _pm=pm;
        }
        private int _srpriority = 0;
        public int SRPriority {get { return _srpriority;}}

        [SerializeField]
        [DesignerPropertySlider(MinValue = 0, MaxValue = 1, NumberOfSteps = 21, Label = "Throttle Down Capability", Tooltip = "How far the engine can throttle down from full power")]
        private float _throttleDown = 0F;
           
        [SerializeField]
        [DesignerPropertyToggleButton()]
        private bool _airLight = false;

        [SerializeField]
        [DesignerPropertySlider(MinValue = 1, MaxValue = 21, NumberOfSteps = 20, Label = "Number of Ignitions", Tooltip = "How many times the engine can ignite (21 = infinite)")]      
        private int _maxIgnitions = 1;



  
        public float FailureFactor => (this as ISRPartMod).HardwareFailures? 1F : 0F;

        /// <summary>
        /// Gets the throttle down capability.
        /// </summary>
        /// <value>
        /// The throttle down capability (0 = none, 1= complete)
        /// </value>
        [XmlIgnore]
        public float ThrottleDown
        {
            get
            {
                return _throttleDown;
            }
        }
        /// <summary>
        /// Gets the maximum ignitions of the engine.
        /// </summary>
        /// <value>
        /// The number of ignitions (-1 = unlimited)
        /// </value>
        public int MaxIgnitions
        {
            get
            {
                return _maxIgnitions > 20? -1 : _maxIgnitions;
            }
        }

        public bool AirLight
        {
            get
            {
                return _airLight;
            }
        }

        public bool Failing
        {
            get
            {
                return _failing;
            }
        }

        [SerializeField]
        public double BurnTime;


        [SerializeField]
        private bool _failing = false;   


        [SerializeField]
        private int _ignitionsChecked = 0;
        public int IgnitionsChecked {get {return _ignitionsChecked;} set {_ignitionsChecked = value;}}
        [SerializeField]
        public int CycleIntegreations {get; private set;} =0;
        [SerializeField]
        public int NozzleIntegreations {get; private set;} =0;
        [SerializeField]
        public int FamilyIntegreations {get; private set;} =0;
        [SerializeField]
        public float MaxPressure {get; private set;} =0;
        public override float MassDry => _mass - Part.GetModifier<RocketEngineData>().MassDry;
        public override long Price => (long)(Part.GetModifier<RocketEngineData>().Price * (0.8F/Part.GetModifier<RocketEngineData>().UserChamberPressure -0.8F));
        /// <summary>
        /// The engine mass, depending only on size, type and nozzle.  The modifer reports the difference between this number and the value produced by RocketEngineData.
        /// </summary>
        private float _mass;
        public void UpdateInfo(IProgramManager pm)
        {
            RocketEngineData mod = Part.GetModifier<RocketEngineData>();
            if (mod.EngineTypeId != "Liquid") _airLight = true;
            if (mod.EngineTypeId == "Solid") _maxIgnitions = 1;
            _mass = (0.35f + 0.65f * mod.UserChamberPressure) * mod.Size * mod.Size * mod.EngineType.MassScale * 9F + 0.01F * (mod.EngineType.BaseMass + mod.NozzleType.CalculateMass(mod.Size, mod.ExtensionSize)); 
            if (Game.IsCareer)
            {
                TechOccurenceList integ = pm.Data.TechIntegrated;
                RocketEngineData red = Part.GetModifier<RocketEngineData>();
                int famid = pm.HM.Get<SRPartFamily>(fam => XNode.DeepEquals(fam.Data,pm.GetFamData(Part, TypeId)))?.Id ?? 0;
                TechList partTech = Technologies(Part);
                CycleIntegreations = integ.Occurences(partTech.First(t => t.Tech.Split(":")[0] == "Cycle"));
                NozzleIntegreations = integ.Occurences(partTech.First(t => t.Tech.Split(":")[0] == "Nozzle"));
                FamilyIntegreations = integ.Occurences(TypeId, string.Format("PartFamily:{0:D}",famid));
                MaxPressure = pm.Career.TechTree.GetItemValue("RocketEngine.MaxChamberPressure").ValueAsFloat;
                Script.SetReliability(pm);
            }
        }
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            base.OnDesignerInitialization(d);
            d.OnPropertyChanged(() => _maxIgnitions, (x,y) => {
                Traverse.Create(Part.GetModifier<RocketEngineData>()).Field("_ignitionsOverride").SetValue(x > 20 ? -1: x);
                _airLight = true;
                d.Manager.RefreshUI();
                }); 
            d.OnPropertyChanged(() => _throttleDown, (x,y) => Traverse.Create(Part.GetModifier<RocketEngineData>()).Field("_minThrottleOverride").SetValue(1F-x)); 
            d.OnPropertyChanged(() => _airLight, (x,y) => {
                if (y && Part.GetModifier<RocketEngineData>().EngineTypeId != "Liquid") 
                {
                    _airLight = true;
                }
                else if (!x)
                {
                    _maxIgnitions = 1;
                    Traverse.Create(Part.GetModifier<RocketEngineData>()).Field("_ignitionsOverride").SetValue(1);
                }
                UpdateInfo(SRManager.Instance.pm);
                d.Manager.RefreshUI();
                });
            UpdateInfo(SRManager.Instance.pm);
        }
        

        public void SetFailing()
        {
            _failing = true;
            Debug.Log("Chamber Pressure is "+ Part.GetModifier<RocketEngineData>().ChamberPressure);
            float newChamberPressure = Part.GetModifier<RocketEngineData>().ChamberPressure * UnityEngine.Random.value;
            Traverse.Create(Part.GetModifier<RocketEngineData>()).Field("_chamberPressure").SetValue(newChamberPressure);
            Debug.Log("Chamber Pressure is now "+ Part.GetModifier<RocketEngineData>().ChamberPressure);
            Traverse.Create(Part.GetModifier<RocketEngineData>()).Field("_exhaustSootIntensity").SetValue(0.5F);
            Traverse.Create(Part.GetModifier<RocketEngineData>()).Field("_exhaustSootLength").SetValue(10F); 
            Traverse.Create(Part.GetModifier<RocketEngineData>()).Field("_hasSmoke").SetValue(true); 
            Traverse.Create(Part.GetModifier<RocketEngineData>()).Field("_smokeOffset").SetValue(10F);
            Part.PartScript.GetModifier<RocketEngineScript>().InitializeExhaust();
            Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"{Part.Name} is failing.");
        }

        public void Safe()
        {
            _failing = false;
            Part.Activated = false;
            Part.Config.SupportsActivation = false;
            Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"{Part.Name} has safely shut down.");
        }


        private Dictionary<string, List<string>> _familyinfo = new Dictionary<string, List<string>>
        {
            {"RocketEngine", new List<string>(){
                "fuelType", "nozzleTypeId", "engineTypeId", "engineSubTypeId", "size"
            }}
        };

        private Dictionary<string, List<string>> _partinfo = new Dictionary<string, List<string>>
        {
            {"RocketEngine", new List<string>()
                {
                    "fuelType", "nozzleTypeId", "engineTypeId", "engineSubTypeId", "size",
                    "chamberPressure", "gimbalRange", "nozzleSize", "nozzleThroatSize",
                    "fuelGrain"
                }
            },
            {"SpaceRace.SRRocketEngine", new List<string>()
                {
                    "airLight", "maxIgnitions", "throttleDown"
                }
            }
        };

        public TechList Technologies(PartData part, bool family = false)
        {
            TechList list = new TechList();
            RocketEngineData red = part.GetModifier<RocketEngineData>();
            list.AddTech(TypeId, string.Format("Nozzle:{0}", red.NozzleType.Id), red.NozzleType.Name);
            if (red.EngineType.Id.StartsWith("GasGenerator"))
            {
                list.AddTech(TypeId, string.Format("Cycle:{0}", "GasGenerator"), "Gas Generator");
            }
            else
            {
                list.AddTech(TypeId, string.Format("Cycle:{0}", red.EngineType.Id), red.EngineType.Name);
            }
            
            list.AddTech(TypeId, string.Format("{1}EngineScale:{0:F2}", red.Scale, red.EngineTypeId));

            string fullFuel = red.FuelType.Id;
            if (fullFuel.StartsWith("HP ")) list.AddTech(TypeId,string.Format("Fuel:{0}", fullFuel.Substring(3)), fullFuel.Substring(3));
            else list.AddTech(TypeId, string.Format("Fuel:{0}", fullFuel), fullFuel);

            if (!family)
            {
                if (part.GetModifier<SRRocketEngineData>().AirLight && part.GetModifier<RocketEngineData>().EngineTypeId =="Liquid") list.AddTech(TypeId, "AirLight", "Airlighting Engine");
                if (part.GetModifier<SRRocketEngineData>().MaxIgnitions > 1) list.AddTech(TypeId,"Reignition");
                if (part.GetModifier<SRRocketEngineData>().ThrottleDown > 0F) list.AddTech(TypeId,"Throttling");
                if (part.GetModifier<SRRocketEngineData>().ThrottleDown > 0.5) list.AddTech(TypeId,"DeepThrottling");
            }
            return list;
        }
        Assets.Scripts.SpaceRace.Collections.EfficiencyFactor ISRPartMod.EfficiencyFactor(string tech, int timesDeveloped, bool contractorDeveloped, Assets.Scripts.SpaceRace.Projects.ProjectCategory category)
        {
            string[] split = tech.Split(":");
            switch (split[0])
            {
                case "PartFamily" :
                    if (category == ProjectCategory.Part)
                    {
                        if (contractorDeveloped)
                        {
                            return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor(){
                            Text = "Variant of our developed part",
                            Penalty = -0.75f,
                            ToolTip = "We have developed a part in this part family."
                            };
                        }
                        else if (timesDeveloped > 0)
                        {
                            return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor(){
                            Text = "Variant of a developed part",
                            Penalty = -0.25f,
                            };
                        }
                    }
                    return null;
                case "SolidEngineScale":
                    if (category!=ProjectCategory.Part) return null;
                    float size = float.Parse(split[1]);
                    float difference = size - 1.0F;
                    foreach(TechOccurenceTriple tot in _pm.Data.PartTechDeveloped)
                    {
                        if (tot.Tech.StartsWith("SolidEngineScale") && tot.Occurences > 0)
                        {
                            if (size - float.Parse(tot.Tech.Split(":")[1]) < difference)
                            difference = size - float.Parse(tot.Tech.Split(":")[1]);
                        }
                    }
                    if (difference > 0F)  return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Largest Engine Ever", difference, "Penalty proportional to the size difference to the next largest developed engine.");
                    return null;
                case "LiquidEngineScale":
                    if (category!=ProjectCategory.Part) return null;
                    float size2 = float.Parse(split[1]);
                    float difference2 = size2 - 1.0F;
                    foreach(TechOccurenceTriple tot in _pm.Data.PartTechDeveloped)
                    {
                        if (tot.Tech.Split(":")[0] == "LiquidEngineScale" && tot.Occurences > 0)
                        {

                            if (size2 - float.Parse(tot.Tech.Split(":")[1]) < difference2)
                            difference2 = size2 - float.Parse(tot.Tech.Split(":")[1]);
                        }
                    }
                    if (difference2 > 0F)  return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Largest Engine Ever", difference2, "Penalty proportional to the size difference to the next largest developed engine.");
                    return null;
                case "Nozzle":
                    switch (split[1])
                    {
                        case "Plug": return null;
                        case "Cone": return null;
                        default:
                        if (!contractorDeveloped && category==ProjectCategory.Part)
                        {
                            switch (timesDeveloped)
                            {
                                case 0 : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Novel nozzle: "+split[1], 0.30f, "No one has developed this nozzle before.");
                                case 1 : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Proprietary nozzle: "+split[1], 0.20f, "Only one contractor has developed this nozzle before.");
                                default : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Unfamiliar nozzle: "+split[1], 0.10f, "Other contractors have developed this nozzle before, but we have not.");
                            }
                        }
                        return null;
                    }
                case "AirLight":
                    if (!contractorDeveloped && category==ProjectCategory.Part)
                    {   
                        switch (timesDeveloped)
                        {
                            case 0:
                            {
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor(){
                                    Text = "First Airlighting Engine",
                                    Penalty = 0.3f
                                };
                            }
                            default:
                            {
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor(){
                                    Text = "Airlighting Engine",
                                    Penalty = 0.15f
                                };
                            }
                        }
                    }
                    return null;
                case "Cycle":
                    if (!contractorDeveloped && category==ProjectCategory.Part)
                    {
                        switch (split[1])
                        {
                            case "Model": return null;
                            case "Booster": return null;
                            case "PressureFed": return null;
                            case "Staged1": switch (timesDeveloped)
                            {
                                case 0 : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Novel cycle: "+split[1], 1.0f, "No one has used this engine cycle before.");
                                case 1 : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Proprietary cycle: "+split[1], 0.50f, "Only one contractor has used this engine cycle before.");
                                default : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Unfamiliar cycle: "+split[1], 0.30f, "Other contractors have used this engine cycle before, but this one has not.");
                            }
                            case "Staged2": switch (timesDeveloped)
                            {
                                case 0 : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Novel cycle: "+split[1], 2.0f, "No one has used this engine cycle before.");
                                case 1 : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Proprietary cycle: "+split[1], 1.0f, "Only one contractor has used this engine cycle before.");
                                default : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Unfamiliar cycle: "+split[1], 0.5f, "Other contractors have used this engine cycle before, but this one has not.");
                            }
                            default: switch (timesDeveloped)
                            {
                                case 0 : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Novel cycle: "+split[1], 0.50f, "No one has used this engine cycle before.");
                                case 1 : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Proprietary cycle: "+split[1], 0.30f, "Only one contractor has used this engine cycle before.");
                                default : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Unfamiliar cycle: "+split[1], 0.10f, "Other contractors have used this engine cycle before, but this one has not.");
                            }
                        }
                    }
                    if (!contractorDeveloped && category==ProjectCategory.Stage)
                    {
                        switch (split[1])
                        {
                            case "PressureFed": return null;
                            case "Model": return null;
                            case "Booster": return null;
                            default: 
                            return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Unfamiliar cycle: "+split[1], 0.50f, "This contractor has not developed a stage for this engine cycle before.");
                        }
                        
                    }
                    return null;
                case "Fuel":
                    switch (split[1])
                    {
                        case "Solid": 
                            if (category == ProjectCategory.Part)
                            {
                                if (contractorDeveloped) return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Solid Motor", -0.80f, "Solid motors are easier to develop");
                                else return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Solid Motor", -0.60f, "Solid motors are easier to develop");
                            }
                            return null;
                        case "LOX/ETOH": return null;
                        case "Monopropellant": return null;
                        case "Nitrogen": return null;
                        default: if (!contractorDeveloped)
                        {
                            switch (timesDeveloped)
                            {
                                case 0 : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Novel fuel: "+split[1], 0.50f, "No one has used this fuel before.");
                                default : 
                                return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor("Unfamiliar fuel: "+split[1], 0.20f, "Other contractors have used this fuel before, but this one has not.");
                            }
                        }
                        return null;
                    }
                default:
                {
                    if (!contractorDeveloped)
                    {
                        switch (timesDeveloped)
                        {
                            case 0:
                            return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor(){
                                Text = "Novel tech: "+ tech,
                                Penalty = 0.5f
                            };
                            case 1:
                            return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor(){
                                Text = "Proprietary tech: "+ tech,
                                Penalty = 0.3f
                            };
                            default:
                            return new Assets.Scripts.SpaceRace.Collections.EfficiencyFactor(){
                                Text = "Unfamiliar tech: "+ tech,
                                Penalty = 0.2f
                            };
                        }
                    }
                }
                return null; 
            }
        }
        public Dictionary<string, List<string>> FamilyInfo {get{ return _familyinfo;}}
        public Dictionary<string, List<string>> PartInfo {get{ return _partinfo;}}

        public string TechDisplayName(string tech)
        {
            return tech;
        }

        public Dictionary<string, string> DevPanelDisplay(PartData part)
        {
            return new Dictionary<string, string>(){
                {"Cycle", part.GetModifier<RocketEngineData>().EngineType.Name},
                {"Fuel", part.GetModifier<RocketEngineData>().FuelType.Name},
                {"Size", string.Format("{0:F2}", part.GetModifier<RocketEngineData>().Scale)}
            };
        }


        public PartDevBid GenericBid(PartData part)
        {
            RocketEngineData mod = part.GetModifier<RocketEngineData>();
            IReactionEngine engine = mod.Script;
            double productionTimeFactor = 1.0;
            if (part.GetModifier<SRRocketEngineData>().MaxIgnitions > 1)
            {
                productionTimeFactor *= 2.0;
            }
            if (part.GetModifier<RocketEngineData>().EngineTypeId == "Redundant")
            {
                productionTimeFactor *= 2.0;
            }
            return new PartDevBid(_pm, this)
            {
                BaseDevTime = 300 * SRFormulas.SecondsPerDay,
                DevCostPerDay = (long)(part.Price * 15.0 / 300.0),
                FamilyName = part.Name +" Family",
                Name = part.Name,
                DesignerDescription = $"{Units.GetForceString(engine.MaximumThrust)} {mod.EngineType.Id} engine burning {mod.FuelType.Name}",
                UnitProductionRequired = part.Price * (0.9f+0.1f * _pm.Career.TechTree.GetItemValue("SpaceRace.ShutDownSafety").ValueAsFloat),
                ProductionCapacity = part.Price / 30 / productionTimeFactor,
                Technologies = Technologies(part)
            };
        }

        public double FamilyStageCost(PartData part)
        {
            RocketEngineData engineData = part.GetModifier<RocketEngineData>();
            return 0.2 * engineData.EngineType.BasePrice  + 10000000.0 * engineData.EngineType.BaseScale * engineData.EngineType.BaseScale * engineData.EngineType.BaseScale * engineData.EngineType.PriceScale * engineData.FuelType.EnginePriceScale * engineData.Scale * engineData.Scale;
        }
    }
}