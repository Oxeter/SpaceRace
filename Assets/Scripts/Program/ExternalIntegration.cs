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
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.Ui.Inspector;
    using Assets.Scripts.SpaceRace.Modifiers;
    using System.Linq;
    using ModApi.Flight.UI;
    using ModApi.Design;
    using ModApi.Flight;
    //using System.Runtime.InteropServices;
    using ModApi.State;
    using System.Xml.Linq;
    using Assets.Scripts.Flight;
    using Assets.Scripts.Craft;
    using ModApi.Craft;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using ModApi.Scenes.Parameters;
    using Assets.Scripts.Flight.Sim;
    using Assets.Scripts.Craft.Fuel;


    public class ExternalIntegrationScript : ProjectScript<ExternalIntegrationData>
    {
        public SRCraftConfig Config => _hm.GetCraftConfig(conf => conf.Id == _data.ConfigId);
        public IntegrationStatus Status => _data.Status;
        public override ProjectCategory Category {get {return ProjectCategory.ExternalIntegration;}}

        public HardwareOrder LongestOrder;
        public override double TimeToCompletion => LongestOrder?.TimeToCompletion ?? _data.TimeToCompletion;
        public override void Initialize()
        {
            base.Initialize();
            PlaceOrders();
            SetRush(Status == IntegrationStatus.Waiting); 
            UpdateRates(true);
        }
        private void AssignLongestOrder()
        {
            LongestOrder = null;
            foreach (HardwareOrder order in _data.Orders)
            {
                if (LongestOrder == null || LongestOrder.TimeToCompletion <= order.TimeToCompletion)
                {
                    LongestOrder = order;
                }
            }
        }
        protected override void SetRush(bool rush)
        {
            _data.Rush = rush;
            if (Status == IntegrationStatus.Waiting)
            {
                _data.Orders.RemoveAll(x => x.CurrentProduction >= x.TotalProduction);
                if (_data.Orders.Count == 0) 
                {
                    Complete();
                    return;
                }
                if (rush)
                {
                    HardwareOrder longestorder = _data.Orders.Aggregate((curMax, x) => (curMax == null || x.TimeToCompletion > curMax.TimeToCompletion ? x : curMax));
                    longestorder.RushRequested = true;
                    Debug.Log($"Longest order is {longestorder.ContractorName}");
                    _pm.Contractor(longestorder.ContractorId).UpdateRates(false);
                    foreach (HardwareOrder order in _data.Orders.Where(or => or.TimeToCompletion > longestorder.TimeToCompletion))
                    {
                        order.RushRequested = true;
                        _pm.Contractor(order.ContractorId).UpdateRates(false);
                    }
                }
                else
                {
                    foreach (HardwareOrder order in _data.Orders)
                    {
                        order.RushRequested = false;
                        _pm.Contractor(order.ContractorId).UpdateRates(false);
                    }
                    AssignLongestOrder();
                }
            }
        }
        public void PlaceOrders()
        {
            foreach (HardwareOrder order in Data.Orders.Where(or => or.TotalProduction > or.CurrentProduction))
            {
                order.Price = 0;
                order.Priority = Id;
                order.ProjectId = Id;
                order.ProjectName = Name;
                _pm.Contractor(order.ContractorId).PlaceOrder(order);
            }
            AssignLongestOrder();
        }       
        public ExternalIntegrationScript(ExternalIntegrationData data, IProgramManager pm)
        {
            _data = data;
            _pm = pm;
            _hm = pm.HM;
        }
        protected override void Complete()
        {
            switch (_data.Status)
            {
                case IntegrationStatus.Waiting:
                    LongestOrder = null;
                    _data.Status = IntegrationStatus.Stacking;
                    SetRush(true);
                    UpdateRates(true);
                    _pm.UpdateAllRates(false);
                    _pm.ReplaceInspectorGroup(Id);
                    return;
                case IntegrationStatus.Stacking:
                    _data.Delivered += 1;
                    if (_data.Delivered % SRFormulas.ExternalTestInterval == 0)
                    {
                        foreach (Technology tech in _data.Technologies) _pm.DevelopTech(tech, 0, ProjectCategory.Integration);
                    }
                    _pm.HM.IncrementCraftIntegrations(_data.ConfigId);
                    if (_data.Delivered >= _data.Ordered) 
                    {
                        Cancel();
                        return;
                    }
                    _data.CurrentProgress = 0;
                    _data.Status = IntegrationStatus.Waiting;
                    _data.Orders = new List<HardwareOrder>(Config.Orders.Select(c => c.NewFreeOrder()));
                    PlaceOrders();
                    SetRush(false);
                    _pm.UpdateAllRates(false);
                    _pm.ReplaceInspectorGroup(Id);
                    return;
            }
        }
        private bool Rushing()
        {
            return _data.Rush;
        }

        private void RushButton(bool flag)
        {

        }

        private string TimeToCompletionString()
        {   
            switch (TimeToCompletion)
            {
                case 0.0 : return "Complete";
                case double.NaN: return "Complete";
                case double.PositiveInfinity: return "No Progress";
                default: return Units.GetRelativeTimeString((float)TimeToCompletion);
            }
        }
        private float ProgressBarValue()
        {
            return LongestOrder == null ? (float)(_data.CurrentProgress / _data.TotalProgress) : (float)(LongestOrder.CurrentProduction /LongestOrder.TotalProduction);
        }
        public override GroupModel GetGroupModel()
        {
            GroupModel _gModel = new GroupModel(_data.Name, Id.ToString());
            _gModel.Add(new TextModel("Ordered",_data.Ordered.ToString));
            _gModel.Add(new TextModel("Delivered",_data.Delivered.ToString));
            _gModel.Add(new TextModel("Status",Status.ToString));
            _gModel.Add(new ProgressBarModel(
                TimeToCompletionString, 
                ProgressBarValue));
            _gModel.Add(new ToggleModel("Rush", Rushing, RushButton));
            foreach (HardwareOrder order in _data.Orders)
            {
                _gModel.Add(new TextModel(order.ContractorName,order.TTCString, null, order.Description, order.IsOpen));
            }
            if (_data.EfficiencyFactors.Count >0)
            {
                _gModel.Add(new LabelModel("Efficiency Factors", ElementAlignment.Center));
                foreach (EfficiencyFactor fac in _data.EfficiencyFactors)
                {
                    _gModel.Add(new TextModel(fac.Text,fac.PenaltyString, null, fac.ToolTip));
                }
                _gModel.Add(new SpacerModel());
            }
            return _gModel;
        }
        protected override void AddEfficiencyFactors()
        {
            base.AddEfficiencyFactors();
            if (_data.Status == IntegrationStatus.Stacking)
            _data.EfficiencyFactors = _pm.CostCalculator.IntegrationEfficiencyFactors(Config);
        }

        public override void Cancel()
        {
            foreach (HardwareOrder order in _data.Orders)
            {
                _pm.Contractor(order.ContractorId).CancelOrder(Id);
            }
            base.Cancel();
        }



        public override void UpdateRates(bool efficiencies)
        {
            switch (_data.Status)
            {
                case IntegrationStatus.Waiting: 
                    if (_data.Orders.Any(or => or.CurrentProduction >= or.TotalProduction))
                    {
                        _data.Orders.RemoveAll(x => x.CurrentProduction >= x.TotalProduction);
                        _pm.ReplaceInspectorGroup(Id);
                    }
                    if (_data.Orders.Count == 0)
                    {
                        Complete();
                    }
                    return;
                default:
                    if (!_data.Completed)
                    {
                        if (efficiencies)
                        {
                            _data.EfficiencyFactors.Clear();
                            AddEfficiencyFactors();
                            _data.EfficiencyFactors = _data.EfficiencyFactors.Where(y => y != null).ToList();
                            _data.Efficiency = 1.0 / (1.0 + _data.EfficiencyFactors.Sum(e => e.Penalty));
                            if (_data.Efficiency < 0) _data.Efficiency = 1000.0;
                        }
                    }
                    return;

            }
        }

    }
    public class ExternalIntegrationData : ProjectData
    {
        [XmlAttribute] 
        public IntegrationStatus Status = IntegrationStatus.Waiting;
        public override double ProgressPerDay => Status == IntegrationStatus.Waiting? 0 : BaseProgressPerDay * Efficiency * Formulas.SRFormulas.RushRate(Rush);
        public List<HardwareOrder> Orders = null;
        [XmlAttribute]
        public int ConfigId = 0;
        public override long PricePerDay => 0;
        [XmlAttribute]
        public int Delivered = 0;
        [XmlAttribute]
        public int Ordered;
    }


}