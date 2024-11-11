namespace Assets.Scripts.SpaceRace.Hardware
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using System.IO;
    using UnityEngine;

    using ModApi.Craft.Parts;
    using ModApi.Common.Extensions;
    using ModApi.Flight;
    using ModApi.Scenes.Events;
    using ModApi.Flight.Events;  

        
    using Assets.Scripts.SpaceRace.Modifiers;
    using Assets.Scripts.SpaceRace.Projects;
    using Unity.Mathematics;
    using Assets.Scripts.Design;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.Formulas;
    using System.Runtime.CompilerServices;
    using UnityEngine.Assertions.Must;

    public class HardwareManager : IHardwareManager
    {
        private HardwareDB _db;

        public HardwareDB DB 
        {
            get 
            {
                return _db;
            }
        }

        private IProgramManager _pm;
        private XmlSerializer _programSerializer; 
        private XmlSerializer _hardwareSerializer; 
        private XmlSerializer _historySerializer; 

        private DesignerPartList _dpl => Game.Instance.CachedDesignerParts;
        private List<DesignerPart> _cachedParts = new List<DesignerPart>();

        private string _datafolder => Application.persistentDataPath + 
            "/UserData/SpaceRace/" + 
            Game.Instance.GameState.Id;
        
        private string _hardwareFile => _datafolder + "/Active/Hardware.xml";

        private string _integrationsFolder => _datafolder + "/Integrations/";

        public Dictionary<int, SRPart> Parts {get; set;}

        public Dictionary<int, SRPartFamily> Families {get; set;}
        public Dictionary<int, SRStage> Stages {get; set;}

        public Dictionary<int, SRCraftConfig> Craft {get; set;}

        public Dictionary<int, Hardware> Hardware {get; set;}

        private CraftDesigns _integrations;
        public HardwareManager(IProgramManager pm, HardwareDB db)
        {
            _pm = pm;
            _db = db;
        }
        public void Initialize()
        {
            Parts = new Dictionary<int, SRPart>();
            Families = new Dictionary<int, SRPartFamily>();
            Stages = new Dictionary<int, SRStage>();
            Craft = new Dictionary<int, SRCraftConfig>();
            Hardware = new Dictionary<int, Hardware>();
            foreach (SRPart part in _db.Parts) 
            {
                Parts[part.Id] = part;
                Hardware[part.Id] = part;
            }
            foreach (SRStage stage in _db.Stages) 
            {
                Stages[stage.Id] = stage;
                Hardware[stage.Id] = stage;
            }
            foreach (SRPartFamily fam in _db.Families) Families[fam.Id] = fam;
            foreach (SRCraftConfig config in _db.CraftConfigs) Craft[config.Id] = config;
            CacheAllDesignerParts();
            _integrations = new CraftDesigns(_datafolder+"Integrations/");
        }
        /// <summary>
        /// Gets a text description of the object in the database with id i
        /// </summary>
        /// <param name="i"> the id </param>
        /// <returns> a description string </returns>
        public string Description(int i)
        {
            SRPartFamily wf = _db.Families.Find(x => x.Id == i);
            if (wf != null) return wf.Name;
            SRPart hw = _db.Parts.Find(x => x.Id == i);
            if (hw != null) return hw.Name;
            SRStage sRStage = _db.Stages.Find(x => x.Id == i);
            if (sRStage != null) return sRStage.Name;
            return string.Empty;
        }
        public string Description(HardwareOccurencePair hop)
        {
            return $"{Description(hop.Id)} x{hop.Occurences}";
        }
        public string Description(HardwareOccurenceList hol)
        {
            return string.Join(", ", hol.Select(hop => Description(hop)));
        }
        public T Get<T>(Predicate<T> condition) where T : class
        {
            Type t = typeof(T);
            if (typeof(SRPartFamily).IsAssignableFrom(t))
            {
                T match = DB.Families.Find(f => condition(f as T)) as T;
                if (match != null) return match;
            }
            if (typeof(SRStage).IsAssignableFrom(t))
            {
                T match = DB.Stages.Find(f => condition(f as T)) as T;
                if (match != null) return match;
            }
            if (typeof(SRPart).IsAssignableFrom(t))
            {
                T match = DB.Parts.Find(f => condition(f as T)) as T;
                if (match != null) return match;
            }
            if (typeof(SRCraftConfig).IsAssignableFrom(t))
            {
                T match = DB.CraftConfigs.Find(f => condition(f as T)) as T;
                if (match != null) return match;
            }
            return null;
        }

        public SRCraftConfig GetCraftConfig(Predicate<SRCraftConfig> match)
        {
                return DB.CraftConfigs.Find(c => match(c));
        }

        public double SumOverCraftConfigs(Func<SRCraftConfig, double> selector)
        {
            return _db.CraftConfigs.Sum(selector);
        }

        public int GetPartId(PartData newPart)
        {
            XElement newPartXml = newPart.GenerateXml(null, false);
            return GetPartId(newPartXml);
        }
        public int GetPartId(XElement x)
        {
            string primarysrmod = _pm.GetPrimarySpaceRaceModName(x);
            if (primarysrmod == null) return -1;
            XElement srpartdata = _pm.GetData(x, primarysrmod);
            foreach (SRPart sRPart in _db.Parts)
            {
                if (XNode.DeepEquals(srpartdata,sRPart.Data))
                {
                    return sRPart.Id;
                }
            }
            return 0;
        }


        public int GetFamilyId(XElement x)
        {
            string primarysrmod = _pm.GetPrimarySpaceRaceModName(x);
            if (primarysrmod == null) return -1;
            XElement famdata = _pm.GetFamData(x, primarysrmod);
            foreach (SRPartFamily fam in _db.Families)
            {
                if (XNode.DeepEquals(famdata,fam.Data))
                {
                    return fam.Id;
                }
            }
            return 0;
        }
        public SRPartFamily GetPartFamily(int i)
        {
            if (Families.TryGetValue(i, out SRPartFamily fam))
            {
                return fam;
            }
            return Families[Parts[i].FamilyId];
        }
        public int GetStageId(SRStage stage)
        {
            SRStage match = _db.Stages.Find(st => stage.PartFamilies.IsSame(st.PartFamilies));
            return match?.Id ?? 0;
        }
        public void DevelopId(int i)
        {
            if (i<= 0) new ArgumentException("Negative index");
            SRPartFamily fam = _db.Families.FirstOrDefault(f => f.Id == i);
            if (fam != null)
            {
                fam.AnyDeveloped = true;
                Debug.Log("Developing Part Family " + i.ToString());
                return;
            }
            SRPart part = _db.Parts.FirstOrDefault(e => e.Id == i);
            if (part!= null) 
            {
                part.Developed = true;
                Debug.Log("Developing Part " + i.ToString());
                return;
            }
            SRStage stage = _db.Stages.FirstOrDefault(s => s.Id == i);
            if (stage != null)
            {
                stage.Developed = true;
                Debug.Log("Developing Stage " + i.ToString());
                return;
            }
            throw new ArgumentException("Nothing to develop in parts database with id: "+i.ToString());
        }
        public void IncrementCraftIntegrations(int i)
        {
            SRCraftConfig config = _db.CraftConfigs.First(c => c.Id == i);
            if (config != null) 
            {
                config.Integrations +=1;
                foreach (SRCraftConfig config2 in _db.CraftConfigs)
                {
                    config2.SimilarIntegrations = MathF.Max(config2.SimilarIntegrations, Math.Min(config2.SimilarIntegrations + config.Similarity(config2), 2F * SRFormulas.NovelIntegrations * config.Similarity(config2)));
                }
                foreach (HardwareOccurencePair hop in config.Parts)
                {
                    GetPartFamily(hop.Id).Integrations += hop.Occurences;
                }
            }
            else throw new ArgumentException("No craft configuration with id: "+i.ToString());
        }
        public float GetStartingSimilarIntegrations(SRCraftConfig config)
        {
            float sim = 0F;
            List<SRCraftConfig> list = new List<SRCraftConfig>(_db.CraftConfigs);
            foreach(SRCraftConfig config2 in list.OrderBy(con => config.Similarity(con)))
            {
                sim = MathF.Min(sim + config.Similarity(config2) * config2.Integrations, 2F * SRFormulas.NovelIntegrations * config.Similarity(config2));
            }
            return sim;
        }

        private void CacheAllDesignerParts()
        {
            _cachedParts.Clear();
            foreach (XElement partXml in _db.PartsXml.Elements("Part")) 
            {
                CacheDesignerPart(partXml);
            }
        }
        public void RemoveAllDesignerParts()
        {
            foreach (DesignerPart part in _cachedParts) 
            {
                _dpl.DeleteSubassembly(part);
            }
        }

        private void CacheDesignerPart(XElement partXml)
        {
            int i = partXml.GetIntAttribute("id");
                if (!string.IsNullOrEmpty(Parts[i].DesignerCategory))
                {
                    XElement dp = new XElement
                    (
                        "DesignerPart",
                        new XAttribute ("name", Parts[i].Name),
                        new XAttribute ("spacerace", "true"),
                        new XAttribute ("category", Parts[i].DesignerCategory),
                        new XAttribute ("description", Parts[i].DesignerDescription),
                        new XAttribute ("order", i+100),
                        new XAttribute ("showInDesigner", "true"),
                        new XAttribute ("snapshotDistanceScaler", "1"),
                        new XAttribute ("snapshotRotation", "-25,100,0"),
                        new XElement(
                            "Assembly",
                            new XElement("Parts", partXml)
                        )
                    );
                    _dpl.AddDesignerPart(dp);
                    _cachedParts.Add(_dpl.Parts.Last());
                }
        }
        public int AddStandaloneFamily(SRPartFamily fam)
        {
            SRPartFamily match = _db.Families.Find(p => XNode.DeepEquals(fam.Data, p.Data));
            if (match != null)
            {
                return match.Id;
            }
            if (fam.DesignerCategory != null)
            {
                throw new ArgumentException("Cannot add a standalone family if the family produces designer parts.");
            }
            int i = _db.NextId;
            fam.AnyDeveloped= true;
            fam.Id = i;
            _db.Families.Add(fam);
            Families[i] = fam;
            _db.NextId += 1;
            return i;
        }
        public SRPart AddPart(PartData part, PartDevBid offer)
        {
            XElement partXml = part.GenerateXml(null, false);
            string primarysrmod = _pm.GetPrimarySpaceRaceModName(partXml);
            XElement partdata = _pm.GetData(partXml, primarysrmod);
            SRPart match = _db.Parts.Find(p => XNode.DeepEquals(partdata, p.Data));
            if (match != null)
            {
                if (Game.InDesignerScene) Game.Instance.Designer.DesignerUi.ShowMessage("Part "+match.Name+" already exists in database.");
                return null;
            }

            XElement famdata = _pm.GetFamData(partXml, primarysrmod);
            SRPartFamily fammatch = _db.Families.Find(fam => XNode.DeepEquals(famdata, fam.Data)) ??
                new SRPartFamily(){
                    Id = _db.NextId,
                    Name = offer.FamilyName?? part.Name+" Family",
                    PrimaryMod = primarysrmod,
                    Data = _pm.GetFamData(partXml, primarysrmod),
                    Technologies = _pm.GetTechList(part, primarysrmod, true),
                    StageProduction = _pm.GetFamilyStageCost(part, primarysrmod),
                    DesignerCategory = offer.DesignerCategory
                };
            _db.Families.Add(fammatch);
            Families[_db.NextId] = fammatch;
            _db.NextId += 1;
            
            TechList parttechlist = _pm.GetTechList(part, primarysrmod, false);
            parttechlist.AddTech(primarysrmod, string.Format("PartFamily:{0:D}",fammatch.Id), fammatch.Name);
            SRPart sRPart =new SRPart(){
                Id = _db.NextId,
                Name = offer.Name ?? part.Name,
                ContractorId = offer.ContractorId,
                PrimaryMod = offer.PartMod.TypeId,
                FamilyId = fammatch.Id,
                Data = _pm.GetData(partXml, primarysrmod),
                Technologies = parttechlist,
                DesignerCategory = offer.DesignerCategory,
                DesignerDescription = offer.DesignerDescription,
                ProductionRequired = offer.UnitProductionRequired,
                ContractorMarkup = offer.Markup
            };
            _db.Parts.Add(sRPart);
            Parts[_db.NextId] = sRPart;
            Hardware[_db.NextId] = sRPart;
            if (!string.IsNullOrEmpty(offer.DesignerCategory))
            {
                partXml.SetAttributeValue("id", _db.NextId);
                partXml.SetAttributeValue("name", offer.Name ?? part.Name);
                partXml.SetAttributeValue("position", (0,0,0));
                partXml.SetAttributeValue("rotation", (0,0,0));
                partXml.Attribute("rootPart")?.Remove();
                partXml.Element("FlightProgram")?.Element("Program")?.Remove();
                _db.PartsXml.Add(partXml);
                CacheDesignerPart(partXml);
            }
            _db.NextId += 1;
            return sRPart;
        }
        public int AddStage(SRStage stage)
        {
            SRStage match = _db.Stages.Find(st => stage.PartFamilies.IsSame(st.PartFamilies));
            if (match != null)
            {
                if (Game.InDesignerScene) Game.Instance.Designer.DesignerUi.ShowMessage("Stage "+match.Name+" already exists in database.");
                return 0;
            }
            stage.Id = _db.NextId;
            _db.Stages.Add(stage);
            Stages[stage.Id] = stage;
            Hardware[stage.Id] = stage;
            _db.NextId += 1;
            return _db.NextId - 1;
        }
        public SRCraftConfig AddCraftConfig(VerboseCraftConfiguration vconfig, string name)
        {
            int i = _db.NextId;
            SRCraftConfig config = new SRCraftConfig(vconfig, name);
            foreach (HardwareOrder order in config.Orders)
            {
                order.Description = GetDescription(order.HardwareRequired);
            }
            config.Id = i;
            config.SimilarIntegrations = GetStartingSimilarIntegrations(config);
            _db.CraftConfigs.Add(config);
            Craft[i] = config;
            _db.NextId += 1;
            return config;
        }
        public string GetDescription(HardwareOccurenceList list)
        {
            string description = string.Empty;
            foreach (HardwareOccurencePair hop in list)
            {
                if (hop.Occurences > 1)
                {
                    description += $"{Hardware[hop.Id].Name} x {hop.Occurences}, ";
                }
                else
                {
                    description += $"{Hardware[hop.Id].Name}, ";
                }
            }
            return description;
        }
        public void MultiplyUnitCost(int id, float overrun)
        {
            if (Hardware.TryGetValue(id, out Hardware hardware))
            {
                hardware.ProductionRequired *= overrun;
            }
            else throw new ArgumentException("ID does not match known hardware.");
        }
        public SRStage NewStage(string craftname, int number, TechList techs, FamilyOccurenceList fol)
        {
            string name = craftname + " Stage " + (number+1).ToString();
            foreach (HardwareOccurencePair fop in fol)
            {
                SRPartFamily fam = _db.Families.Find(f => f.Id == fop.Id && f.Technologies.Contains("SpaceRace.SRCapsule", "Capsule"));
                if (fam != null) name = fam.Name + " Stage";
                break;
            }
            return new SRStage()
            {
                Id = 0,
                Name = name,
                PartFamilies = fol,
                Technologies = new TechList(techs),
                ProductionRequired = fol.Where(fop => fop.Id > 0).Sum(fop => Families[fop.Id].StageProduction * fop.Occurences)
            };
        }

        public void DeletePart(int i)
        {
            _db.Parts.Remove(Parts[i]);
            XElement partXml = _db.PartsXml.Elements("Part").FirstOrDefault(el => el.GetIntAttribute("id") == i);
            partXml?.Remove();
            Hardware.Remove(i);
            Parts.Remove(i);

        }
        public void DeleteStage(int i)
        {
            _db.Stages.Remove(Stages[i]);
            Hardware.Remove(i);
            Stages.Remove(i);
        }
    }
}
