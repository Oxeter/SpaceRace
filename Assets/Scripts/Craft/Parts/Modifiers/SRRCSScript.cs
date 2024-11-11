namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using ModApi.Ui.Inspector;
    using ModApi.Math;
    using ModApi.Craft;

    public class SRRCSScript : PartModifierScript<SRRCSData>, IFlightFixedUpdate, IFlightFixedUpdateWarp, IAnalyzePerformance
    {
        public bool UsesMachNumber => false;
        private bool _ignited = false;
        private TextModel _burnTimeModel;
        private TextModel _nextIgnitionModel;
        private TextModel _operationalModel;
        protected override void OnInitialized()
        {
            base.OnInitialized();
            if (Game.InDesignerScene)
            {
                Data.UpdateInfo(SRManager.Instance.pm);
            }
        }
        public void Fail()
        {
            base.PartScript.Data.Activated = false;
            PartScript.Data.Config.SupportsActivation = false;
            PartScript.Data.ActivationGroup = 0;
        }
        public void FlightFixedUpdate(in FlightFrameData frame)
        {
            if (Data.Part.GetModifier<ReactionControlNozzleData>().Script.RcnThrottle >0f)
            {
                Data.BurnTime += frame.DeltaTime;
                if (!_ignited)
                {
                    _ignited=true;
                    if (UnityEngine.Random.value  < Data.NextFailureRate * Data.FailureFactor)
                    {
                        Game.Instance.FlightScene.FlightSceneUI.ShowMessage(PartScript.Data.Name + " failed on ignition.");
                        Fail();
                        return;
                    }
                    Data.NextFailureRate *= 0.5f;
                }
                _burnTimeModel?.Update();
            }
            else
            {
                _ignited=false;
                Data.NextFailureRate = Data.BaseFailureRate /2 - (Data.BaseFailureRate /2 - Data.NextFailureRate) * Mathf.Pow(0.9993f, frame.DeltaTime);
            }
            _nextIgnitionModel?.Update();
        }
        public void FlightFixedUpdateWarp(in FlightFrameData frame)
        {
            Data.NextFailureRate = Data.BaseFailureRate /2 - (Data.BaseFailureRate /2 - Data.NextFailureRate) * Mathf.Pow(0.9993f, (float)frame.DeltaTimeWorld); 
            _nextIgnitionModel?.Update();
        }
        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            GroupModel groupModel = new GroupModel("Reliability");
            model.AddGroup(groupModel);
            CreateInspectorModel(groupModel, false);
        }
        public void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
        {
            CreateInspectorModel(groupModel, true);
        }
        private void CreateInspectorModel(GroupModel model, bool flight)
        {
            if (!flight) 
            {
                _operationalModel = model.Add(new TextModel("Status", () => PartScript.Data.Config.SupportsActivation? "Nominal" : "Failed"));
                _burnTimeModel = model.Add(new TextModel("Burn Time", () => Units.GetRelativeTimeString(Data.BurnTime), null, "The time that the engine has burned."));
                _nextIgnitionModel = model.Add(new TextModel("Ignition Failure Chance", () => Units.GetPercentageString(Data.NextFailureRate), null, "Failure chance at next ignition."));
            }
            else
            {
                _nextIgnitionModel = model.Add(new TextModel("Ignition Failure Rate", () => Units.GetPercentageString(Data.BaseFailureRate), null, "Chance to fail during a series of maneuvers."));
            }
        }
    }
}