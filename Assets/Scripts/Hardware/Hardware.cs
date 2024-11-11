namespace Assets.Scripts.SpaceRace.Hardware
{
    using System.Collections;
    using System.Collections.Generic;

    using UnityEngine;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Linq;
    using System;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using Assets.Scripts.SpaceRace.Projects;
    using Assets.Scripts.SpaceRace.Collections;
    using System.Xml.Linq;
    using ModApi.Math;

    public class Hardware
    {
        [XmlAttribute]
        public int Id;
        [XmlAttribute]  
        public string Name;
        [XmlAttribute] 
        public int ContractorId = 0;
        [XmlAttribute]
        public bool Developed = false;
        [XmlAttribute]  
        public bool Active = true;
        public TechList Technologies = new TechList();
        /// <summary>
        /// Production required to produce this part (in dollars)
        /// </summary>
        [XmlAttribute] 
        public double ProductionRequired;
        public long Price => (long)(ProductionRequired * ContractorMarkup);
        /// <summary>
        /// The markup charged by the contractor. Price = ProductionRequired * Markup
        /// </summary>
        [XmlAttribute]
        public double ContractorMarkup = 1.0;

    }

    public class SRPart : Hardware
    {
        [XmlAttribute]  
        public string PrimaryMod;
        [XmlAttribute]
        public string DesignerCategory;
        [XmlAttribute]  
        public int FamilyId = 0;
        [XmlAttribute]
        public string DesignerDescription = string.Empty;
        public XElement Data;
    }

    public class SRCraftConfig : ILaunchable
    {
        [XmlAttribute]
        public int Id {get; set;}
        [XmlAttribute]
        public string Name {get; set;}
        public SRCraftConfig Config => this;
        public IntegrationStatus Status {get; set;} = IntegrationStatus.Configuration;
        public double TimeToCompletion => 0.0;
        [XmlAttribute]
        public int Integrations = 0;
        [XmlAttribute]
        public float SimilarIntegrations = 0F;
        [XmlAttribute]
        public bool Active = true;
        [XmlAttribute]
        public double ProductionRequired;
        public string ProductionDescription;
        [XmlAttribute]
        public long UndevelopedPartPrice;
        public HardwareOccurenceList Stages = new StageOccurenceList();
        public HardwareOccurenceList Parts = new HardwareOccurenceList(); 
        public List<HardwareOrder> Orders = new List<HardwareOrder>();
        public TechList Technologies = new TechList();
        [XmlAttribute]
        public float Mass = 0F;
        [XmlAttribute]
        public float Height = 0F;
        [XmlAttribute]
        public float Width = 0F;
        public List<(string, double, long)> Fuel = new List<(string, double, long)>();
        public float Similarity(SRCraftConfig config)
        {
            return config == null? 0F : MathF.Max(Stages.Similarity(config.Stages), Parts.Similarity(config.Parts));
        }
        public SRCraftConfig(){}
        public SRCraftConfig(VerboseCraftConfiguration vconfig, string name, IntegrationStatus status = IntegrationStatus.Configuration)
        {
            Status = status;
            Name = name;
            Technologies = new TechList(vconfig.Stages.Keys.Select(stage => stage.Technologies));
            Technologies.Merge(new TechList(vconfig.Parts.Values.Select(p => p.Technologies)));
            Stages = new HardwareOccurenceList(vconfig.Stages.Keys.Select(stage => new HardwareOccurencePair(stage.Id,1)));
            Parts = new HardwareOccurenceList(vconfig.Parts.Values.Select(part => new HardwareOccurencePair(part.Id == 0? part.FamilyId : part.Id,1)));
            Orders = vconfig.Orders;
            UndevelopedPartPrice = vconfig.UndevelopedPartPrice;
            Mass = vconfig.MassWet;
            Height = vconfig.Height;
            Width = vconfig.Width;
            Fuel = vconfig.Fuels.Keys.Select(f => (f.Id, vconfig.Fuels[f], (long)(f.Density * f.Price * vconfig.Fuels[f]))).ToList();
        }
        public override string ToString()
        {
            return $"Craft Config: Stages = {Stages.ToString()} Parts = {Parts.ToString()}";
        }
    }

    public class SRStage : Hardware
    {
        public FamilyOccurenceList PartFamilies = new FamilyOccurenceList(); 
        [XmlIgnore]
        public List<(SRPartFamily, int)> FamiliesOverride = null;
        public SRStage(){}
        public override string ToString()
        {
            string s = "PartFamilies ";
            foreach (HardwareOccurencePair fop in PartFamilies)
            {
                s += string.Format("({0:D}, {1:D}) ", fop.Id, fop.Occurences);
            }
            return s;
        }
        public string CostBreakdown(IHardwareManager hm)
        {
            string s = string.Empty;
            if (FamiliesOverride != null)
            {
                foreach ((SRPartFamily, int) pair in FamiliesOverride)
                {
                    s += string.Format("{0}: {1} x {2:D} = {3}\n", pair.Item1.Name, Units.GetMoneyString((long)pair.Item1.StageProduction), pair.Item2, Units.GetMoneyString((long)pair.Item1.StageProduction * pair.Item2));
                }
                return s;
            }
            foreach (HardwareOccurencePair fop in PartFamilies.Where(fop => fop.Id > 0))
            {
                SRPartFamily fam = hm.Families[fop.Id];
                s += string.Format("{0}: {1} x {2:D} = {3}\n", fam.Name, Units.GetMoneyString((long)fam.StageProduction), fop.Occurences, Units.GetMoneyString((long)fam.StageProduction + fop.Occurences));
            }
            return s;
        }
    }

}