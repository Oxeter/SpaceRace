namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Craft.Propulsion;
    using UnityEngine;
    using ModApi.Design.PartProperties;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using Unity.Mathematics;
    using Assets.Scripts.SpaceRace.Projects;
    using HarmonyLib;

    [Serializable]
    [DesignerPartModifier("Construction")]
    [PartModifierTypeId("SpaceRace.SRFuelTank")]
    public class SRFuelTankData : PartModifierData<SRFuelTankScript>, ISRPartMod
    {
        public string DesignerCategory {get {return null;}}
        private IProgramManager _pm;
        public SRFuelTankData(){}
        public SRFuelTankData(IProgramManager pm)
        {
            _pm = pm;
        }
        public FuelTankConstruction Construction => SRManager.TankConstructions.First(tc =>tc.Name == _constructionTypeId);
        private int _srpriority = 0;
        public int SRPriority {get {return _srpriority;}}

        [SerializeField]
        [PartModifierProperty(true,false)]
        private float _diameter = 0F;
        public float Diameter {get{return _diameter;} set{_diameter = value;}}

        [SerializeField]
        [DesignerPropertySpinner(Label = "Construction", Order = 10, Tooltip = "The materials and geometry of the tank")]
        private string _constructionTypeId = "Steel Wall";

        public string ConstructionTypeId => _constructionTypeId;

        [SerializeField]
        [PartModifierProperty(true,false)]
        private bool _highPressure = false;
        public bool HighPressure => _highPressure;

        [DesignerPropertyLabel(Order = 20, PreserveState = false, NeverSerialize = true)]
        private string _highPressureString = string.Empty;

        [SerializeField]
        [PartModifierProperty(true,false)]
        private bool _forceHighPressure = false;
        private void SetShellDensity()
        {
            FuselageData fdata = Part.GetModifier<FuselageData>();
            Traverse density = Traverse.Create(fdata).Field("_shellDensityOverride").SetValue(_highPressure? Construction.HPDensity : Construction.Density);
            Part.GetModifier<FuselageData>().MeshPriceMultiplier = Construction.PricePerMass;
            Part.PartScript.CraftScript.SetStructureChanged();
        }
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            base.OnDesignerInitialization(d);
            d.OnSpinnerValuesRequested(() => _constructionTypeId, GetConstructionTypes);
            d.OnPropertyChanged(() => _constructionTypeId, (x,y)=> {
                SetShellDensity();
                Part.GetModifier<FuselageData>().OnDesignerCraftStructureChanged();
                d.Manager.RefreshUI();});
            d.OnValueLabelRequested(() => _highPressureString, (string x) => _highPressure? "High Pressure: Heavy but required for pressure-fed engines and RCS" : "Normal Pressure: Lightweight but engines must pump the propellant themselves.");
        }
        private bool IsHighPressure(string id)
        {
            if (id.StartsWith("HP ")) return true;
            if (id == "Monopropellant") return true;
            if (id == "Nitrogen") return true;
            if (id == "Battery") return true;
            return false;
        }

        public void ContructionByFuelType(FuelTankData data)
        {
            _highPressure = IsHighPressure(data.FuelType.Id) || _forceHighPressure;
            Debug.Log(string.Format("{0} is high pressure? {1}",data.FuelType.Id, _highPressure.ToString()));
            if (_highPressure && !Construction.HP) _constructionTypeId = "Steel Wall";
            if (data.FuelType.Id == "Solid") 
            {
                _highPressure = _forceHighPressure;
                _constructionTypeId = "Steel Wall";
            }
            SetShellDensity();
            base.DesignerPartProperties.GetSpinnerProperty(() => _constructionTypeId)?.UpdateValues();
        }


        private void GetConstructionTypes(List<string> list)
        {
            list.Clear();
            foreach (FuelTankConstruction construction in SRManager.TankConstructions.Where(con => con.HP || !_highPressure))
            {
                if (construction.Tech != null)
                {
                    if (!Game.IsCareer || Game.Instance.GameState.Validator.IsItemAvailable(construction.Tech))
                    {
                        list.Add(construction.Name);
                    }
                }
                else
                {   
                    list.Add(construction.Name);
                } 
            }
        }

        public TechList Technologies(PartData part, bool family = false)
        {
            return new TechList
            (
                "SpaceRace.SRFuelTank", 
                new List<string>()
                {
                    string.Format("Construction:{0}", part.GetModifier<SRFuelTankData>().ConstructionTypeId),
                    string.Format("Diameter:{2}{0}:{1:F1}", part.GetModifier<SRFuelTankData>().ConstructionTypeId, part.GetModifier<SRFuelTankData>().Diameter, part.GetModifier<SRFuelTankData>().HighPressure? "HP " : string.Empty),
                    string.Format("{0}", part.GetModifier<FuelTankData>().FuelType.Id == "Solid" ? "Solid" : "Liquid"),
                }
            );
        }

        public Dictionary<string, string> DevPanelDisplay(PartData part)
        {

            return new Dictionary<string, string>()
            {
                {"Construction Type", part.GetModifier<SRFuelTankData>().ConstructionTypeId},
                {"Diameter", string.Format("{0:F1}m", part.GetModifier<SRFuelTankData>().Diameter)}
            };
        }
        public Dictionary<string, List<string>> FamilyInfo {get;} = new Dictionary<string, List<string>>() 
        {
            {"SpaceRace.SRFuelTank", new List<string>(){"constructionTypeId", "diameter"}}
        };
        public Dictionary<string, List<string>> PartInfo {get;} = new Dictionary<string, List<string>>()
        {
            {"SpaceRace.SRFuelTank", new List<string>(){"constructionTypeId", "diameter"}}
        }; 
        public string FamilyName(PartData part)
        {
            return string.Format("{0} Tank ({1:F1}m)", part.GetModifier<SRFuelTankData>().ConstructionTypeId, part.GetModifier<SRFuelTankData>().Diameter);
        }        
        public PartDevBid GenericBid(PartData part)
        {
            return new PartDevBid(_pm, this, DesignerCategory);
        }

        EfficiencyFactor ISRPartMod.EfficiencyFactor(string tech, int timesDeveloped, bool contractorDeveloped, ProjectCategory category)
        {
            switch (category)
            {
                case ProjectCategory.Stage:
                if (tech.Split(":")[0]=="Construction")
                {
                    switch (tech.Split(":")[1]) 
                    {
                        case "Steel Wall": return null;
                        case "Steel Balloon": goto case "Aluminium Alloy Isogrid";
                        case "Aluminium Isogrid": goto case "Aluminium Alloy Isogrid";
                        case "Aluminium Alloy Isogrid":
                        if (timesDeveloped == 0) return new EfficiencyFactor("Novel Tank: " + tech.Split(":")[1], 1.0F);
                        if (!contractorDeveloped) return new EfficiencyFactor("Unfamiliar Tank: " + tech.Split(":")[1], 0.3F);
                        return null;
                        default: 
                        if (timesDeveloped == 0) return new EfficiencyFactor("Novel Tank: " + tech.Split(":")[1], 0.3F);
                        if (!contractorDeveloped) return new EfficiencyFactor("Unfamiliar Tank: " + tech.Split(":")[1], 0.1F);
                        else return null;
                    }
                }
                if (!contractorDeveloped & tech.Split(":")[0]=="Diameter")
                {
                    switch (tech.Split(":")[1]) 
                    {
                        case "Steel Wall": return null;
                        case "HP Steel Wall": return null;
                        case "Steel Balloon": goto case "Aluminium Alloy Isogrid";
                        case "Aluminium Isogrid": goto case "Aluminium Alloy Isogrid";
                        case "Aluminium Alloy Isogrid":
                        return new EfficiencyFactor($"New Tooling: {tech.Split(":")[1]} {tech.Split(":")[2]}", 0.75F);
                        default: 
                        return new EfficiencyFactor($"New Tooling: {tech.Split(":")[1]} {tech.Split(":")[2]}", 0.3F);
                    }
                }
                return null;
                case ProjectCategory.Integration:
                if (tech.Split(":")[0]=="Construction")
                {
                    switch (tech.Split(":")[1]) 
                    {
                        case "Steel Wall": return null;
                        case "Steel Balloon":
                        if (timesDeveloped == 0) return new EfficiencyFactor("Novel Tank: " + tech.Split(":")[1], 0.5F);
                        else return new EfficiencyFactor("Balloon Tanks", 0.3F);
                        default: 
                        if (timesDeveloped == 0) return new EfficiencyFactor("Novel Tank: " + tech.Split(":")[1], 0.2F);
                        return null;
                    }
                }
                return null;
                default : return null;
            }
        }    
        public double FamilyStageCost(PartData part)
        {
            SRFuelTankData data = part.GetModifier<SRFuelTankData>();
            return 10.0 * data.Diameter * data.Diameter * data.Construction.PricePerMass * (data.HighPressure ? data.Construction.HPDensity ?? data.Construction.Density : data.Construction.Density);
        }
    }
    public class FuelTankConstruction
    {
        public string Name;
        public float Density;
        public float PricePerMass;
        public string Tech;
        public bool HP;
        public float? HPDensity;

        public FuelTankConstruction(string name, float density, float pricePerMass, string tech = null, bool hp = false, float? hpDensity = null)
        {
            Name = name;
            Density = density;
            PricePerMass = pricePerMass;
            Tech = tech;
            HP = hp;
            HPDensity = hpDensity;
        }

    }


    
}