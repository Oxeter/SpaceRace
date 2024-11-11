namespace Assets.Scripts.SpaceRace.Projects
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Serialization;
    using Assets.Scripts.SpaceRace.Formulas;
    using Assets.Scripts.SpaceRace.Collections;
    using UnityEngine;
    using ModApi.Math;
    using System;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Modifiers;
    using ModApi.Craft.Parts;
    using System.Linq;
    using Unity.Mathematics;
    using Assets.Scripts.Design;

    public class CostCalculator : ICostCalculator
    {
        private IProgramManager _pm;
        public CostCalculator(IProgramManager pm)
        {
            _pm = pm;
        }

        public void AdjustForMarketForces(List<PartDevBid> bids)
        {
            if (bids.Count > 1)
            {
                bids.Sort((a,b) => (int)(b.TotalProjectedCost - a.TotalProjectedCost));
                if (bids[0].TotalProjectedCost < 0.9 * bids[1].TotalProjectedCost)
                {
                    bids[0].DevCostPerDay = (long)(bids[1].DevCostPerDay * bids[1].ProjectedDevelopmentTime / bids[0].ProjectedDevelopmentTime);
                    bids[0].Comments= new List<string>(){"We can develop this much more easily than our competitors, but we see no reason to pass the savings on to you."};
                }
            }
        }


        private double Diameter(SRStage stage)
        {
            return stage.Technologies
                .Where(tech => tech.Mod == "SpaceRace.SRFuelTank" && tech.Tech.Split(":")[0] == "Diameter")
                .Select(tech => double.Parse(tech.Tech.Split(":")[2]))
                .Append(0.1)
                .Max();
        }

        private Dictionary<string, double> TankConstructionFactor = new Dictionary<string, double>
        {
            {"Steel Wall",1.0},
            {"HP Steel Wall",1.0},
            {"Aluminium Wall",2.0},
            {"HP Aluminium Wall",2.0},
            {"Aluminium Alloy Wall",2.5},
            {"HP Aluminium Alloy Wall",2.5},
            {"Steel Balloon",10.0},
            {"Aluminium Stringers",3.0},
            {"Aluminium Alloy with Stringers",3.5},
            {"Aluminium Isogrid",6.0},
            {"Aluminium Alloy Isogrid",7.0},
        };

        private double TankDevPrice(string cons, double diameter)
        {
            return diameter * diameter * 100000 * TankConstructionFactor[cons];
        }

        private double StageFuelFactor(TechList list)
        {
            if (list.Any(tech => tech.Mod == "SpaceRace.SRFuelTank" && tech.Tech =="Liquid"))
            {
                return 1.0;
            }
            if (list.Any(tech => tech.Mod == "SpaceRace.SRFuelTank" && tech.Tech =="Solid"))
            {
                return 0.5;
            }
            else return 0.25;
        }

        private double DevTimeFactor(IContractorScript con)
        {
            return 1.0 - 0.5*con.Data.Attitude.Optimism + 2.0 * con.Data.Attitude.Patience * con.Data.Attitude.Patience;
        }
        private double DevCostFactor(IContractorScript con)
        {
            double activeFactor = con.Active? 0.2 :0;
            return 0.75 - 0.2*con.Data.Attitude.Competitiveness  + 0.5 * con.Data.Attitude.Size + activeFactor+ 0.1 * con.ActiveDevelopments* con.ActiveDevelopments;
        }
        private double Markup(IContractorScript con)
        {
            double activeFactor = con.Active? 0.1 :0;
            return 1.20 - 0.2*con.Data.Attitude.Competitiveness + 0.4*con.Data.AverageRecentRush + 0.2*con.Data.Attitude.Competence +activeFactor;
        }


        private List<string> Comments(IContractorScript con, TechList tech)
        {
            List<string> list = new List<string>(){};
            if (con.ActiveDevelopments > 1) list.Add("Our development teams are already busy, but for the right price we'll take this on.");
            foreach (Technology item in tech)
            if (con.Data.DevelopedTechs.Contains(item) && !string.IsNullOrEmpty(item.Description)) list.Add($"We are experienced with {item.Description}.");
            if (list.Count==0) list.Add("We hope you accept our bid");
            return list;
        }

        private PartDevBid ContractorBid(PartDevBid generic, IContractorScript con)
        {
            return new PartDevBid(_pm, generic.PartMod, generic.DesignerCategory)
            {
                ContractorId = con.Id,
                BaseDevTime = generic.BaseDevTime * DevTimeFactor(con),
                DevCostPerDay = (long)(generic.DevCostPerDay * DevCostFactor(con) * Math.Pow(DevTimeFactor(con), -1.5)),
                FamilyName = generic.FamilyName,
                Name = generic.Name,
                DesignerDescription = generic.DesignerDescription,
                UnitProductionRequired = generic.UnitProductionRequired,
                ProductionCapacity = math.max(generic.UnitProductionRequired * generic.BatchSize / GetProductionDays(generic.UnitProductionRequired, con) + math.max(0.0, 0.5 - con.Data.AverageRecentInventory)*con.Data.BaseProductionRate, con.Data.BaseProductionRate),
                Technologies = generic.Technologies,
                Markup = Markup(con),
                Comments = Comments(con, generic.Technologies)
            };
        }
        public List<PartDevBid> GetPartDevBids(PartData part, ISRPartMod mod, List<IContractorScript> contractors, Technology famtech)
        {
            PartDevBid generic = mod.GenericBid(part);
            generic.Technologies.Add(famtech);
            List<PartDevBid> list = contractors.Select(con => ContractorBid(generic, con)).ToList();
            AdjustForMarketForces(list);
            return list;
        }

        public List<StageDevBid> GetStageBids(List<SRStage> stages, List<IContractorScript> contractors, TechOccurenceList techlist, SRStage previous = null, SRStage following = null)
        {
            List<double> productionsRequired = stages.Select(st => st.ProductionRequired).ToList();
            double baseDevTime = 50.0 * Math.Max(1.0, Math.Log(productionsRequired.Sum(), 10.0) - 3)* Formulas.SRFormulas.SecondsPerDay;
            TechList technologies = new TechList(stages.Select(st => st.Technologies));
            double fuelSpeedFactor = StageFuelFactor(technologies);
            return contractors
            .Select(con => new StageDevBid(_pm, con.Data.Id, null)
            {
                BaseDevTime = baseDevTime * DevTimeFactor(con) * fuelSpeedFactor,
                Technologies = technologies,
                DevCostPerDay = (long)(productionsRequired.Sum() / 10 * DevCostFactor(con) * Math.Pow(DevTimeFactor(con), -1.5) / fuelSpeedFactor),
                ProductionsRequired = productionsRequired,
                ProductionsDescription = string.Join("\n", stages.Select(st => $"{st.Name}\n{st.CostBreakdown(_pm.HM)}")),
                Markup = Markup(con),
                ProductionCapacity = Math.Max(productionsRequired.Sum() / GetProductionDays(productionsRequired.Sum(), con) + math.max(0.0, 0.5 - con.Data.AverageRecentInventory)*con.Data.BaseProductionRate, con.Data.BaseProductionRate),
                Comments = Comments(con, technologies)
            })
            .ToList();
        }
        public double GetProductionDays(double cost, IContractorScript contractor, double baseDays = 20.0)
        {
            return baseDays * Math.Pow(Math.Max(Math.Log(cost, 10.0) - 3, 1), 1f - 0.5 * contractor.Data.Attitude.Size) * (1.5f-contractor.Data.Attitude.Optimism);
        }
        public int GetMaxTechnicians(SRCraftConfig config)
        {
            int techs = 0;
            for (int i = 0; i < config.Stages.Count(); i++)
            {
                HardwareOccurencePair stagePair = config.Stages[i];
                SRStage stage = _pm.HM.Stages[stagePair.Id];
                techs += (int)(stage.ProductionRequired * stagePair.Occurences / math.sqrt(i+1) / SRFormulas.TechProgress / 50);
            }
            return Math.Max(techs, 10);
        }
        public double GetIntegrationProduction(SRCraftConfig config, ref string desc)
        {
            double production = 0.0;
            int previousContractor = 0;
            for (int i=0; i < config.Stages.Count(); i++)
            {
                HardwareOccurencePair stagePair = config.Stages[i];
                SRStage stage = _pm.HM.Stages[stagePair.Id];
                if (i==0)
                {
                    production += stage.ProductionRequired * 0.5 * stagePair.Occurences;
                    desc += $"{stage.Name}: {Units.GetMoneyString((long)stage.ProductionRequired)} x {Units.GetPercentageString(0.5f)} (First Stage)\n";
                }
                else if (previousContractor == stage.ContractorId)
                {
                    production += stage.ProductionRequired * stagePair.Occurences;
                    desc += $"{stage.Name}: {Units.GetMoneyString((long)stage.ProductionRequired)} \n";
                }
                else
                {
                    production += stage.ProductionRequired * 1.2 * stagePair.Occurences;
                    desc += $"{stage.Name}: {Units.GetMoneyString((long)stage.ProductionRequired)} x {Units.GetPercentageString(1.2f)} (Different Contractor)\n";

                }                    
                if (stagePair.Occurences > 1)
                {
                    desc += $" x {stagePair.Occurences}";
                }
                previousContractor = stage.ContractorId;
            }
            if (config.Technologies.Any(tech => tech.Mod == "Spacerace.Capsules"))
            {
                production *= 1.5;
                desc += $"Crew Rated: + {Units.GetPercentageString(0.5F)}\n";
            }
            return production;
        }
        public double GetIntegrationRollout(SRCraftConfig config)
        {
            return config.ProductionRequired / 4;
        }
        public double GetIntegrationRepair(SRCraftConfig config)
        {
            return config.ProductionRequired / 3;
        }
        public List<EfficiencyFactor> IntegrationEfficiencyFactors(SRCraftConfig config)
        {
            List<EfficiencyFactor> list = config.Technologies.Select(tech => _pm.GetEfficiencyFactor(tech, 0, ProjectCategory.Integration)).Where(y => y != null).ToList();
            float similarity = config.SimilarIntegrations > 2F * SRFormulas.NovelIntegrations? -0.5F : math.pow(2.0F, 1.0F - config.SimilarIntegrations / SRFormulas.NovelIntegrations) -1F;
            if (similarity > 0) list.Add(new EfficiencyFactor("Novel Configuration", similarity, "Decreases as this design is integrated more times.  Partial credit for integrating designs with parts/stages in common."));
            if (similarity < 0) list.Add(new EfficiencyFactor("Familiar Configuration", similarity, "Decreases as this design is integrated more times.  Partial credit for integrating designs with parts/stages in common."));
            return list;
        }
        public long GetIntegrationCost(SRCraftConfig config)
        {
            return (long)(config.ProductionRequired * (1+IntegrationEfficiencyFactors(config).Sum(fac => fac.Penalty)))+ config.Orders.Sum(or => or.Price)+ config.Fuel.Sum(v => v.Item3);
        }
        public (float, float) GetDevelopmentDelay(List<EfficiencyFactor> factors, ContractorAttitude attitude)
        {
            float x = UnityEngine.Random.value;
            float y = UnityEngine.Random.value;
            float delay = 1F + math.max(0.5f + factors.Sum(fac => fac.Penalty), 1f) * (float)(0.5 - 0.3 *attitude.Competence + 0.2 * attitude.Patience + 0.3 * attitude.Optimism)* math.min(x,y);
            float overrun = 1F + math.max(0.5f *factors.Sum(fac => fac.Penalty), 1f) * (float)(0.5 - 0.2 *attitude.Competence + 0.2 * attitude.Competitiveness + 0.3 * attitude.Optimism) * math.min(x,1f-y);
            return (delay, overrun);
        }
        public double RefurbishProgress(Hardware hardware, ContractorData contractor)
        {
            return 0.9- 0.0001 * math.pow(2.0, 12 - contractor.RefurbishedHardware.Occurences(hardware.Id));
        }
    }

    public enum StageEngineType
    {
        Liquid,
        Solid,
        None
    }
}