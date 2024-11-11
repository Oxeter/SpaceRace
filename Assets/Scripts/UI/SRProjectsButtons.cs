using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ModApi.Common;
using ModApi.Common.Extensions;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using Assets.Scripts.SpaceRace.Projects;

namespace Assets.Scripts.SpaceRace.Ui
{


    public static class SRProjectsButtons
    {
        public const string flightProjectToggleButtonId = "srprojects-flight-button";
        public const string flightCrewButtonId = "srprojects-crew-button";

        public const string designProjectToggleButtonId = "srprojects-design-button";   

        public const string developmentToggleButtonId = "srdevelopment-button"; 

        public const string designSpaceCenterButtonId = "srprojects-design-space-center-button";  
        public const string menuSpaceCenterButtonId = "srprojects-menu-space-center-button";  

        public static void Initialize()
        {
            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(
                UserInterfaceIds.Flight.NavPanel, 
                OnBuildNavPanel);

            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(
                UserInterfaceIds.Design.DesignerUi, 
                OnBuildDesignerUI);    

            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(
                UserInterfaceIds.Flight.FlightSceneUI, 
                OnBuildFlightSceneUI);
            
            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(
                UserInterfaceIds.Design.MenuPanel,
                OnBuildMenuPanelUI);
            
            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(
                UserInterfaceIds.Menu.MenuUi,
                OnBuildMenuUI);

            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(
                UserInterfaceIds.Flight.TimePanel, 
                OnBuildTimePanel);

        }




        private static void OnBuildMenuUI(BuildUserInterfaceXmlRequest request)
        {
           
            if (Game.IsCareer)
            {
                var nameSpace = XmlLayoutConstants.XmlNamespace;
                var launchButton = request.XmlDocument
                    .Descendants(nameSpace + "ContentButton")
                    .First(x => (string)x.Attribute("onClick") == "OnLaunchButtonClicked();");
                launchButton.SetAttributeValue("id", menuSpaceCenterButtonId);
                launchButton.Element(nameSpace+"TextMeshPro").Add(new XAttribute("fontSize", 24));
                launchButton.Element(nameSpace+"TextMeshPro").Attribute("text").SetValue("SPACE CENTER");
                request.AddOnLayoutRebuiltAction(xmlLayoutController =>
                {
                    var button = xmlLayoutController.XmlLayout.GetElementById(menuSpaceCenterButtonId);
                    button.AddOnClickEvent(() => SRManager.Instance.pm.GoToSpaceCenter(false), true);
                });
            }
        }
        private static void OnBuildMenuPanelUI(BuildUserInterfaceXmlRequest request)
        {
            if (Game.IsCareer)
            {
                var nameSpace = XmlLayoutConstants.XmlNamespace;
                var exitButton = request.XmlDocument
                    .Descendants(nameSpace + "Button")
                    .First(x => (string)x.Attribute("onClick") == "OnExitButtonClicked();");
                XElement scButton = new XElement(
                    nameSpace + "Button",
                    new XAttribute("id", designSpaceCenterButtonId),
                    new XAttribute("class", "btn audio-btn-big"),
                    new XAttribute("rectAlignment","LowerLeft"),
                    new XAttribute("tooltip","Exit the designer and return to the Space Center"),
                    new XElement(
                        nameSpace+"TextMeshPro",
                        new XAttribute("text","SPACE CENTER")
                        )
                );
                exitButton.Element(nameSpace+"TextMeshPro").Attribute("text").SetValue("MAIN MENU");
                exitButton.AddBeforeSelf(scButton);
                request.AddOnLayoutRebuiltAction(xmlLayoutController =>
                {
                    var button = xmlLayoutController.XmlLayout.GetElementById(designSpaceCenterButtonId);
                    Debug.Log(button == null? "Button not found" : "Button found");
                    button.AddOnClickEvent(() => SRManager.Instance.pm.GoToSpaceCenter());
                });
            }
        }
        private static void OnBuildFlightSceneUI(BuildUserInterfaceXmlRequest request)
        {
            if (Game.IsCareer)
            {
                var nameSpace = XmlLayoutConstants.XmlNamespace;
                var timePanel = request.XmlDocument
                    .Descendants(nameSpace + "ChildXmlLayout")
                    .First(x => (string)x.Attribute("id") == "time-panel");
                timePanel.Attribute("controller").SetValue("Assets.Scripts.SpaceRace.UI.SRTimePanelController");
                timePanel.Parent.Add(
                    new XElement(
                        nameSpace + "ChildXmlLayout",
                        new XAttribute("id", "space-center-ui"),
                        new XAttribute("viewPath","SpaceRace/Flight/SpaceCenterUi"),
                        new XAttribute("controller","Assets.Scripts.SpaceRace.UI.SpaceCenterUiController")
                    )
                );


            }
        }
        private static void OnBuildTimePanel(BuildUserInterfaceXmlRequest request)
        {
            if (Game.IsCareer)
            {
                var nameSpace = XmlLayoutConstants.XmlNamespace;
                var timeText = request.XmlDocument
                    .Descendants(nameSpace + "TextMeshPro")
                    .First(x => (string)x.Attribute("id") == "time-text");
                timeText.Attribute("offsetXY").SetValue("-175 0");
                timeText.Attribute("width").SetValue("190");
                timeText.Parent.Parent.Attribute("width").SetValue("380");
            }
        }
        private static void OnBuildNavPanel(BuildUserInterfaceXmlRequest request)
        {
            var nameSpace = XmlLayoutConstants.XmlNamespace;
            var translationButton = request.XmlDocument
                .Descendants(nameSpace + "ContentButton")
                .First(x => (string)x.Attribute("id") == "nav-sphere-translation");
            translationButton.Parent.Add(
                new XElement(
                    nameSpace + "ContentButton",
                    new XAttribute("id", flightProjectToggleButtonId),
                    new XAttribute("class", "panel-button audio-btn-click"),
                    new XAttribute("name", "NavPanel.ToggleSRProjects"),
                    new XAttribute("tooltip", "SpaceRace Project Manager"),
                    new XElement(
                        nameSpace + "Image",
                        new XAttribute("class", "panel-button-icon"),
                        new XAttribute("sprite", "Ui/Sprites/Career/IconExplorationContact"))                 

                )
            );
            translationButton.Parent.Add(
                new XElement(
                    nameSpace + "ContentButton",
                    new XAttribute("id", flightCrewButtonId),
                    new XAttribute("class", "panel-button audio-btn-click"),
                    new XAttribute("name", "NavPanel.AssignCrew"),
                    new XAttribute("tooltip", "Send Crew"),
                    new XElement(
                        nameSpace + "Image",
                        new XAttribute("class", "panel-button-icon"),
                        new XAttribute("sprite", "Ui/Sprites/Design/IconButtonAssignCrew"))                 
                )
            ); 
            request.AddOnLayoutRebuiltAction(xmlLayoutController =>
            {
                var button = xmlLayoutController.XmlLayout.GetElementById(flightProjectToggleButtonId);
                var button2 = xmlLayoutController.XmlLayout.GetElementById(flightCrewButtonId);
                if (Game.IsCareer)
                {
                    button.AddOnClickEvent(SRManager.Instance.pm.ToggleProgramPanel);
                    button2.AddOnClickEvent(SRManager.Instance.pm.OnClickCrewButton);
                }
                else 
                {
                    button.SetAndApplyAttribute("active", "false");
                    button2.SetAndApplyAttribute("active", "false");
                }
            }); 
        }



        private static void OnBuildDesignerUI(BuildUserInterfaceXmlRequest request)
        {
            var ns = XmlLayoutConstants.XmlNamespace;
            var launchButton = request.XmlDocument
                .Descendants(ns + "Panel")
                .First(x => (string)x.Attribute("name") == "LaunchButton");
            launchButton.Attribute("tooltip").SetValue("Simulate Launch");
            launchButton.Parent.Add(
                new XElement(
                    ns + "Panel",
                    new XAttribute("id", designProjectToggleButtonId),
                    new XAttribute("class", "toggle-button audio-btn-click"),
                    new XAttribute("name", "Designer.ToggleSRProjects"),
                    new XAttribute("tooltip", "Space Race Project Manager"),
                    new XElement(
                        ns + "Image",
                        new XAttribute("class", "toggle-button-icon"),
                        new XAttribute("sprite", "Ui/Sprites/Career/IconExplorationContact"))              

                )
            );
            ns = XmlLayoutConstants.XmlNamespace;
            var viewButton = request.XmlDocument
                .Descendants(ns + "Panel")
                .First(x => (string)x.Attribute("internalId") == "flyout-view");
            viewButton.Parent.Add(
                new XElement(
                    ns + "Panel",
                    new XAttribute("id", developmentToggleButtonId),
                    new XAttribute("class", "toggle-button toggle-flyout audio-btn-click"),
                    new XAttribute("internalId", "flyout-development"),
                    new XAttribute("name", "ButtonPanel.DevelopmentButton"),
                    new XAttribute("tooltip", "Stage and Part Development"),
                    new XAttribute("onClick", "OnToggleFlyoutButtonClicked(this);"),
                    new XElement(
                        ns + "Image",
                        new XAttribute("class", "toggle-button-icon"),
                        new XAttribute("sprite", "SpaceRace/Sprites/TutorialGear"))              

                )
            );
            var viewFlyout = request.XmlDocument
                .Descendants(ns + "Panel")
                .First(x => (string)x.Attribute("id") == "flyout-view");
            viewFlyout.Parent.Add(
                new XElement(
                    ns + "Panel",
                    new XAttribute("id", "flyout-development"),
                    new XAttribute("class", "panel flyout"),
                    new XAttribute("width", "360"),
                    new XAttribute("active", "false"),
                    new XElement(
                        ns + "Panel",
                        new XAttribute("class", "flyout-header"),
                            new XElement(
                            ns + "TextMeshPro",
                            new XAttribute("text", "DEVELOPMENT")
                        ), 
                        new XElement(
                            ns + "Image",
                            new XAttribute("class", "flyout-close-button audio-btn-back")
                        )
                    ),
                    new XElement(
                        ns + "Panel",
                        new XAttribute("class", "flyout-content no-image"),
                        new XElement(
                            ns + "ChildXmlLayout",
                            new XAttribute("viewPath", "SpaceRace/Designer/DevelopmentFlyout"),
                            new XAttribute("controller", "Assets.Scripts.SpaceRace.Ui.DevelopmentPanelScript")
                        )
                    )
                )   
            );          
        }
    }
}