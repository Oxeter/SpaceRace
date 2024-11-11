namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Assets.Scripts.SpaceRace.Collections;
    using ModApi.Math;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using Unity.Mathematics;
    using UnityEngine;
    using Assets.Scripts.SpaceRace.Projects;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using Assets.Scripts.SpaceRace.Formulas;
    using System.ComponentModel;

    [Serializable]
    [DesignerPartModifier("Avionics")]
    [PartModifierTypeId("SpaceRace.SRAvionics")]
    public class SRAvionicsData : PartModifierData<SRAvionicsScript>, ISRPartMod
    {
        private IProgramManager _pm;
        [SerializeField]
        [DesignerPropertySpinner(Label = "Guidance", Order = 10, Tooltip = "The method used to guide the craft")]
        private string _guidance = "Mechanical";

        [SerializeField]
        [DesignerPropertySlider(MinValue = 0, MaxValue = 16, NumberOfSteps = 17, Label = "Maximum Thrust", Tooltip = "The guidence system cannot control a craft with more than this amount of thrust.")]
        private float _maxThrustScale = 0F;
        [SerializeField]
        [DesignerPropertySlider(MinValue = 0, MaxValue = 16, NumberOfSteps = 17, Label = "Communication Range", Tooltip = "The ")]
        private float _communicationScale = 0F;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _powerConsumption = 0F;
    
        [SerializeField]
        public string LaunchLocationName;

        public string Guidance => _guidance;
        [SerializeField]
        public bool TooMuchThrust = false;
        [SerializeField]
        public bool InCommunication= true;
        [SerializeField]
        public double FlightTime = 0.0;

        [SerializeField]
        public int OriginalCommandId = -1;
        public string DesignerCategory => "Command";
        public float MaxFlightTime = 0F;
        public float PowerConsumption {get {return _powerConsumption;} private set {_powerConsumption = value;}}
        public int SRPriority => -1;

        /// <summary>
        /// The maximum thrust in kilonewtons that the guidance system can handle
        /// </summary>
        public float MaxThrust => _guidance == "Unguided" ? -1f : 100f * math.pow(2 , (int)_maxThrustScale);
        public double MaxComDistance {get 
        {
            float comtech = 3f;
            if (Game.IsCareer)
            {
                comtech = Game.Instance.GameState.Validator.ItemValue("SpaceRace.ComTech");
            }
            return 200000.0 * math.pow(2.0, _communicationScale + 4 * comtech);
        }}
        public SRAvionicsData(){}
        public SRAvionicsData(IProgramManager pm)
        {
            _pm = pm;
        }
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            base.OnDesignerInitialization(d);
            d.OnPropertyChanged(() => _maxThrustScale, (x,y) => {d.Manager.Flyout.RefreshUI(); UpdatePowerConsumption();});
            d.OnPropertyChanged(() => _communicationScale, (x,y) => {d.Manager.Flyout.RefreshUI(); UpdatePowerConsumption();});
            d.OnPropertyChanged(() => _guidance, (x,y) => {d.Manager.Flyout.RefreshUI(); UpdatePowerConsumption();});
            d.OnValueLabelRequested(()=> _maxThrustScale, (f) => Units.GetForceString(MaxThrust));
            d.OnValueLabelRequested(()=> _communicationScale, (f) => Units.GetDistanceString((float)MaxComDistance));
            d.OnSpinnerValuesRequested(() => _guidance, GetGuidanceTypes);
        }

        private void GetGuidanceTypes(List<string> list)
        {
            list.Clear();
            list.Add("Mechanical");
            list.Add("Unguided");
            list.Add("Radio");
            if (Game.Instance.GameState.Validator.IsItemAvailable("Command.Electronic")) list.Add("Electronic");
        }

        private void UpdatePowerConsumption()
        {
            float comconsumption = 0.01f * math.pow(2F, _communicationScale / 2F);
            float guidanceconsumptiuon = 0.02f;
            switch (_guidance)
            {
                case "Mechanical":
                guidanceconsumptiuon = 0.25f + 0.03f*_maxThrustScale;
                MaxFlightTime = (float)SRFormulas.MechanicalGuidanceTime;
                break;
                case "Radio":
                guidanceconsumptiuon = 0.25f + 0.03f*_maxThrustScale;
                MaxFlightTime = float.MaxValue;
                break;
                case "Electronic":
                guidanceconsumptiuon = 0.05f + 0.03f*_maxThrustScale;
                MaxFlightTime = float.MaxValue;
                break;
                default:
                MaxFlightTime = 0F;
                break;
            }
            PowerConsumption = comconsumption + guidanceconsumptiuon;
        }

        public override long Price => _guidance == "Unguided"? 10000 * (1 + (int)_communicationScale): (long)MaxThrust * 50 + 10000 * (int)_communicationScale;
        public override float MassDry {get {
            float commass = 0.02f* (1f + _communicationScale);
            switch (_guidance)
            {
                case "Unguided": return 0.05f + commass;
                case "Mechanical": return 1.3f * MathF.Pow(1.2f,_maxThrustScale)+ commass;
                case "Radio": return 0.8f * MathF.Pow(1.2f,_maxThrustScale)+ commass;
                case "Electronic": return 0.7f * MathF.Pow(1.2f,_maxThrustScale) + commass;
                default : return 0.8f * MathF.Pow(1.2f,_maxThrustScale)+ commass;
            }
        }}
        public TechList Technologies(PartData part, bool family = false)
        {
            return new TechList()
            {
                new Technology(TypeId,string.Format("Guidance:{0}",part.GetModifier<SRAvionicsData>().Guidance)),
                new Technology(TypeId,string.Format("MaxThrust:{0}",part.GetModifier<SRAvionicsData>().MaxThrust))
            };
        }

        public string UndevelopedFamilyName(PartData part)
        {
            return FamilyName(part);
        }
        public string FamilyName(PartData part)
        {
            return MaxThrust > 0 ? $"{Units.GetForceString(part.GetModifier<SRAvionicsData>().MaxThrust)} {part.GetModifier<SRAvionicsData>().Guidance} Avionics" : "Unguided Avionics";
        }

        public string UndevelopedPartName(PartData part)
        {
            return part.PartScript.CraftScript.Data.Name+" Avionics";
        }

        public Dictionary<string, string> DevPanelDisplay(PartData part)
        {

            return new Dictionary<string, string>()
            {
                {"Guidance", part.GetModifier<SRAvionicsData>().Guidance},
                {"Max Thrust", Units.GetForceString(part.GetModifier<SRAvionicsData>().MaxThrust)},
                {"Com Range", Units.GetDistanceString((float)part.GetModifier<SRAvionicsData>().MaxComDistance)},
                {"Battery", Units.GetEnergyString((float)part.GetModifier<FuelTankData>().Capacity * 1000f)}
            };
        }
        public Dictionary<string, List<string>> FamilyInfo => new Dictionary<string, List<string>>() 
        {
            {"SpaceRace.SRAvionics", new List<string>(){"guidance", "maxThrustScale"}}
        };

        public Dictionary<string, List<string>> PartInfo => new Dictionary<string, List<string>>()
        {
            {"FuelTank", new List<string>(){"capacity"}},
            {"SpaceRace.SRAvionics", new List<string>(){"guidance", "maxThrustScale", "communicationScale"}}
        };

        public EfficiencyFactor EfficiencyFactor(string tech, int timesDeveloped, bool contractorDeveloped, ProjectCategory category)
        {
            
            switch (category)
            {
                case ProjectCategory.Part:
                if (tech.StartsWith("PartFamily"))
                {
                    if (contractorDeveloped)
                    {
                        return new EfficiencyFactor(){
                        Text = "Variant of our developed part",
                        Penalty = -0.75f
                        };
                    }
                    else if (timesDeveloped > 0)
                    {
                        return new EfficiencyFactor(){
                        Text = "Variant of a developed part",
                        Penalty = -0.25f
                        };
                    }
                }
                else if (tech.Split(':')[0] == "Guidance" && !contractorDeveloped)
                {
                    if (tech.Split(':')[1] == "Unguided") return null;
                    if (timesDeveloped == 0) 
                    {
                        return new EfficiencyFactor(){
                            Text = "Novel guidance: "+ tech.Split(':')[1],
                            Penalty = 1f
                        };
                    }
                    else if (timesDeveloped == 1)
                    {
                        return new EfficiencyFactor(){
                            Text = "Proprietary guidance: "+ tech.Split(':')[1],
                            Penalty = 0.5f
                        };
                    }
                    else 
                    {
                        return new EfficiencyFactor(){
                            Text = "Unfamiliar guidance: " + tech.Split(':')[1],
                            Penalty = 0.25f
                        };
                    }
                }
                return null;
                default:
                if (tech.Split(':')[0] == "Guidance" && timesDeveloped == 0)
                {
                    return new EfficiencyFactor(){
                        Text = "Novel guidance: "+ tech.Split(':')[1],
                        Penalty = 0.2f
                    };
                }
                return null;
            }
           
            
        }
        
        public PartDevBid GenericBid(PartData part)
        {
            SRAvionicsData mod = part.GetModifier<SRAvionicsData>();
            return new PartDevBid(_pm, this)
            {
                BaseDevTime = mod.Guidance == "Unguided" ? 100.0 * SRFormulas.SecondsPerDay : 300.0 * SRFormulas.SecondsPerDay,
                DevCostPerDay = (long)(part.Price * 8.0 / 100),
                FamilyName = FamilyName(part),
                Name = UndevelopedPartName(part),
                DesignerDescription = mod.Guidance == "Unguided" ? "Unguided Avionics" : $"{Units.GetForceString(mod.MaxThrust)} {mod.Guidance} Avionics",
                UnitProductionRequired = part.Price,
                ProductionCapacity = mod.Guidance == "Unguided" ? part.Price / 25.0 : part.Price / 75.0,
                Technologies = Technologies(part)
            };
        }
        public double FamilyStageCost(PartData part)
        {
            SRAvionicsData data = part.GetModifier<SRAvionicsData>();
            return data.Guidance == "Unguided"? 5000: (long)data.MaxThrust * 10L;
        }
    }
}