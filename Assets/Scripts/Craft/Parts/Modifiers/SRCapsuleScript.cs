namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using ModApi.Math;
    using ModApi.Ui.Inspector;
    using ModApi.GameLoop;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using Assets.Scripts.Craft.Parts.Modifiers.Eva;
    using ModApi.Craft;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using UnityEngine;
    using System.Collections.Generic;
    using Assets.Scripts.Design;
    using ModApi;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;

    public class SRCapsuleScript : PartModifierScript<SRCapsuleData>, IFlightUpdate, IModifierWaterPhysicsConfig, IAnalyzePerformance
    {
        //
        // Summary:
        //     The attach point positions
        private Transform _attachPointPositions;

        //
        // Summary:
        //     The scalar transform for the command pod.
        private Transform _scalar;

        //
        // Summary:
        //     Gets the part volume in cubic meters.
        //
        // Value:
        //     The part volume in cubic meters.
        float IModifierWaterPhysicsConfig.PartVolume => base.Data.TotalVolume;
        public bool UsesMachNumber => false;

        public TextModel PowerConsumptionModel {get; private set;}
        //
        // Summary:
        //     Updates the scale and attach point positions.
        //
        // Parameters:
        //   newScale:
        //     The new scale.
        //
        //   repositionAttachedParts:
        //     if set to true [reposition attached parts].
        //
        //   heightStretch:
        //     The height multiplier.
        public void UpdateScale(float newScale, bool repositionAttachedParts = false, float heightStretch = 1f)
        {
            base.Data.UpdateOtherModifiersAndStuff();
            _scalar.localScale = new Vector3(newScale, newScale * heightStretch, newScale);
            Dictionary<int, bool> movedParts = new Dictionary<int, bool>();
            foreach (Transform attachPointPosition in _attachPointPositions)
            {
                foreach (AttachPoint attachPoint in base.Data.Part.AttachPoints)
                {
                    if (!(attachPoint.Name == attachPointPosition.name))
                    {
                        continue;
                    }

                    attachPoint.Scale = 1f * base.Data.ScaledSize;
                    Vector3 position = attachPoint.Position;
                    attachPoint.Position = attachPointPosition.localPosition * newScale * heightStretch;
                    attachPoint.Radius = attachPointPosition.localScale.y * newScale;
                    if (!(attachPoint.AttachPointScript != null))
                    {
                        break;
                    }

                    if (repositionAttachedParts)
                    {
                        Vector3 position2 = attachPoint.Position;
                        Vector3 delta = attachPoint.AttachPointScript.transform.parent.TransformVector(position2 - position);
                        foreach (PartConnection partConnection in attachPoint.PartConnections)
                        {
                            DesignerUtilities.RepositionParts(base.Data.Part, partConnection, delta, movedParts);
                            PartData otherPart = partConnection.GetOtherPart(base.Data.Part);
                            List<IConnectedAttachPointChangedHandler> modifiersWithInterface = otherPart.PartScript.GetModifiersWithInterface<IConnectedAttachPointChangedHandler>();
                            foreach (IConnectedAttachPointChangedHandler item in modifiersWithInterface)
                            {
                                foreach (PartConnection.Attachment attachment in partConnection.Attachments)
                                {
                                    item.OnAttachPointRadiusChanged(attachment.GetOtherAttachPoint(attachPoint), attachPoint);
                                }
                            }
                        }
                    }

                    attachPoint.AttachPointScript.transform.localPosition = attachPoint.Position;
                    break;
                }
            }
        }

        //
        // Summary:
        //     Called when the part modifier script is initialized.
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Data.UpdateCoM();
            _scalar = Utilities.FindFirstGameObjectMyselfOrChildren("PodParent", base.PartScript.GameObject).transform;
            _attachPointPositions = Utilities.FindFirstGameObjectMyselfOrChildren("AttachPointPositions", _scalar.gameObject).transform;
            UpdateScale(base.Data.ScaledSize, repositionAttachedParts: false, base.Data.Height);
            UpdatePowerConsumption();
        }
        public override void OnDeactivated()
        {
            base.OnDeactivated();
            Data.UpdateCoM();
            PartScript.BodyScript.RecalculateMass();
        }   
        public override void OnActivated()
        {
            base.OnActivated();
            Data.UpdateCoM();
            PartScript.BodyScript.RecalculateMass();
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            ICommandPod command = PartScript.CommandPod;
            if (command.BatteryFuelSource.TotalFuel >0)
            {
                command.BatteryFuelSource.RemoveFuel(Data.PowerConsumption * (float)frame.DeltaTimeWorld);
            }
            else
            {
                CrewCompartmentData modifier = Data.Part.GetModifier<CrewCompartmentData>();
                foreach (EvaScript script in modifier.Script.Crew)
                {
                    script.PartScript.TakeDamage(0.1F, PartDamageType.Pressure);
                }
            }
            
        }
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            UpdatePowerConsumption();
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            UpdatePowerConsumption();
        }

        public void UpdatePowerConsumption()
        {
            CrewCompartmentData modifier = PartScript.Data.GetModifier<CrewCompartmentData>();
            if (Game.InFlightScene)
            {
                Data.PowerConsumption = 0.25f + modifier.Script.Crew.Count * 0.25f;
            }
            else
            {
                Data.PowerConsumption = 0.25f + modifier.Capacity * 0.25f;
            }
            PowerConsumptionModel?.Update();
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            PowerConsumptionModel = model.Add(new TextModel("Capsule Power Usage", ()=> Units.GetPowerString(Data.PowerConsumption * 1000f)));
        }
        public void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
        {
            PowerConsumptionModel = groupModel.Add(new TextModel("Capsule Power Usage", ()=> Units.GetPowerString(Data.PowerConsumption * 1000f)));
        }
    }
}