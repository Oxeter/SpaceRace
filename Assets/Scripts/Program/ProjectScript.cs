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
    using ModApi;
    using ModApi.Scenes.Events;
    using ModApi.Flight.Events;
    //using System.Runtime.InteropServices;
    using ModApi.State;
    using System.Xml.Linq;
    using ModApi.Flight;
    using Assets.Scripts.State;


    public abstract class ProjectScript<T> : IProjectScript<T> where T : ProjectData 
    {
        public abstract ProjectCategory Category {get;}
        protected T _data;
        public T Data => _data;
        public long PricePerDay => Completed? 0 : _data.PricePerDay;
        public bool Active => _data.Active;
        public bool Completed => _data.Completed;
        public virtual double TimeToCompletion => _data.TimeToCompletion;
        protected IProgramManager _pm;
        protected IHardwareManager _hm;
        public int Id => _data.Id;
        public string Name => _data.Name;
        public int ContractorId => _data.ContractorId;
        public virtual string PricePerDayString()
        {
            return Units.GetMoneyString(PricePerDay);
        }
        public virtual string PricePerDayTooltip()
        {
            return Units.GetRelativeTimeString(_data.TimeToCompletion) + " Remaining";
        }
        public bool PricePerDayVisible()
        {
            return PricePerDay > 0;
        }
        
        protected virtual void SetRush(bool rush)
        {
            _data.Rush = rush;
            UpdateRates(false);
        }

        private float _lastCancelClickTime = 0f;
        public virtual GroupModel GetGroupModel()
        {
            GroupModel _gModel = new GroupModel(_data.Name, Id.ToString());
            _gModel.Add(new TextModel("Contractor",() => _pm.Contractor(_data.ContractorId).Data.Name));
            _gModel.Add(new ProgressBarModel(
                () => Units.GetRelativeTimeString(_data.TimeToCompletion), 
                () => (float)(_data.CurrentProgress / _data.TotalProgress)));
            _gModel.Add(new ToggleModel("Rush", () => _data.Rush, SetRush));
            _gModel.Add(new TextModel("Price/Day", PricePerDayString));
            _gModel.Add(new SpacerModel());
            if (_data.EfficiencyFactors.Count >0)
            {
                _gModel.Add(new LabelModel("Efficiency Factors", ElementAlignment.Center));
                foreach (EfficiencyFactor fac in _data.EfficiencyFactors)
                {
                    _gModel.Add(new TextModel(fac.Text, fac.PenaltyString, null, fac.ToolTip));
                }
                _gModel.Add(new SpacerModel());
            }
            _gModel.Add(new TextButtonModel(
                "Cancel", 
                OnCancelClicked));
            _gModel.Add(new TextButtonModel(
                "Complete", 
                (b) => Complete(),
                null,
                ()=> _pm.Career.TechTree.GetItemValue("Cheats.SpaceRaceCheats")?.ValueAsBool ?? false
                ));    
            return _gModel;
        }

        public virtual void Initialize()
        {
        }
        public virtual void UpdateProgress(double dT)
        {
            _data.CurrentProgress += _data.ProgressPerDay * dT * Formulas.SRFormulas.DaysPerSecond;
            if (_data.CurrentProgress >= _data.TotalProgress) 
            {
                Complete();
            }
        }
        
        public virtual void UpdateRates(bool efficiencies)
        {
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
                _pm.SetNextCompletion(TimeToCompletion, this);
            }
        }

        protected virtual void AddEfficiencyFactors()
        {

        }
        protected virtual void Complete()
        {
            _data.Rush = false;
            _pm.SetWarp(false);
            _pm.Career.SpendMoney((long)(PricePerDay * (_data.TotalProgress- _data.CurrentProgress)/_data.ProgressPerDay)); //refund!
            _data.CurrentProgress = _data.TotalProgress;
            _data.Completed = true;
            foreach (Technology tech in _data.Technologies) _pm.DevelopTech(tech, _data.ContractorId, Category);
            _pm.UpdateAllRates(true);
        }

        protected void OnCancelClicked(ItemModel b)
        {
            if (Time.time < _lastCancelClickTime +3.0f)
            {
                Cancel();
            }
            else
            {
                _lastCancelClickTime = Time.time;
                if (Game.InFlightScene)
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Click again to cancel the project", false, 3.0f);
                if (Game.InDesignerScene)
                Game.Instance.Designer.DesignerUi.ShowMessage("Click again to cancel the project");
            }
        }

        public virtual void Cancel()
        {
            _pm.EndProject(_data.Id);
        }
    }


    public class PartDevelopmentScript : ProjectScript<PartDevelopmentData>, IHardwareDevelopmentScript
    {
        public override ProjectCategory Category {get {return ProjectCategory.Part;}}
        public PartDevelopmentScript(PartDevelopmentData data, IProgramManager pm)
        {
            _data = data;
            _pm = pm;
            _hm = pm.HM;
        }
        protected override void Complete()
        {
            if (Game.InFlightScene) Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Name + " Development Complete");
            _pm.ActivateContractor(_data.ContractorId);
            if (_pm.Contractor(_data.ContractorId).Data.BaseProductionRate < _data.ProductionCapacity)
            {
                _pm.Contractor(_data.ContractorId).SetBaseProductionRate(_data.ProductionCapacity);
            }
            _hm.DevelopId(_data.PartId);
            if (_data.PartFamilyId > 0) _hm.DevelopId(_data.PartFamilyId);
            base.Complete();
            _pm.EndProject(_data.Id);
        }

        public override void UpdateProgress(double dT)
        {
            _data.CurrentProgress += _data.ProgressPerDay * dT * Formulas.SRFormulas.DaysPerSecond;
            //_pm.Career.SpendMoney((long)(PricePerDay * dT * SRFormulas.DaysPerSecond));
            if (!_data.Delayed && 2 * _data.CurrentProgress >= _data.TotalProgress) 
            {
                Delay();
            }
            if (_data.CurrentProgress >= _data.TotalProgress) 
            {
                Complete();
            }
        }
        protected override void AddEfficiencyFactors()
        {
            base.AddEfficiencyFactors();
            foreach (Technology tech in _data.Technologies)
            {
                _data.EfficiencyFactors.Add(_pm.GetEfficiencyFactor(tech, _data.ContractorId, ProjectCategory.Part));
            }
        }
        public override void UpdateRates(bool efficiencies)
        {   
            if (!_data.Delayed && !_data.Completed)
            {
                if (2.0 * _data.CurrentProgress >= _data.TotalProgress && _data.TotalProgress > 0.0) Delay();
            }
            base.UpdateRates(efficiencies);
        }
        private void Delay()
        {
            _data.Delayed = true;
            (float, float) delay = _pm.CostCalculator.GetDevelopmentDelay(_data.EfficiencyFactors, _pm.Contractor(_data.ContractorId).Data.Attitude);
            _data.TotalProgress *= delay.Item1;
            _hm.MultiplyUnitCost(_data.PartId, delay.Item2);
            _pm.UpdateAllRates(false);
            if (Game.InFlightScene)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format("{0} development will take {1} longer. The parts will also be {2} more expensive than anticipated.", Name, Units.GetPercentageString(delay.Item1-1F), Units.GetPercentageString(delay.Item2-1F)));
            }
        }
        public override void Cancel()
        {
            _hm.DeletePart(_data.PartId);
            base.Cancel();
        }
    }

    public class StageDevelopmentScript : ProjectScript<StageDevelopmentData>, IStageDevelopmentScript
    {
        public override ProjectCategory Category {get {return ProjectCategory.Stage;}}
        public StageDevelopmentScript(StageDevelopmentData data, IProgramManager pm)
        {
            _data = data;
            _pm = pm;
            _hm = pm.HM;
        }
        protected override void Complete()
        {
            if (Game.InFlightScene) Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Name + " Development Complete");
            _pm.ActivateContractor(_data.ContractorId);
            if (_pm.Contractor(_data.ContractorId).Data.BaseProductionRate < _data.ProductionCapacity)
            {
                _pm.Contractor(_data.ContractorId).SetBaseProductionRate(_data.ProductionCapacity);
            }
            foreach (int i in _data.StageIds) _hm.DevelopId(i);
            foreach (Technology tech in _data.Technologies) _pm.DevelopTech(tech, _data.ContractorId, ProjectCategory.Stage);
            base.Complete();
            _pm.EndProject(_data.Id);
        }
        public override void UpdateProgress(double dT)
        {
            _data.CurrentProgress += _data.ProgressPerDay * dT * Formulas.SRFormulas.DaysPerSecond;
            //_pm.Career.SpendMoney((long)(PricePerDay * dT * SRFormulas.DaysPerSecond));
            if (!_data.Delayed && 2 * _data.CurrentProgress >= _data.TotalProgress) 
            {
                Delay();
            }
            if (_data.CurrentProgress >= _data.TotalProgress) 
            {
                Complete();
            }
        }
        protected override void AddEfficiencyFactors()
        {
            base.AddEfficiencyFactors();
            foreach (Technology tech in _data.Technologies)
            {
                _data.EfficiencyFactors.Add(_pm.GetEfficiencyFactor(tech, _data.ContractorId, ProjectCategory.Stage));
            }

        }
        public override void UpdateRates(bool efficiencies)
        {   
            if (!_data.Delayed && !_data.Completed)
            {
                if (2.0 * _data.CurrentProgress >= _data.TotalProgress) Delay();
            }
            base.UpdateRates(efficiencies);
        }
        private void Delay()
        {
            _data.Delayed = true;
            (float, float) delay = _pm.CostCalculator.GetDevelopmentDelay(_data.EfficiencyFactors, _pm.Contractor(_data.ContractorId).Data.Attitude);
            _data.TotalProgress *= delay.Item1;
            foreach (int i in _data.StageIds) _hm.MultiplyUnitCost(i, delay.Item2);
            _pm.UpdateAllRates(false);
            if (Game.InFlightScene)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage(string.Format("{0} development will take {1} longer. The parts will also be {2} more expensive than anticipated.", Name, Units.GetPercentageString(delay.Item1-1F), Units.GetPercentageString(delay.Item2-1F)));
            }
        }
        public override void Cancel()
        {
            foreach (int i in _data.StageIds)
            {
                _hm.DeleteStage(i);
            }
            base.Cancel();
        }
    }
}