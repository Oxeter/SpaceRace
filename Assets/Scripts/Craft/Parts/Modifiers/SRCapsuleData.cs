namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using UnityEngine;
    using SpaceRace.Projects;
    using SpaceRace.Collections;
    using Assets.Scripts.Craft.Parts.Modifiers.Eva;
    using Assets.Scripts.Design.PartProperties;
    using ModApi.Math;
    using Unity.Mathematics;
    using Assets.Scripts.Design;
    using ModApi.Design.PartProperties;
    using ModApi.Craft.Propulsion;
    using ModApi;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using Assets.Scripts.SpaceRace.Formulas;

    [Serializable]
    [DesignerPartModifier("Capsule")]
    [PartModifierTypeId("SpaceRace.SRCapsule")]
    public class SRCapsuleData : PartModifierData<SRCapsuleScript>, ISRPartMod
    {
        private IProgramManager _pm;
            //
        // Summary:
        //     The base scale of the pod.
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _baseScale = 1f;       

                // Summary:
        //     The base scale of the pod.
        [SerializeField]
        [DesignerPropertySlider(0.02f, 0.5f, 49, Label = "Volume for Commodities", Order = 30, Tooltip = "What portion of the capsule volume is reserved for commodities (fuel and battery).  This takes space away from crew.")]
        private float _commoditiesScale = 0.05f;        

        //
        // Summary:
        //     The radius percent.
        [SerializeField]
        [DesignerPropertySlider(0f, 0.25f, 26, Label = "COM Shifter", Order = 20, Tooltip = "How far the center of mass shifts when the part is activated.")]
        private float _comShifter = 0f;       
        //
        // Summary:
        //     The height of the command pod at uniform scale of 1.
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _height = 1.27f;


        //
        // Summary:
        //     The radius percent.
        [SerializeField]
        [DesignerPropertySlider(0.75f, 1.25f, 17, Label = "Height", Order = 16, Tooltip = "How stretched the pod is in the vertical axis.")]
        private float _heightStretch = 1f;

        //
        // Summary:
        //     The scale we last adjusted to.
        private float _lastScale;

        //
        // Summary:
        //     The mass of the command pod.
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _mass = 0f;
        //
        // Summary:
        //     The percentage of volume to be used by fuel.
        [SerializeField]
        [DesignerPropertySlider(0f, 0.5f, 101, Label = "Volume for Fuel", Order = 31, Tooltip = "Define the percentage of the available volume to use for fuel. When fuel+battery exceeds 10%, it will impinge on crew capacity.", TechTreeIdForMaxValue = "Command.Battery")]
        private float _configureFuelCapacity = 0f;

        private bool _hasFuselage => Part.GetModifier<FuselageData>() != null;
        //
        // Summary:
        //     The bottom diameter at uniform scale of 1.
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _radiusBottom = 1.0f;

        //
        // Summary:
        //     The radius percent.
        [SerializeField]
        [DesignerPropertySlider(0.5f, 2.5f, 41, Label = "Radius", Order = 15, Tooltip = "The radius of the pod base.", TechTreeIdForMaxValue = "MaxSize.Capsule")]
        private float _radiusPercent = 1f;

        //
        // Summary:
        //     The top diameter at uniform scale of 1.
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _radiusTop = 0.416f;

        //
        // Summary:
        //     The are per astronaut.
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _requiredAreaPerAstronaut = 1.5f;

        //
        // Summary:
        //     The center of mass of the pod when at 100% scale.
        [SerializeField]
        [PartModifierProperty(true, false)]
        private Vector3 _unscaledCenterOfMass = new Vector3(0f, -0.4f, 0f);

                //
        // Summary:
        //     The center of mass of the pod when at 100% scale.
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _wallThickness = 0.05f;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _powerConsumption = 0.25F;

        public override float MassDry => _mass;

        //
        // Summary:
        //     Gets or sets the portion of the volume reserved for commodities
        public float CommoditiesScale
        {
            get
            {
                return _commoditiesScale;
            }
            set
            {
                _commoditiesScale = value;
                UpdateOtherModifiersAndStuff();
                base.Part.PartScript.CraftScript.SetStructureChanged();
            }
        }

        //
        // Summary:
        //     Gets or sets the center of mass shifter
        public float COMShifter
        {
            get
            {
                return _comShifter;
            }
            set
            {
                _comShifter = value;
                UpdateOtherModifiersAndStuff();
                base.Part.PartScript.CraftScript.SetStructureChanged();
            }
        }
        //
        // Summary:
        //     Gets or sets the percentage of volume to be used by monopropellant 
        public float FuelCapacity
        {
            get
            {
                return _configureFuelCapacity;
            }
            set
            {
                _configureFuelCapacity = value;
                _configureFuelCapacity = Math.Min(_configureFuelCapacity, 1f - Part.GetModifier<CommandPodData>().Battery);
                UpdateOtherModifiersAndStuff();
                base.Part.PartScript.CraftScript.SetStructureChanged();
            }
        }
        //
        // Summary:
        //     Gets the height stretch.
        //
        // Value:
        //     The height stretch.
        public float Height => _heightStretch;
        public float PowerConsumption 
        {
            get 
            {
                return _powerConsumption;
            } 
            set 
            {
                _powerConsumption = value;
            }
        }
        //
        // Summary:
        //     Gets the price of the modifier.
        //
        // Value:
        //     The price.
        public override long Price => Mathf.CeilToInt((float)base.Part.PartType.Price * ScaledSize * Height * (1.0f + 0.2f * _comShifter) - (float)base.Part.PartType.Price);

        //
        // Summary:
        //     Gets the scale.
        //
        // Value:
        //     The scale.
        public float ScaledSize => _radiusPercent * _baseScale;

        //
        // Summary:
        //     Gets or sets the scale without applying the base scale.
        //
        // Value:
        //     The scale without scaling by the base scale.
        public override float Scale
        {
            get
            {
                return _radiusPercent;
            }
            set
            {
                _radiusPercent = value;
            }
        }
            //
        // Summary:
        //     Gets the total volume of the pod in cubic meters.
        //
        // Value:
        //     The total volume of the pod in cubic meters.
        public float TotalVolume => CalculateVolume(0f);

        //
        // Summary:
        //     Gets the unscaled center of mass.
        //
        // Value:
        //     The unscaled center of mass.
        public Vector3 UnscaledCenterOfMass => Part.Activated ?  _unscaledCenterOfMass + new Vector3(0f, 0f, -_comShifter)  : _unscaledCenterOfMass;
        
        //
        // Summary:
        //     Updates the other modifiers, such as battery, fuel tank, engine, etc.
        public void UpdateOtherModifiersAndStuff()
        {
            UpdateCoM();
            _configureFuelCapacity = Math.Min(_configureFuelCapacity, 1 - Part.GetModifier<CommandPodData>().Battery);
            float num = CalculateVolume(_radiusBottom * _wallThickness) *_commoditiesScale;
            List<FuelTankData> list = new List<FuelTankData>();
            base.Part.GetModifiers(list);
            FuelTankData fuelTankData = list.FirstOrDefault((FuelTankData x) => x.FuelType == FuelType.Battery);
            fuelTankData.Capacity = 2494.8 * (double)num * base.Part.GetModifier<CommandPodData>().Battery * 1000f;
            fuelTankData.Fuel = fuelTankData.Capacity;
            FuelTankData fuelTankData2 = list.FirstOrDefault((FuelTankData x) => x.FuelType != FuelType.Battery);
            if (fuelTankData2 != null)
            {
                fuelTankData2.Capacity = (double)num * _configureFuelCapacity * 1000f;
                fuelTankData2.Fuel = fuelTankData2.Capacity;
            }
            base.Part.GetModifier<GyroscopeData>()?.SetBasePowerAndMass(0.5f * num, 0f);
            CrewCompartmentData modifier = base.Part.GetModifier<CrewCompartmentData>();
            float num2 = CalculateVolume(_radiusBottom * _wallThickness);
            float num3 = CalculateVolume(0f);
            _mass = (num3 - num2) * 3000f * 0.01f * (1f + 0.2f * _comShifter);
            float num4 = .75f * (1f - _commoditiesScale);
            float num5 = MathF.PI * Mathf.Pow(_radiusBottom * ScaledSize, 2f) * num4 ;
            modifier.Capacity = Mathf.FloorToInt(num5 / _requiredAreaPerAstronaut);
            modifier.CrewExitPosition *= ScaledSize / _lastScale;
            _lastScale = ScaledSize;
            float num6 = modifier.Script.Crew.Count - modifier.Capacity;
            for (int i = 0; (float)i < num6; i++)
            {
                EvaScript evaScript = modifier.Script.Crew[modifier.Script.Crew.Count - 1];
                List<PartConnection> partConnectionsBetweenParts = PartConnection.GetPartConnectionsBetweenParts(base.Part, evaScript.PartScript.Data);
                foreach (PartConnection item in partConnectionsBetweenParts)
                {
                    List<PartConnection> symmetricPartConnections = Symmetry.GetSymmetricPartConnections(base.Part.PartScript, item, includeSourcePart: false);
                    foreach (PartConnection item2 in symmetricPartConnections)
                    {
                        item2.DestroyConnection();
                    }

                    item.DestroyConnection();
                }
            }
        }
        //
        // Summary:
        //     Called when the part modifier data is initialized in the designer scene.
        //
        // Parameters:
        //   d:
        //     The designer part properties.
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            base.OnDesignerInitialization(d);
            d.OnValueLabelRequested(() => _commoditiesScale, (float x) => $"{Units.GetVolumeString(1000 * CalculateVolume(_radiusBottom * _wallThickness) *_commoditiesScale)}/{Utilities.FormatPercentage(x)}");
            d.OnPropertyChanged(() => _commoditiesScale, delegate (float newValue, float oldValue)
            {
                CommoditiesScale = newValue;

            }); 
            d.OnValueLabelRequested(() => _radiusPercent, (float x) => $"{Utilities.FormatPercentage(x)} ({x * _radiusBottom * _baseScale:0.00}m)");
            d.OnPropertyChanged(() => _radiusPercent, delegate
            {
                Symmetry.SynchronizePartModifiers(base.Part.PartScript);
                Symmetry.ExecuteOnSymmetricPartModifiers(this, includeSourceModifier: true, delegate (SRCapsuleData modifier)
                {
                    modifier.Script.UpdateScale(ScaledSize, repositionAttachedParts: true, Height);
                });
                base.Script.PartScript.CraftScript.SetStructureChanged();
            });
            d.OnVisibilityRequested(() => _heightStretch, (x) => !_hasFuselage);
            d.OnValueLabelRequested(() => _heightStretch, (float x) => Utilities.FormatPercentage(x) ?? "");
            d.OnPropertyChanged(() => _heightStretch, delegate
            {
                Symmetry.SynchronizePartModifiers(base.Part.PartScript);
                Symmetry.ExecuteOnSymmetricPartModifiers(this, includeSourceModifier: true, delegate (SRCapsuleData modifier)
                {
                    modifier.Script.UpdateScale(ScaledSize, repositionAttachedParts: true, Height);
                });
                base.Script.PartScript.CraftScript.SetStructureChanged();
            });
            d.OnValueLabelRequested(() => _configureFuelCapacity, (float x) => GetFuelLabel(x));
            d.OnPropertyChanged(() => _configureFuelCapacity, delegate (float newValue, float oldValue)
            {
                FuelCapacity = newValue;
            });            
            d.OnValueLabelRequested(() => _comShifter, (float x) => Units.GetPercentageString(x));
            d.OnPropertyChanged(() => _comShifter, delegate (float newValue, float oldValue)
            {
                COMShifter = newValue;
            });
        }


        //
        // Summary:
        //     Called after the modifier has been fully initialized.
        protected override void OnInitialized()
        {
            base.OnInitialized();
            _lastScale = ScaledSize;
        }

        //
        // Summary:
        //     Calculates the volume.
        //
        // Parameters:
        //   shellThickness:
        //     The shell thickness.
        //
        // Returns:
        //     The volume.
        private float CalculateVolume(float shellThickness)
        {
            float num = _radiusTop * ScaledSize - shellThickness;
            float num2 = _radiusBottom * ScaledSize - shellThickness;
            float num3 = MathF.PI * num * num;
            float num4 = MathF.PI * num2 * num2;
            float num5 = _height * ScaledSize * Height - shellThickness;
            return 1f / 3f * num5 * (num3 + num4 + Mathf.Sqrt(num3 * num4));
        }

        //
        // Summary:
        //     Updates the Center of mass.
        public void UpdateCoM()
        {
            base.Part.Config.CenterOfMass = UnscaledCenterOfMass * ScaledSize;
        }
        //
        // Summary:
        //     Gets the monoprop label.
        //
        // Parameters:
        //   x:
        //     The x.
        //
        // Returns:
        //     The label.
        private string GetFuelLabel(float x)
        {
            List<FuelTankData> list = new List<FuelTankData>();
            base.Part.GetModifiers<FuelTankData>(list);
            FuelTankData modifier = list.FirstOrDefault(y => y.FuelType != FuelType.Battery);
            if (modifier != null)
            {
                return Units.GetMassString((float)(modifier.Capacity * (double)modifier.FuelType.Density * 0.01)) + " / " + Utilities.FormatPercentage(x);
            }

            return Utilities.FormatPercentage(x) ?? "";
        }
        public string DesignerCategory {get {return "Command";}}

        private int _srpriority = 1;
        public int SRPriority {get { return _srpriority;}}

        public SRCapsuleData(){}

        public SRCapsuleData(IProgramManager pm)
        {
            _pm=pm;
        }

        public Dictionary<string, List<string>> FamilyInfo {get;} = new Dictionary<string, List<string>>()
        {
            {"CrewCompartment", new List<string>(){"capacity"}},
            {"SpaceRace.SRCapsule", new List<string>(){"radiusTop", "radiusBottom", "height"}},
            {"SpaceRace.SRAvionics", new List<string>(){"guidance", "maxThrustScale"}}
        };

        public Dictionary<string, List<string>> PartInfo {get;} = new Dictionary<string, List<string>>()
        {
            {"CrewCompartment", new List<string>(){"capacity"}},
            {"SpaceRace.SRCapsule", new List<string>(){"radiusTop", "radiusBottom", "height"}},
            {"FuelTank", new List<string>(){"capcacity"}},
            {"SpaceRace.SRAvionics", new List<string>(){"guidance", "maxThrustScale", "communicationScale"}}
        };

        public TechList Technologies(PartData part, bool family = false)
        {
            return new TechList()
            {
                new Technology(TypeId,"Capsule"),
                new Technology("SpaceRace.SRAvionics",string.Format("Guidance:{0}",part.GetModifier<SRAvionicsData>().Guidance)),
                new Technology(TypeId,string.Format("CapsuleCapacity:{0:D}", part.GetModifier<CrewCompartmentData>().Capacity))
            };

        }

        public Dictionary<string, string> DevPanelDisplay(PartData part)
        {
            return new Dictionary<string, string>(){
                {"Capacity", part.GetModifier<CrewCompartmentData>().Capacity.ToString()},
                {"Guidance", part.GetModifier<SRAvionicsData>().Guidance},
                {"Max Thrust", Units.GetForceString(part.GetModifier<SRAvionicsData>().MaxThrust)},
                {"Com Range", Units.GetDistanceString((float)part.GetModifier<SRAvionicsData>().MaxComDistance)},
                {"Battery", Units.GetEnergyString((float)part.GetModifier<FuelTankData>().Capacity * 1000f)}
            };
        }

        private int LargestCapacityDeveloped()
        {
            int value = 1;
            foreach (TechOccurenceTriple triple in _pm.Data.PartTechDeveloped)
            {
                if (triple.Tech.Split(":")[0] == "CapsuleCapacity" && triple.Occurences > 0)
                value = math.max(value, int.Parse(triple.Tech.Split(":")[1]));
            }
            return value;
        }

        EfficiencyFactor ISRPartMod.EfficiencyFactor(string tech, int timesDeveloped, bool contractorDeveloped, ProjectCategory category)
        {
            switch (category)
            {
                case ProjectCategory.Part:
                if (tech == "Capsule")
                {
                    if (timesDeveloped == 0) return new EfficiencyFactor("First Manned Spacecraft", .75F);
                    if (!contractorDeveloped) return new EfficiencyFactor("Unfamiliar with Capsules", .25F);
                }
                if (tech.Split(":")[0] == "CapsuleCapacity")
                {
                    int difference = int.Parse(tech.Split(":")[1]) - LargestCapacityDeveloped();
                    if (difference > 0) 
                    return new EfficiencyFactor("Largest Manned Spacecraft", .2F * difference);
                }
                if (tech.Split(":")[0] == "PartFamily")
                {
                    if (timesDeveloped > 0) 
                        if (contractorDeveloped) 
                        {
                            if (!Game.IsCareer || _pm.GameState.Validator.IsItemAvailable("SpaceRace.ModularCapsules"))
                            {
                                return new EfficiencyFactor("Variant of our modular Capsule", -.95F);
                            }
                            else return new EfficiencyFactor("Variant of our Capsule", -.75F);

                        }
                        else return new EfficiencyFactor("Vairant of a Capsule", -.5F);
                }
                else if (tech.Split(':')[0] == "Guidance" && !contractorDeveloped)
                {
                    if (tech.Split(':')[1] == "Unguided") return null;
                    if (timesDeveloped == 0) 
                    {
                        return new EfficiencyFactor(){
                            Text = "Novel guidance: "+ tech.Split(':')[1],
                            Penalty = 0.5f
                        };
                    }
                    else if (timesDeveloped == 1)
                    {
                        return new EfficiencyFactor(){
                            Text = "Proprietary guidance: "+ tech.Split(':')[1],
                            Penalty = 0.2f
                        };
                    }
                    else 
                    {
                        return new EfficiencyFactor(){
                            Text = "Unfamiliar guidance: " + tech.Split(':')[1],
                            Penalty = 0.1f
                        };
                    }
                }
                return null;
                case ProjectCategory.Stage:
                {
                    if (tech == "Capsule" && !contractorDeveloped) return new EfficiencyFactor("Unfamiliar with Capsules", .25F);
                    if (tech.Split(':')[0] == "Guidance" && timesDeveloped == 0) 
                    {
                        return new EfficiencyFactor(){
                            Text = "Novel guidance: "+ tech.Split(':')[1],
                            Penalty = 0.2f
                        };
                    }
                }
                return null;
                case ProjectCategory.Integration:
                if (tech == "Capsule") return new EfficiencyFactor("Crew Rated", .50F);
                if (tech.Split(':')[0] == "Guidance" && timesDeveloped == 0) 
                {
                    return new EfficiencyFactor(){
                        Text = "Novel guidance: "+ tech.Split(':')[1],
                        Penalty = 0.2f
                    };
                }
                return null;
                default:
                return null;

            }
        }

        public PartDevBid GenericBid(PartData part)
        {
            return new PartDevBid(_pm, this)
            {
                BaseDevTime = 400 * SRFormulas.SecondsPerDay,
                DevCostPerDay = (long)(part.Price * 10.0 / 400.0),
                FamilyName = part.Name+ " Family",
                Name = part.Name,
                DesignerDescription = $"Capsule for {part.GetModifier<CrewCompartmentData>().Capacity} crew",
                UnitProductionRequired = part.Price,
                ProductionCapacity = part.Price / (90.0 + 30.0 * part.GetModifier<CrewCompartmentData>().Capacity),
                Technologies = Technologies(part)
            };
        }
        public double FamilyStageCost(PartData part)
        {
            SRCapsuleData data = part.GetModifier<SRCapsuleData>();
            return 0.2 * part.PartType.Price * data.ScaledSize * data.Height;
        }
    }
}