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
    using ModApi.Math;
    using Rewired.Demos;
    using System.Drawing.Text;
    using Assets.Scripts.State;
    using UnityEngine.UI;
    using Assets.Scripts.Design.Tutorial.Career;
    using Unity.Mathematics;
    using Assets.Scripts.Flight;

    public class ContractorScript : IContractorScript, IProgramChild
    {
        public ProjectCategory Category {get {return ProjectCategory.Contractor;}}
        public int Id => _data.Id;
        public string Name => _data.Name;
        public bool Active => _data.Active;
        public bool Completed => false;
        public long PricePerDay => Orders.Count>0? (long)(Orders[0].Markup * _data.BaseProductionRate * Formulas.SRFormulas.RushPrice(Orders[0].Rush)): 0;
        private TextModel priceModel;
        private TextModel productionModel;
        private TextModel avgIntentoryModel;
        private TextModel avgRushModel;
        private ProgressBarModel progressBarModel;
        protected ContractorData _data;
        protected IProgramManager _pm;
        public ContractorData Data => _data;       
        public int ActiveDevelopments {get; private set;} = 0;
        public int ContractorId => _data.Id;
        public List<HardwareOrder> Orders = new List<HardwareOrder>();
        public double TimeToCompletion => (Orders.Count == 0)? double.PositiveInfinity : Orders[0].TimeToCompletion;
        public ContractorScript(ContractorData data, IProgramManager pm)
        {
            _data = data;
            _pm = pm;
        }
        public void Initialize()
        {
        }
        public GroupModel GetGroupModel()
        {
            GroupModel _gModel = new GroupModel(_data.Name, Id.ToString());
            _gModel.Add(new TextModel("Developments", ()=> ActiveDevelopments.ToString()));
            _gModel.Add(new TextModel("Orders", ()=> Orders.Count.ToString()));
            productionModel = _gModel.Add(new TextModel("Production/Day", () => Units.GetMoneyString((long)(_data.BaseProductionRate * Formulas.SRFormulas.RushRate(Orders.Count > 0 ? Orders[0].Rush : Data.Rush || Data.Rush))), null, "The value of hardware being produced"));
            priceModel = _gModel.Add(new TextModel("Price/Day", PricePerDayString, null, "What you are paying this contractor"));
            _gModel.Add(new ToggleModel("Rush Orders", ()=>Data.Rush, (b) => Data.Rush = b, "Rush all orders."));
            progressBarModel = _gModel.Add(new ProgressBarModel("Inventory: " + Units.GetPriceString((long)_data.Inventory), () => (float)(_data.Inventory / _data.InventoryLimit) ));
            foreach (HardwareOrder order in Orders)
            {
                order.Model = _gModel.Add(new TextModel(order.Rush? order.ProjectName+" (Rush)": order.ProjectName, () => Units.GetRelativeTimeString(order.TimeToCompletion), null, order.Description));
            }
            avgRushModel = _gModel.Add(new TextModel("Avg Rush", () => Units.GetPercentageString((float)_data.AverageRecentRush)));
            avgIntentoryModel = _gModel.Add(new TextModel("Avg Inventory", () => Units.GetPercentageString((float)(_data.AverageRecentInventory/_data.InventoryLimit))));
            return _gModel;
        }
        public bool DevelopTech(string mod, string tech)
        {
            _data.Active = true;
            if (_data.DevelopedTechs.Contains(mod,tech)) return false;
            _data.DevelopedTechs.AddTech(mod, tech);
            return true;
        }
        public bool DevelopTech(Technology tech)
        {
            return DevelopTech(tech.Mod, tech.Tech);
        }
        public void SetBaseProductionRate(double rate)
        {
            _data.BaseProductionRate = rate;
        }

        public void SetRush(bool rush)
        {
            Data.Rush = rush;
            UpdateRates(false);
            //_pm.ReplaceInspectorGroup(Id);
        }

        public void PlaceOrder(HardwareOrder order)
        {
            Orders.Add(order);
            if (!order.RefurbishedHardwareCollected)
            {
                order.RefurbishedHardwareCollected= true;
                foreach (HardwareOccurencePair hop in order.HardwareRequired)
                {
                    Hardware hw = _pm.HM.Hardware[hop.Id];
                    int num = math.min(hop.Occurences, _data.RecoveredHardware.Occurences(hop.Id) - _data.RefurbishedHardware.Occurences(hop.Id));
                    order.CurrentProduction += num * _pm.CostCalculator.RefurbishProgress(hw, _data) * hw.ProductionRequired;
                    _data.RefurbishedHardware.AddPair(hop.Id, num);
                }
            }
            //Debug.Log(Name +": " +order.Description);
            UpdateRates(false);
            _pm.ReplaceInspectorGroup(Id);
            _pm.ReplaceInspectorGroup(0);
        }
        public void CancelOrder(int i)
        {
            Orders.Remove(Orders.Find(or => or.ProjectId==i));
        }
        public string PricePerDayString()
        {
            return Orders.Count > 0? Units.GetMoneyString((long)(Orders[0].Markup * _data.BaseProductionRate * Formulas.SRFormulas.RushPrice(Data.Rush))): Units.GetMoneyString(0);
        }
        public string PricePerDayTooltip()
        { 
            return Orders.Count > 0? Orders[0].ProjectName: string.Empty;
        }
        public bool PricePerDayVisible()
        {
            return Orders.Count > 0 && _data.BaseProductionRate > 0;
        }
        public bool IsDeveloped(string mod, string tech)
        {
            return _data.DevelopedTechs.Contains(mod, tech);
        }
        public bool IsDeveloped(TechOccurenceTriple triple)
        {
            return IsDeveloped(triple.Mod, triple.Tech);
        }
        public virtual void UpdateProgress(double dT)
        {
            _data.AverageRecentInventory *= (Formulas.SRFormulas.SecondsPerYear - dT) * Formulas.SRFormulas.YearsPerSecond;
            _data.AverageRecentRush *= 1 - dT * Formulas.SRFormulas.YearsPerSecond;
            if (Orders.Count == 0)
            {
                if (_data.Active)
                {
                    _data.Inventory = _data.InventoryLimit - Math.Max(0.0, (_data.InventoryLimit - _data.Inventory) * Math.Exp(-dT * Formulas.SRFormulas.YearsPerSecond));
                    _data.AverageRecentInventory += dT * Formulas.SRFormulas.YearsPerSecond * _data.Inventory / _data.InventoryLimit;
                }
            }
            else 
            {
                if (Data.Rush || Orders[0].Rush) _data.AverageRecentRush += dT * Formulas.SRFormulas.YearsPerSecond;
                Orders[0].CurrentProduction += dT * Formulas.SRFormulas.DaysPerSecond * _data.BaseProductionRate * Formulas.SRFormulas.RushRate(Orders[0].Rush || Data.Rush);
                //_pm.Career.SpendMoney((long)(Orders[0].Markup * dT * SRFormulas.DaysPerSecond * _data.BaseProductionRate * SRFormulas.RushPrice(_rush)));
                //Debug.Log(Orders[0].ToString());
                if(Orders[0].CurrentProduction >= Orders[0].TotalProduction)
                {
                     _pm.Career.SpendMoney((long)((Orders[0].TotalProduction - Orders[0].CurrentProduction) * Orders[0].Markup));
                    _data.Inventory += Orders[0].CurrentProduction - Orders[0].TotalProduction;
                    Orders[0].CurrentProduction = Orders[0].TotalProduction;
                    Orders[0].Model = null;
                    Orders.RemoveAt(0);
                    _pm.UpdateAllRates(false);
                    _pm.ReplaceInspectorGroup(Id);
                }
                foreach (HardwareOrder order in Orders)
                {
                    order.TimeToCompletion -= dT;
                    order.UpdateTextModel();
                }
            }
            UpdateProgressBar();
            if (avgRushModel !=null ) avgRushModel.Value = Units.GetPercentageString((float)_data.AverageRecentRush);
            if (avgIntentoryModel !=null ) avgIntentoryModel.Value = Units.GetPercentageString((float)_data.AverageRecentInventory);
        }
        public virtual void UpdateRates(bool efficiencies)
        {
            ActiveDevelopments = _pm.ActiveChildren.Values.Count(proj => proj.ContractorId == Id) -1;
            Orders.RemoveAll(or => or.TotalProduction <= or.CurrentProduction);
            if (Orders.Count > 0)
            {
                Orders.Sort((a,b)=> a.Priority - b.Priority);
                //Debug.Log(Name +" has orders:" + Orders.Count.ToString());
                bool rushrequest = Data.Rush;
                double inventory = Math.Min(Orders[0].TotalProduction - Orders[0].CurrentProduction, _data.Inventory);
                Orders[0].CurrentProduction += inventory;
                //Debug.Log($"{Orders[0].ContractorName} spending {Units.GetMoneyString((long)(inventory * Orders[0].Markup))} on inventory");
                _pm.Career.SpendMoney((long)(inventory * Orders[0].Markup));
                _data.Inventory -= inventory;
                for (int i = Orders.Count - 1; i >= 0; i--)
                {
                    if (Orders[i].RushRequested) rushrequest = true;
                    Orders[i].Rush = rushrequest;
                    Orders[i].TimeToCompletion = (Orders[i].TotalProduction - Orders[i].CurrentProduction) / _data.BaseProductionRate / Formulas.SRFormulas.RushRate(Orders[i].Rush) * Formulas.SRFormulas.SecondsPerDay;
                }
                for (int i =1; i < Orders.Count; i++)
                {
                    Orders[i].TimeToCompletion += Orders[i-1].TimeToCompletion;
                    Orders[i].UpdateTextModel();
                }
                //_pm.SetNextCompletion(Orders[0].TimeToCompletion);
                _pm.Budget.UpdateRates(false);
                UpdateProgressBar();
                if (priceModel != null) priceModel.Value = Units.GetMoneyString(PricePerDay);
                if (productionModel != null) priceModel.Value = Units.GetMoneyString((long)(_data.BaseProductionRate * Formulas.SRFormulas.RushRate(Orders.Count > 0 ? Orders[0].Rush : Data.Rush || Data.Rush)));
            }
        }
        protected void UpdateProgressBar()
        {
            if (progressBarModel == null) return;
            if (Orders.Count == 0)
            {
                progressBarModel.Value = (float)(_data.Inventory / _data.InventoryLimit);
                progressBarModel.Label = $"Inventory: {Units.GetMoneyString((long)_data.Inventory)}";
            }
            else
            {
                progressBarModel.Value = (float)(Orders[0].CurrentProduction / Orders[0].TotalProduction);
                progressBarModel.Label = $"{Units.GetMoneyString((long)Orders[0].CurrentProduction)}/{Units.GetMoneyString((long)Orders[0].TotalProduction)}";
            }
        }


    }

    public class TemporaryContractorScript : ContractorScript
    {
        public TemporaryContractorScript(ContractorData data, IProgramManager pm) : base(data,pm){}

        public override void UpdateProgress(double dT)
        {
            if (Orders.Count >0) 
            {
                if(Orders[0].CurrentProduction >= Orders[0].TotalProduction)
                {
                    _data.Inventory = Orders[0].CurrentProduction - Orders[0].TotalProduction;
                    _pm.Career.SpendMoney((long)(-_data.Inventory * Orders[0].Markup));
                    Orders[0].CurrentProduction = Orders[0].TotalProduction;
                    Orders[0].Model = null;
                    Orders.RemoveAt(0);
                    _pm.UpdateAllRates(false);
                    _pm.ReplaceInspectorGroup(Id);
                }
                foreach (HardwareOrder order in Orders)
                {
                    order.TimeToCompletion -= dT;
                    order.UpdateTextModel();
                }
            }
            UpdateProgressBar();
        }
        public override void UpdateRates(bool efficiencies)
        {
            base.UpdateRates(efficiencies);
            if (_data.Inventory <= 0.0) _pm.EndProject(Id);
        }
    }
}
