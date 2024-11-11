namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Math;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using ModApi.Design.PartProperties;
    using ModApi.Craft.Propulsion;
    using UnityEngine;
    using Assets.Scripts.SpaceRace.Collections;

    using HarmonyLib;
    using Assets.Scripts.SpaceRace.Projects;
    using Assets.Scripts.SpaceRace.Formulas;

    [Serializable]
    [DesignerPartModifier("RCS Fuel")]
    [PartModifierTypeId("SpaceRace.SRRCS")]
    public class SRRCSData : PartModifierData<SRRCSScript>, ISRPartMod
    {
        private IProgramManager _pm;
        public string DesignerCategory {get {return "Control & Descent";}}
        [SerializeField]
        public double BurnTime = 0.0; 
        [DesignerPropertySpinner(Tooltip = "More advanced fuels are more powerful and efficient but also more expensive.  IMPORTANT: All RCS thrusters currently consume Monopropellant, regardless of fuel type.")]
        [SerializeField]
        private string _fuelTypeId = "Nitrogen";
        [PartModifierProperty(true, false)]
        [SerializeField]
        private float _basePower = 800f;
        [SerializeField]
        public float BaseFailureRate = 0.004F;
        [SerializeField]
        public float NextFailureRate = 0.002F;
        public string FuelTypeId => _fuelTypeId;
        public float FailureFactor => (this as ISRPartMod).HardwareFailures? 1F : 0F;

        public FuelType Fuel => Game.Instance.PropulsionData.Fuels.FirstOrDefault(x => x.Id == _fuelTypeId);

        private List<string> SupportedFuels = new List<string>(){"Nitrogen","Monopropellant","HP MMHNTO"};
        public override long Price => (long)(Part.GetModifier<ReactionControlNozzleData>().Price * PriceFuelModifier(_fuelTypeId));
        public int SRPriority => 0;
        private ReactionControlNozzleData rcnd => Part.GetModifier<ReactionControlNozzleData>();
        public override float MassDry => rcnd.Scale * 0.1f - rcnd.Scale * rcnd.Scale * rcnd.Scale *  0.1f;
        public SRRCSData(){}

        public SRRCSData(IProgramManager pm)
        {
            _pm=pm;
        }
        private double PriceFuelModifier(string fuel)
        {
            if (fuel == "Nitrogen") return -0.5;
            if (fuel == "Monopropellant") return 0;
            if (fuel == "HP MMHNTO") return 3;
            return 0;
        }

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnPropertyChanged(() => _fuelTypeId, delegate
            {
                UpdateFuelType(d);
                UpdateInfo(SRManager.Instance.pm);
            });
            d.OnSpinnerValuesRequested(() => _fuelTypeId, GetFuelTypes);
        }
        public void UpdateInfo(IProgramManager pm)
        {
            float num = pm.Data.TechIntegrated.Occurences(Technologies(Part)[0]);
            BaseFailureRate = num > 11? 0.002f : 0.002f * (1f + 0.125f * (12.0f-num) * (12.0f-num));
            NextFailureRate = BaseFailureRate / 2;
        }
        private void UpdateFuelType(IDesignerPartPropertiesModifierInterface d)
        {
            UpdatePower();
            d.Manager.Flyout.RefreshUI();
        }
        public void UpdatePower()
        {
            ReactionControlNozzleData data = Part.GetModifier<ReactionControlNozzleData>();
            Traverse.Create(data).Field("_power").SetValue(FuelPower(Fuel) * data.Scale * data.Scale);
        }
        private float FuelPower(FuelType fuel)
        {
            switch (fuel.Id)
            {
                case "Nitrogen": return 0.25f * _basePower;
                case "Monopropellant": return _basePower;
                case "HP MMHNTO": return 2f * _basePower;
                default: return _basePower;
            }
        }

        private void GetFuelTypes(List<string> obj)
        {

            foreach (string item in SupportedFuels)
            {
                if (Game.Instance.GameState.Validator.IsItemAvailable("FuelType.{0}", item))
                {
                    obj.Add(item);
                }
            }
        }

        public string UndevelopedFamilyName(PartData part)
        {
            return $"{Units.GetForceString(part.GetModifier<ReactionControlNozzleData>().Power)} {part.GetModifier<SRRCSData>().FuelTypeId} Thruster"; 
        }
        public string FamilyName(PartData part)
        {
            return $"{Units.GetForceString(part.GetModifier<ReactionControlNozzleData>().Power)} {part.GetModifier<SRRCSData>().FuelTypeId} Thruster"; 
        }

        
        public Dictionary<string, List<string>> FamilyInfo => new Dictionary<string, List<string>>
        {
            {"ReactionControlNozzle", new List<string>()
                {
                    "powerScale", "scale"
                }
            },
            {"SpaceRace.SRRCS", new List<string>()
                {
                    "fuelTypeId"
                }
            }
        };

        public Dictionary<string, List<string>> PartInfo => new Dictionary<string, List<string>>
        {
            {"ReactionControlNozzle", new List<string>()
                {
                    "powerScale", "scale"
                }
            },
            {"SpaceRace.SRRCS", new List<string>()
                {
                    "fuelTypeId"
                }
            }
        };


        public TechList Technologies(PartData part, bool family = false)
        {
            return new TechList()
            {
                new Technology(TypeId, string.Format("Fuel:{0}", part.GetModifier<SRRCSData>().FuelTypeId), part.GetModifier<SRRCSData>().Fuel.Name), 
            };
        }

        public Dictionary<string, string> DevPanelDisplay(PartData part)
        {
            return new Dictionary<string, string>(){
                {"Fuel", part.GetModifier<SRRCSData>().FuelTypeId},
                {"Size", string.Format("{0:F2}", part.GetModifier<ReactionControlNozzleData>().Scale)},
                {"Power", Units.GetForceString(part.GetModifier<ReactionControlNozzleData>().Power)}
            };
        }


        public PartDevBid GenericBid(PartData part)
        {
            return new PartDevBid(_pm, this)
            {
                BaseDevTime = part.GetModifier<SRRCSData>().FuelTypeId =="Nitrogen" ? 150.0 * SRFormulas.SecondsPerDay : 300.0 * SRFormulas.SecondsPerDay,
                DevCostPerDay = (long)(part.Price * 8.0 / 150.0),
                FamilyName = $"{Units.GetForceString(part.GetModifier<ReactionControlNozzleData>().Power)} {part.GetModifier<SRRCSData>().Fuel.Name} Thruster",
                Name = $"{Units.GetForceString(part.GetModifier<ReactionControlNozzleData>().Power)} {part.GetModifier<SRRCSData>().Fuel.Name} Thruster",
                DesignerDescription = "Reaction control nozzles help provide precise attitude control. Comsumes monopropellant.",
                UnitProductionRequired = part.Price,
                ProductionCapacity = part.Price / 8.0,
                Technologies = Technologies(part),
                BatchSize = 8,
            };
        }

        public double FamilyStageCost(PartData part)
        {
            ReactionControlNozzleData data = part.GetModifier<ReactionControlNozzleData>();
            SRRCSData data2 = part.GetModifier<SRRCSData>();
            return 0.2 * (data.Price + data2.Price);
        }
    }
}