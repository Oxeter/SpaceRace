using System;
using Assets.Scripts.State;
using Assets.Scripts.Ui;
using ModApi.Flight;
using ModApi.State;
using ModApi.Ui;
using UI.Xml;
using UnityEngine;
using ModApi.Craft;
using ModApi.Flight.UI;
using ModApi.Scenes.Parameters;
using Assets.Scripts.Flight.Sim;
using System.Linq;
using Assets.Scripts.SpaceRace;

namespace Assets.Scripts.Flight.UI{

    //
    // Summary:
    //     Script for the retry flight dialog.
    public class SRRetryFlightDialogScript : StatsDialogScript
    {
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
        public static SRRetryFlightDialogScript Create(Transform parent)
        {
            SRRetryFlightDialogScript retryFlightDialogScript = Game.Instance.UserInterface.CreateDialog("Ui/Xml/StatsDialog", parent, delegate (SRRetryFlightDialogScript d, IXmlLayoutController c)
            {
                d.OnLayoutRebuilt((XmlLayout)c.XmlLayout);
            });
            retryFlightDialogScript.ConfigureButtons();
            return retryFlightDialogScript;
        }

        //
        // Summary:
        //     Restores the state of the game state to the PreFlight tag.
        private static void RestorePreFlightGameState()
        {
            GameState gameState = Game.Instance.GameState;
            string id = gameState.Id;
            string tagPreFlight = gameState.GetTagPreFlight();
            string tagActive = gameState.GetTagActive();
            if (string.IsNullOrWhiteSpace(tagPreFlight))
            {
                throw new Exception($"Unable to restore pre-flight state for game state '{id}' of type '{gameState.Type}'. Game state path: {gameState.RootPath}");
            }
            Game.Instance.GameStateManager.RestoreGameStateTag(id, tagPreFlight, tagActive);
        }

        //
        // Summary:
        //     Configures the buttons.
        private void ConfigureButtons()
        {
            _headerText.text = "EXIT FLIGHT";
            SetButtonText(base.ButtonLeft, "RETRY");
            base.ButtonLeft.AddOnClickEvent(delegate
            {
                OnRetryClicked();
            });
            base.ButtonCenter.SetActive(active: false);
            if (Game.Instance.GameState.Type == GameStateType.Level)
            {
                _headerText.text = "END FLIGHT";
                _statsHeader.text = "Would you like to retry this level or exit?";
                SetButtonText(base.ButtonRight, "EXIT");
            }
            else
            {
                _headerText.text = "RETRY / UNDO";
                _statsHeader.text = "Okay, let's pretend this never happened. We can roll everything back to the way it was before this flight.\n\nWould you like to retry this flight or just undo and exit?";
                SetButtonText(base.ButtonRight, "UNDO & EXIT");
            }

            base.ButtonRight.AddOnClickEvent(delegate
            {
                OnExitClicked();
            });
        }

        //
        // Summary:
        //     Player has elected to cancel their flight and revert back to as things were before
        //     launch.
        private void OnExitClicked()
        {   
            switch (Game.Instance.GameState.Type)
            {
                case GameStateType.Simulation:
                    //Game.Instance.LoadGameStateOrDefault(Game.Instance.GameState.Id, "Active");
                    FlightSceneScript.Instance.ExitFlightScene(false, FlightSceneExitReason.UndoAndExit, "Design");
                    return;
                case GameStateType.Default:
                    ICraftNode spacecenter = Game.Instance.FlightScene.FlightState.CraftNodes.FirstOrDefault(node=> Assets.Scripts.SpaceRace.SRManager.IsSpaceCenter(node));
                    bool flag = Game.Instance.GameState.Type == GameStateType.Level;
                    RestorePreFlightGameState();
                    if (spacecenter != null)
                    {
                        Game.Instance.GameState.PreflightLoadParameters.LaunchCraftId = null;
                        Game.Instance.GameState.PreflightLoadParameters.ResumeCraftNodeId = spacecenter.NodeId;
                        
                    }
                    FlightSceneScript.Instance.ReloadFlightScene(saveFlightState: false, Game.Instance.GameState.PreflightLoadParameters, FlightSceneExitReason.UndoAndExit);
                    return;
                default: throw new Exception("Altered menu should not be visible for levels or planetstudio");
            }

        }

        //
        // Summary:
        //     Player has elected to retry the flight.
        private void OnRetryClicked()
        {
            RestorePreFlightGameState();
            FlightSceneScript.Instance.ReloadFlightScene(saveFlightState: false, Game.Instance.GameState.PreflightLoadParameters, FlightSceneExitReason.Retry);
        }
    }
}