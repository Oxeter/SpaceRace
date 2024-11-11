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

namespace Assets.Scripts.SpaceRace.UI{
    //
    // Summary:
    //     Script for the end flight dialog.
    public class SREndFlightDialogScript : StatsDialogScript
    {
        private IProgramManager _pm;
        //
        // Summary:
        //     Creates the dialog.
        //
        // Parameters:
        //   parent:
        //     The parent canvas use for this dialog.
        //
        // Returns:
        //     The newly created dialog.
        public static SREndFlightDialogScript Create(Transform parent)
        {
            return Game.Instance.UserInterface.CreateDialog("Ui/Xml/StatsDialog", parent, delegate (SREndFlightDialogScript d, IXmlLayoutController c)
            {
                d.OnLayoutRebuilt((XmlLayout)c.XmlLayout);
            });
        }

        //
        // Summary:
        //     Called when the layout has been rebuilt.
        //
        // Parameters:
        //   xmlLayout:
        //     The XML layout.
        protected override void OnLayoutRebuilt(XmlLayout xmlLayout)
        {
            _pm = SRManager.Instance.pm;
            base.OnLayoutRebuilt(xmlLayout);
            base.ButtonCancel.SetActive(active: false);
            SetButtonText(base.ButtonLeft, "SAVE FLIGHT");
            base.ButtonLeft.Tooltip = "Exit the flight scene and keep this as an active craft in case you want to resume control of it later.";
            base.ButtonLeft.AddOnClickEvent(delegate
            {
                OnExitClicked();
            });
            base.ButtonLeft.SetActive(Game.Instance.GameState.Type != GameStateType.Simulation);
            SetButtonText(base.ButtonCenter, "RETRY / UNDO");
            base.ButtonCenter.Tooltip = "Undo this flight and try again.";
            base.ButtonCenter.AddOnClickEvent(delegate
            {
                OnRetryClicked();
            });
            base.ButtonCenter.SetActive(Game.Instance.GameState.Validator.IsItemAvailable("Cheats.UndoRetry"));
            SetButtonText(base.ButtonRight, "RECOVER CRAFT");
            base.ButtonRight.Tooltip = "View options for recovering this craft. If it's close enough to a recovery location, you can scrap its parts for money.";
            base.ButtonRight.AddOnClickEvent(delegate
            {
                OnRecoverCraftClicked();
            });
            base.ButtonRight.SetActive(Game.Instance.FlightScene.CraftNode.NodeId > 0);
            UpdateEndFlightStats();
        }

        //
        // Summary:
        //     Called when the save flight button is clicked.
        private void OnExitClicked()
        {
            _panel.SetActive(active: false);
            SRSaveFlightDialogScript saveFlightDialogScript = SRSaveFlightDialogScript.Create(base.transform, temporaryFlightState: false);
            saveFlightDialogScript.OnRetryClicked = delegate
            {
                OnRetryClicked();
                _panel.SetActive(active: false);
            };
            saveFlightDialogScript.OnExitClicked = delegate
            {
                Close();
            };
            saveFlightDialogScript.Closed += delegate
            {
                _panel.SetActive(active: true);
            };
        }

        //
        // Summary:
        //     Called when the recover craft button is clicked.
        private void OnRecoverCraftClicked()
        {
            IFlightScene flightSceneScript = Game.Instance.FlightScene;
            CraftNode craftNode = FlightSceneScript.Instance.CraftNode as CraftNode;
            SRCraftRecovery recovery = new SRCraftRecovery(SRManager.Instance.pm, craftNode, new CraftNodeDataDynamic(craftNode));
            bool showRetryButton = Game.Instance.GameState.Validator.IsItemAvailable("Cheats.UndoRetry");
            SRRecoverCraftDialogScript recoverCraftDialogScript = SRRecoverCraftDialogScript.Create(recovery, showRetryButton);
            _panel.SetActive(active: false);
            recoverCraftDialogScript.OnRetryClicked = delegate
            {
                OnRetryClicked();
                _panel.SetActive(active: false);
            };
            recoverCraftDialogScript.CraftDestroyed = delegate
            {   
                Close();
                _pm.GoToSpaceCenter(true);
            };
            recoverCraftDialogScript.CraftRecovered = delegate
            {
                Close();
                _pm.GoToSpaceCenter(true);
            };
            recoverCraftDialogScript.CraftRecoveredWhole = delegate
            {
                Close();
                _pm.GoToSpaceCenter(false);
            };
            recoverCraftDialogScript.Closed += delegate
            {
                _panel.SetActive(active: true);
            };
        }

        //
        // Summary:
        //     Called when the retry button is clicked.
        private void OnRetryClicked()
        {
            _panel.SetActive(active: false);
            SRRetryFlightDialogScript retryFlightDialogScript = SRRetryFlightDialogScript.Create(base.transform);
            retryFlightDialogScript.Closed += delegate
            {
                _panel.SetActive(active: true);
            };
        }

        //
        // Summary:
        //     Updates the stats.
        private void UpdateEndFlightStats()
        {
            ClearStats();
            _headerText.text = "END FLIGHT";
            _statsHeader.text = "FLIGHT STATS";
            Assets.Scripts.Flight.FlightLog flightLog = FlightSceneScript.Instance.FlightLog;
            AddStat("Flight Time", TimeManagerScript.GetTimeString((long)flightLog.FlightTime));
            AddStat("Max Velocity", Units.GetVelocityString((int)flightLog.MaxVelocity));
            AddStat("Max Altitude", Units.GetDistanceString((float)flightLog.MaxAltitude));
            if (flightLog.IsNewLaunch)
            {
                AddStat("Max Q Altitude", Units.GetDistanceString((float)flightLog.MaxQ));
            }

            AddStat("Distance Traveled", Units.GetDistanceString((float)flightLog.DistanceTraveled));
        }

    }
}

