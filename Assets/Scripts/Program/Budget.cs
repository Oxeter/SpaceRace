namespace Assets.Scripts.SpaceRace.Projects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Serialization;
    using Assets.Scripts.SpaceRace.Formulas;
    using Assets.Scripts.SpaceRace.Collections;
    using UnityEngine;
    using ModApi.Ui.Inspector;
    using ModApi.Math;
    using Assets.Scripts.Ui.Inspector;
    using Assets.Scripts.State;
    using System.Linq;
    using Unity.Mathematics;

    public class BudgetScript : IProgramChild
    {
        private IProgramManager _pm;
        public int Id {get {return 0;}}
        public int ContractorId {get {return 0;}}
        public string Name {get {return "Budget";}}
        public long PricePerDay => _upkeep + _technicianSalaries + _astronautSalaries - 20000;
        public ProjectCategory Category {get {return ProjectCategory.Budget;}}
        private long _upkeep = 0;
        private long _technicianSalaries ;
        private long _astronautSalaries ; 
        public TextModel TotalCashFlowModel;
        public bool Active => true;
        public bool Completed => false;
        public BudgetScript(IProgramManager pm)
        {
            _pm = pm;
        }
        private string TotalCashflowString()
        {
            if (_pm.CostPerDay > 0)
            return "-" + Units.GetMoneyString(_pm.CostPerDay);
            else return Units.GetMoneyString(-_pm.CostPerDay);
        }
        public GroupModel GetGroupModel()
        {
            GroupModel g = new GroupModel("Budget", "Budget");
            g.Add(new LabelModel("Funding", ElementAlignment.Center));
            g.Add(new TextModel("Base", () => Units.GetMoneyString(20000), null, "Fixed funding for the program"));
            foreach (IProgramChild item in _pm.ActiveChildren.Values.Where(ch => ch.Category == ProjectCategory.Funding))
            {
                g.Add(new TextModel(item.Name, item.PricePerDayString, null, item.PricePerDayTooltip()));
            }
            g.Add(new SpacerModel());
            g.Add(new LabelModel("Expenses", ElementAlignment.Center));
            g.Add(new TextModel("Technician salaries", () => Units.GetMoneyString(_technicianSalaries), null, $"Technicians cost {Units.GetMoneyString(SRFormulas.TechUpkeep)}per day per technician"));
            g.Add(new TextModel("Astronaut salaries", () => Units.GetMoneyString(_astronautSalaries), null, $"Astronauts cost {Units.GetMoneyString(SRFormulas.AstroUpkeep)} per day per astronaut", ()=> Game.Instance.GameState.Validator.ItemValue("Crew")>0F));
            g.Add(new TextModel("Program management", () => Units.GetMoneyString(_upkeep), null, "Upkeep is proportional to the square of the active contracts"));
            bool flag = true;
            foreach (IProgramChild item in _pm.ActiveChildren.Values.Where(ch => ch.Category == ProjectCategory.Contractor && ch.PricePerDay > 0)) 
            {
                if (flag)
                {
                    g.Add(new SpacerModel());
                    g.Add(new LabelModel("Contractors", ElementAlignment.Center));
                }
                g.Add(new TextModel(item.Name, item.PricePerDayString, null, item.PricePerDayTooltip(), item.PricePerDayVisible));
                flag = false;
            }
            flag = true;
            foreach (IProgramChild item in _pm.ActiveChildren.Values.Where(ch => ch.Category == ProjectCategory.Integration)) 
            {
                if (flag)
                {
                    g.Add(new SpacerModel());
                    g.Add(new LabelModel("Integrations", ElementAlignment.Center));
                }
                g.Add(new TextModel(item.Name, item.PricePerDayString, null, item.PricePerDayTooltip(), item.PricePerDayVisible));
                flag=false;
            }
            flag = true;
            foreach (IProgramChild item in _pm.ActiveChildren.Values.Where(ch => ch.Category == ProjectCategory.Stage || ch.Category == ProjectCategory.Part)) 
            {
                if (flag)
                {
                    g.Add(new SpacerModel());
                    g.Add(new LabelModel("Hardware Developments", ElementAlignment.Center));
                }
                g.Add(new TextModel(item.Name, item.PricePerDayString, null, item.PricePerDayTooltip(), item.PricePerDayVisible));
                flag=false;
            }
            flag = true;
            foreach (IProgramChild item in _pm.ActiveChildren.Values.Where(ch => ch.Category == ProjectCategory.Construction)) 
            {
                if (flag)
                {
                    g.Add(new SpacerModel());
                    g.Add(new LabelModel("Construction", ElementAlignment.Center));
                }
                g.Add(new TextModel(item.Name, item.PricePerDayString, null, item.PricePerDayTooltip(), item.PricePerDayVisible));
                flag=false;
            }
            g.Add(new SpacerModel());
            TotalCashFlowModel = g.Add(new TextModel("Total", TotalCashflowString));
            return g;
        }

        public void Initialize()
        {
        }
        public void UpdateProgress(double dT)
        {
        }

        public void UpdateRates(bool efficiencies)
        {
            _technicianSalaries = _pm.Data.TotalTechnicians * SRFormulas.TechUpkeep;
            _astronautSalaries = (long)Game.Instance.GameState.Crew.Members.Where(crew => crew.State != CrewMemberState.Deceased).Count() * SRFormulas.AstroUpkeep;
            _upkeep =SRFormulas.ContractUpkeep(_pm.Career.Contracts.Active.Count());
            if (TotalCashFlowModel != null) TotalCashFlowModel.Value = TotalCashflowString();
        }
        string IProgramChild.PricePerDayString()
        {
            return string.Empty;
        }
        string IProgramChild.PricePerDayTooltip()
        {
            return string.Empty;
        }
        bool IProgramChild.PricePerDayVisible()
        {
            return false;
        }

    }

    public class ContractFunding
    {
        [XmlIgnore]
        public bool Active = false;
        [XmlAttribute]
        public string Id;
        [XmlAttribute]
        public string Name;
        [XmlAttribute]
        public List<string> ContractIds = new List<string>();
        [XmlAttribute]
        public long PermanentFunding = 0;
        [XmlIgnore]
        public long TotalFunding = 0;
        [XmlAttribute]
        public long FundingPaid = 0;
        [XmlAttribute]
        public long FundingPerDay = 0;
        
        public string AmountString()
        {
            return Units.GetMoneyString(FundingPerDay);
        }
        public string Tooltip => Units.GetMoneyString(TotalFunding - FundingPaid)+" Remaining";

    }

}