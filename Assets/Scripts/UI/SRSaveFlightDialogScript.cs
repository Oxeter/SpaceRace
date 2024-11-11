#region Assembly SimpleRockets2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 
#endregion

using System;
using Assets.Scripts.Ui;
using ModApi.Craft;
using ModApi.Flight;
using ModApi.Ui;
using UI.Xml;
using UnityEngine;
using ModApi.Flight.UI;
using Assets.Scripts.Flight;
using System.Linq;

namespace Assets.Scripts.SpaceRace.UI{

    //
    // Summary:
    //     Script for the save flight dialog.
    public class SRSaveFlightDialogScript : StatsDialogScript
    {
        public Action OnExitClicked { get; set; }
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
        //   parent:
        //     The parent canvas use for this dialog.
        //
        //   temporaryFlightState:
        //     if set to true if a flight state is temporary.
        //
        // Returns:
        //     The newly created dialog.
        public static SRSaveFlightDialogScript Create(Transform parent, bool temporaryFlightState)
        {
            SRSaveFlightDialogScript saveFlightDialogScript = Game.Instance.UserInterface.CreateDialog("Ui/Xml/StatsDialog", parent, delegate (SRSaveFlightDialogScript d, IXmlLayoutController c)
            {
                d.OnLayoutRebuilt((XmlLayout)c.XmlLayout);
            });
            saveFlightDialogScript.ConfigureButtons();
            return saveFlightDialogScript;
        }

        //
        // Summary:
        //     Determines whether the craft is on a collision course with the planet.
        private static bool IsOnCollisionCourse()
        {
            ICraftNode craftNode = FlightSceneScript.Instance.CraftNode;
            if (!craftNode.InContactWithPlanet)
            {
                double height = craftNode.Parent.PlanetData.AtmosphereData.Height;
                if (!craftNode.IsDestroyed && craftNode.Orbit.Eccentricity <= 1.0 && craftNode.Orbit.PeriapsisDistance < craftNode.Parent.PlanetData.Radius + height + 1000.0)
                {
                    return true;
                }
            }

            return false;
        }

        //
        // Summary:
        //     Configures the buttons.
        private void ConfigureButtons()
        {
            bool spacecenter = FlightSceneScript.Instance.CraftNode.NodeId < 0;
            string text = "Save this flight so that you can resume it later from the Resume Flight button in the main menu.";
            text += "\n\nIf you're not happy with what you accomplished this flight, then it might be best to Retry / Undo instead.";
            if (IsOnCollisionCourse())
            {
                text += "\n\nYour craft is on a collision course with the planet and will crash if left unattended. Are you sure you want to save and exit?";
            }

            _headerText.text = "SAVE FLIGHT";
            _statsHeader.text = text;
            SetButtonText(base.ButtonLeft, "RETRY / UNDO");
            base.ButtonLeft.AddOnClickEvent(delegate
            {
                Close();
                OnRetryClicked?.Invoke();
            });
            base.ButtonLeft.Tooltip = "There's no penalty for trying again. If you didn't complete any contracts or milestones, this is the recommended option.";
            base.ButtonLeft.SetActive(Game.Instance.GameState.Validator.IsItemAvailable("Cheats.UndoRetry"));
            base.ButtonCenter.SetActive(active: false);
            SetButtonText(base.ButtonRight, spacecenter? "SAVE & EXIT" : "SPACE CENTER");
            base.ButtonRight.AddOnClickEvent(delegate
            {
                Close();
                OnExitClicked?.Invoke();
                if (spacecenter)
                {
                    FlightSceneScript.Instance.ExitFlightScene(true, FlightSceneExitReason.SaveAndExit, "Menu");
                }
                else
                {
                    SRManager.Instance.pm.GoToSpaceCenter(false);
                }
            });
        }

        //
        // Summary:
        //     Player has elected to cancel their flight and revert back to as things were before
        //     launch.
        
       
    }
}