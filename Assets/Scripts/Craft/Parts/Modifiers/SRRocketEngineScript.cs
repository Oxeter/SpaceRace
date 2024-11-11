namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using Assets.Scripts.Design.Staging;
    using ModApi.Math;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;
    using ModApi.Scripts.State.Validation;
    using ModApi.Ui.Inspector;
    using UnityEngine;
    using Assets.Scripts.SpaceRace.Formulas;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.Projects;

    public class SRRocketEngineScript : PartModifierScript<SRRocketEngineData> , IFlightFixedUpdate, IAnalyzePerformance
    {
        private RocketEngineMath.Params _srparams = new RocketEngineMath.Params();
        private float _igniteFailure = 0f;        
        private float _fullDurationFailure = 0f;
        private double _ratedBurnTime = double.MaxValue;  
        private float _shutdownsafety = 1F;
        private float _cyclefactor = 1F ;
        private float _nozzlefactor = 0F;
        private float _familyfactor = 0F;
        private float _familyignitefactor = 0F;
        private double _baseDuration = 60.0;
        private float _baseFail = 0F;
        private double _cpfactor = 0.0;
        private TextModel _burnTimeModel;
        private TextModel _igniteFailureModel;
        private TextModel _fullDurationFailureModel;
        private TextModel _ratedBurnTimeModel;
        public bool UsesMachNumber => false;
        public float IgniteFailure => _igniteFailure;    
        public float FullDurationFailure => _fullDurationFailure;
        public double RatedBurnTime => _ratedBurnTime;   
        public float ShutDownSafety => _shutdownsafety;
        public bool HardwareFailures => (Data as ISRPartMod).HardwareFailures;
        public override void OnActivated()
        {
            base.OnActivated();
            if (!Data.AirLight && !PartScript.CraftScript.CraftNode.InContactWithPlanet)
            {
                PartScript.Deactivate();
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Engines without airlight capability must light on the ground.");
            }
        }
        public void SetReliability(IProgramManager pm)
        {   
            _shutdownsafety = pm.Career?.TechTree.GetItemValue("RocketEngine.ShutDownSafety")?.ValueAsFloat ?? 2;
            RocketEngineData red = Data.Part.GetModifier<RocketEngineData>();
            switch (red.NozzleType.Id)
            {
                case "Plug": _baseDuration = 10.0;
                break;
                case "Film": _baseDuration = 60.0;
                break;
                case "Cone": _baseDuration = 150.0;
                break;
                case "Bell": _baseDuration = 480.0;
                break;
                case "Bravo": _baseDuration = 150.0;
                break;
                case "Delta": _baseDuration = 480.0;
                break;
                case "Alpha": _baseDuration = 480.0;
                break;
                case "Omega": _baseDuration = 240.0;
                break;
                case "Echo": _baseDuration = 150.0;
                break;               
                default: _baseDuration = 150.0;
                break;
            }
            float cp = red.UserChamberPressure;
            double _cpfactor = 2.0 - Math.Pow(2.0, 4.0 * (cp-Data.MaxPressure));
            _ratedBurnTime = Math.Max(_baseDuration * _cpfactor, 1.0);
            int finao = 0;
            switch (red.EngineType.Id)
            {
                case "Model":  _baseFail = 0.005f;
                break;
                case "Booster": _baseFail = 0.005f;
                break;
                case "PressureFed": _baseFail = 0.01f;
                break;
                case "Monoprop": _baseFail = 0.005f;
                break;
                case "Redundant": _baseFail = 0.002f;
                finao = 30;
                break;              
                default: _baseFail = 0.03f;
                break;
            }
            TechOccurenceList integ = pm.Data.TechIntegrated;
            _cyclefactor =  (float)(10 * Math.Pow(0.9f, Data.CycleIntegreations+finao));
            _nozzlefactor = (float)(0.1 * Math.Pow(0.9f, Data.NozzleIntegreations));
            _familyfactor = (float)(0.1 * Math.Pow(0.9f, Data.FamilyIntegreations+finao));
            _fullDurationFailure = _baseFail * (1 + _cyclefactor) + _nozzlefactor + _familyfactor;
            _familyignitefactor = _familyfactor;
            if (Game.IsCareer && red.EngineTypeId == "Liquid")
            {
                if (Data.AirLight)
                {
                    float safety = pm.Career.TechTree.GetItemValue("SpaceRace.AirLightReliability")?.ValueAsFloat ?? 1f;
                    _familyignitefactor *= 3.0f / safety;
                }
                else 
                {
                    float safety = pm.Career.TechTree.GetItemValue("SpaceRace.GroundLightReliability")?.ValueAsFloat ?? 1f;
                    _familyignitefactor /= safety;
                }
            }
            _igniteFailure = _baseFail * (1 + _cyclefactor) + _familyignitefactor;
            _igniteFailureModel?.Update();
            _fullDurationFailureModel?.Update();
            _ratedBurnTimeModel?.Update();
        }
        /// <summary>
        /// Computes the chance of failure during a frame of length dT.
        /// </summary>
        /// <param name="dT">Frame time in seconds</param>
        /// <returns>Chance of failure</returns>
        private float FailureChance (double dT)
        {
            return (float)(Data.BurnTime < RatedBurnTime ?
                dT * FullDurationFailure / RatedBurnTime / (1f - FullDurationFailure * Data.BurnTime / RatedBurnTime):
                dT / RatedBurnTime / 4.0f);
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {   
            if (Game.Instance.GameState.Type != ModApi.State.GameStateType.Default) return;
            if (PartScript.Data.GetModifier<RocketEngineData>().Ignited)
            {
                Data.BurnTime += frame.DeltaTime;
                if (_burnTimeModel != null) _burnTimeModel.Value = Units.GetRelativeTimeString(Data.BurnTime);
                if (PartScript.Data.GetModifier<RocketEngineData>().IgnitionsUsed > Data.IgnitionsChecked)
                {
                    Data.IgnitionsChecked = PartScript.Data.GetModifier<RocketEngineData>().IgnitionsUsed;
                    float roll = UnityEngine.Random.value;
                    if ( roll < IgniteFailure * Data.FailureFactor)
                    {
                        Debug.Log($"Failed roll: {roll} < {IgniteFailure * Data.FailureFactor}");
                        Game.Instance.FlightScene.FlightSceneUI.ShowMessage(PartScript.Data.Name + " failed on ignition.");
                        base.PartScript.Data.Activated = false;
                        PartScript.Data.Config.SupportsActivation = false;
                    }
                }
                if (Data.Failing)
                {
                    PartScript.TakeDamage(Formulas.SRFormulas.FailingDamage(UnityEngine.Random.value), PartDamageType.Pressure);
                    Debug.Log("Chamber Pressure is "+ PartScript.Data.GetModifier<RocketEngineData>().ChamberPressure);
                }
                else
                {
                    if (UnityEngine.Random.value < FailureChance(frame.DeltaTime) * Data.FailureFactor) Data.SetFailing();
                }
            }
            else if (Data.Failing)
            {
                if (UnityEngine.Random.value < 0.02f * _shutdownsafety * frame.DeltaTime)
                {
                    Data.Safe();
                    return;
                }
                PartScript.TakeDamage(0.5f* Formulas.SRFormulas.FailingDamage(UnityEngine.Random.value), PartDamageType.Pressure);     
            }
        }
        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            GroupModel groupModel = new GroupModel("Reliability");
            model.AddGroup(groupModel);
            CreateInspectorModel(groupModel, true);
        }
        public void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
        {
            CreateInspectorModel(groupModel, false);
        }
        private void CreateInspectorModel(GroupModel model, bool flight)
        {
            if (flight) _burnTimeModel = model.Add(new TextModel("Burn Time", () => Units.GetRelativeTimeString(Data.BurnTime), null, "The time that the engine has burned."));
            _ratedBurnTimeModel = model.Add(new TextModel("Rated Burn Time", () => Units.GetRelativeTimeString(RatedBurnTime), null, $"Cycle: {Units.GetRelativeTimeString(_baseDuration)} x Chamber Pressure: {Units.GetPercentageString((float)_cpfactor)}"));
            _fullDurationFailureModel = model.Add(new TextModel("Full Duration Failure", () => ModApi.Utilities.FormatPercentage(FullDurationFailure), null, $"Cycle: {Units.GetPercentageString(_baseFail * (1 + _cyclefactor))} + Nozzle: {Units.GetPercentageString(_nozzlefactor)} + Part Family: {Units.GetPercentageString(_familyfactor)}"));
            _igniteFailureModel = model.Add(new TextModel("Ignition Failure", () => ModApi.Utilities.FormatPercentage(IgniteFailure), null, $"Cycle: {Units.GetPercentageString(_baseFail * (1 + _cyclefactor))} + Part Family: {Units.GetPercentageString(_familyignitefactor)}"));
        }

        public override void ValidatePart(ValidationResult result)
        {
            base.ValidatePart(result);
            if (!Data.AirLight && Data.MaxIgnitions > 1)
            {
                result.AddMessage(
                    ValidationID.CraftConfigurationType, 
                    "Engine will not need multiple ignitions if it cannot airlight.",
                    PartScript.Data,
                    ValidationMessageType.Error
                );
            }
            if (!Data.AirLight && PartScript.Data.ActivationStage != 0)
            {
                result.AddMessage(
                    ValidationID.CraftConfigurationType, 
                    "Engine that cannot airlight is not in the first stage",
                    PartScript.Data,
                    ValidationMessageType.Warning
                );
            }
        }
    }
}