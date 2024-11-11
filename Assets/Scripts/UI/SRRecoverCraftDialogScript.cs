using System.Linq;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.Ui;
using ModApi.Flight;
using ModApi.Math;
using ModApi.State;
using ModApi.Ui;
using UI.Xml;
using UnityEngine;
using ModApi.Flight.UI;
using ModApi.Craft;
using Assets.Scripts.Flight.UI;
using Assets.Scripts.Flight;
using Assets.Scripts.SpaceRace.Projects;
using System;
using System.Security.Cryptography;


namespace Assets.Scripts.SpaceRace.UI
{
    public class SRRecoverCraftDialogScript : StatsDialogScript
    {
        //
        // Summary:
        //     Gets or sets the action to execute when the destroy craft button is clicked.
        //
        //
        // Value:
        //     The action to execute when the destroy craft button is clicked.
        public Action CraftDestroyed { get; set; }

        //
        // Summary:
        //     Gets or sets the action to execute when the recover parts or humans button is clicked.
        //
        //
        // Value:
        //     The action to execute when the recover parts or humans button is clicked.
        public Action CraftRecovered { get; set; }

        //
        // Summary:
        //     Gets or sets the action to execute when the recover craft button is clicked.
        //
        //
        // Value:
        //     The action to execute when the recover craft button is clicked.
        public Action CraftRecoveredWhole { get; set; }

        //
        // Summary:
        //     Gets or sets the action to execute when retry is clicked.
        //
        // Value:
        //     The action to execute when retry is clicked.
        public Action OnRetryClicked { get; set; }

        //
        // Summary:
        //     Creates the dialog.
        //
        // Parameters:
        //   recovery:
        //     The recovery.
        //
        //   showRetryButton:
        //     if set to true [show retry button].
        //
        // Returns:
        //     The newly created dialog.
        public static SRRecoverCraftDialogScript Create(SRCraftRecovery recovery, bool showRetryButton = false)
        {
            SRRecoverCraftDialogScript recoverCraftDialogScript = Game.Instance.UserInterface.CreateDialog("SpaceRace/StatsDialogBig", null, delegate (SRRecoverCraftDialogScript d, IXmlLayoutController c)
            {
                d.OnLayoutRebuilt((XmlLayout)c.XmlLayout);
            });
            recoverCraftDialogScript.ShowRecovery(recovery, showRetryButton);
            return recoverCraftDialogScript;
        }

        //
        // Summary:
        //     Shows the recovery details.
        //
        // Parameters:
        //   recovery:
        //     The recovery.
        //
        //   showRetryButton:
        //     if set to true [show retry button].
        private void ShowRecovery(SRCraftRecovery recovery, bool showRetryButton)
        {
            ClearStats();
            _headerText.text = "RECOVER CRAFT";
            if (showRetryButton)
            {
                SetButtonText(base.ButtonLeft, "RETRY / UNDO");
                base.ButtonLeft.AddOnClickEvent(delegate
                {
                    Close();
                    OnRetryClicked?.Invoke();
                });
                base.ButtonLeft.Tooltip = "There's no penalty for trying again. If you didn't complete any contracts or milestones, this is the recommended option.";
            }
            else
            {
                base.ButtonLeft.SetActive(active: false);
            }
            if (recovery.FailMessage != string.Empty)
            {
                _statsHeader.text = recovery.FailMessage;
                SetButtonText(base.ButtonCenter, "ABANDON");
                base.ButtonCenter.Tooltip = "Abandon the craft.";
                base.ButtonRight.SetActive(active: false);
                base.ButtonCenter.AddOnClickEvent(delegate
                {
                    Close();
                    CraftDestroyed?.Invoke();
                });
                return;
            }
            if (recovery.Config != null)
            {
                AddStat("Recoverable Stages", recovery.StageDescription);
                AddStat("Recoverable Parts", recovery.PartDescription);
                if (recovery.RecoverFuel)
                {
                    AddStat("Fuel", recovery.FuelDescription);
                }
            }
            AddStat("Recovery Location", recovery.ClosestLocation.Name);
            AddStat("Recovery Price", Units.GetDistanceString((float)recovery.ClosestDistance));
            if (recovery.NumAstronauts > 0)
            {
                AddStat("Astronauts Onboard", recovery.NumAstronauts.ToString());
            }
            if (recovery.CanRecoverWhole)
            {
                _statsHeader.text  = $"This craft appears to be an intact {recovery.Config.Name} still on its launchpad.";
                SetButtonText(base.ButtonCenter, "RECOVER PARTS");
                base.ButtonCenter.Tooltip = $"Recover any humans onboard, return developed parts to contractors to refurbish, partial refund on other parts, refund the price of fuel.\n\n{Units.GetMoneyString(recovery.PartRecoveryPrice)}";
                SetButtonText(base.ButtonRight, "RECOVER CRAFT");
                base.ButtonRight.Tooltip = $"Recover the entire craft.  Return to integration facility for repairs and try to launch it again.\n\n{Units.GetMoneyString(recovery.RepairPrice)}";
                base.ButtonCenter.AddOnClickEvent(delegate
                {
                    Close();
                    recovery.RecoverParts();
                    CraftRecovered?.Invoke();
                });
                base.ButtonRight.AddOnClickEvent(delegate
                {
                    Close();
                    recovery.RecoverWhole();
                    CraftRecoveredWhole?.Invoke();
                });
            }
            else if (recovery.HumansOnBoard)
            {
                _statsHeader.text  = "This craft has humans on board who should be rescued.";
                SetButtonText(base.ButtonCenter, "RECOVER CREW");
                base.ButtonCenter.Tooltip = $"Recover any humans onboard, abandon the rest of the craft.\n\n{Units.GetMoneyString(recovery.CrewRecoveryPrice)}";
                SetButtonText(base.ButtonRight, "RECOVER PARTS");
                base.ButtonRight.Tooltip = $"Recover any humans onboard, return developed parts to contractors to refurbish, partial refund on other parts, refund the price of fuel.\n\n{Units.GetMoneyString(recovery.PartRecoveryPrice)}";
                base.ButtonCenter.AddOnClickEvent(delegate
                {
                    Close();
                    recovery.RecoverHumans();
                    CraftRecovered?.Invoke();
                });
                base.ButtonRight.AddOnClickEvent(delegate
                {
                    Close();
                    recovery.RecoverParts();
                    CraftRecovered?.Invoke();
                });
            }
            else
            {
                _statsHeader.text  = "This craft can be recovered for parts, if you deem it worth the price.";
                SetButtonText(base.ButtonCenter, "ABANDON");
                base.ButtonCenter.Tooltip = "Abandon the craft.";
                SetButtonText(base.ButtonRight, "RECOVER PARTS");
                base.ButtonRight.Tooltip = $"Return developed parts to contractors to refurbish, partial refund on other parts, refund the price of fuel.\n\n{Units.GetMoneyString(recovery.PartRecoveryPrice)}";
                base.ButtonCenter.AddOnClickEvent(delegate
                {
                    Close();
                    CraftDestroyed?.Invoke();
                });
                base.ButtonRight.AddOnClickEvent(delegate
                {
                    Close();
                    recovery.RecoverParts();
                    CraftRecovered?.Invoke();
                });
            }


        }
    }
}