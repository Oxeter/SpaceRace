namespace Assets.Scripts.SpaceRace.UI
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using ModApi.Ui;
    using UnityEngine;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using ModApi.Career;
    using Assets.Scripts.Craft.Parts.Modifiers;

    using Assets.Scripts.SpaceRace.Modifiers;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Projects;
    using ModApi.Common.Extensions;
    using Assets.Scripts.Career.Contracts;
    using Assets.Scripts.Flight.MapView.UI.Inspector;
    using System.Windows.Forms;

    public class SRTutorialMessages
    {
        private IProgramManager _pm;
        public readonly List<TutorialMessage> Messages;
        public const int MaxMessage = 1000;
        public SRTutorialMessages(IProgramManager pm, XElement element)
        {
            _pm = pm;
            Messages = element.Elements("TutorialMessage").Select(m => new TutorialMessage(m)).ToList();
        }
        public void ShowMessage()
        {
            if (ModSettings.Instance.ShowHelp)
            {
                Game.Instance.UserInterface.CreateListView(new TutorialListViewModel(_pm, Messages));
            }
        }

        public void CheckForSceneMessage()
        {
            switch (Game.Instance.SceneManager.CurrentScene)
            {
                case "Menu":
                if (_pm.CurrentTutorialMessage == 0)
                {
                    _pm.CurrentTutorialMessage = 1;
                    ShowMessage();
                }
                if (_pm.CurrentTutorialMessage == 1 && _pm.Career.TechTree.GetNode("undo").Researched)
                {
                    _pm.CurrentTutorialMessage = 2;
                    ShowMessage();
                }
                break;
                case "Flight":
                if (_pm.CurrentTutorialMessage == 3)
                {
                    _pm.CurrentTutorialMessage = 11;
                    ShowMessage();
                }
                if (_pm.CurrentTutorialMessage == 28 && _pm.StageDevelopments.Keys.Count > 0)
                {
                    _pm.CurrentTutorialMessage = 34;
                    ShowMessage();
                }
                if (_pm.CurrentTutorialMessage == 45 && _pm.Integrations.Keys.Count > 0)
                {
                    _pm.CurrentTutorialMessage = 52;
                    ShowMessage();
                }
                break;
                case "Design":
                if (_pm.CurrentTutorialMessage == 15)
                {
                    _pm.CurrentTutorialMessage = 25;
                    ShowMessage();
                }
                if (_pm.CurrentTutorialMessage == 37)
                {
                    _pm.CurrentTutorialMessage = 45;
                    ShowMessage();
                }
                break;
                default:
                return;
            }
        }

        public void CheckForContractMessage(Contract contract)
         {
            switch (contract.Id)
            {
                case "space-center":
                if (_pm.CurrentTutorialMessage == 2)
                {
                    _pm.CurrentTutorialMessage =3;
                    ShowMessage();
                }
                return;
                case "Karman":
                if (_pm.CurrentTutorialMessage == 11)
                {
                    _pm.CurrentTutorialMessage = 15;
                    ShowMessage();
                }
                if (contract.Status == ContractStatus.Complete)
                {
                    _pm.CurrentTutorialMessage = 70;
                    ShowMessage();
                }
                return;
            }
         }

        public void CheckForIntegrationMessage(IntegrationStatus status)
        {
            switch (status)
            {
                case IntegrationStatus.Stacking:
                if (_pm.CurrentTutorialMessage == 52)
                {
                    _pm.CurrentTutorialMessage = 57;
                    ShowMessage();
                }
                goto default;
                case IntegrationStatus.Stacked:
                if (_pm.CurrentTutorialMessage == 57)
                {
                    _pm.CurrentTutorialMessage = 62;
                    ShowMessage();
                }
                goto default;
                case IntegrationStatus.Ready:
                if (_pm.CurrentTutorialMessage == 62)
                {
                    _pm.CurrentTutorialMessage = 67;
                    ShowMessage();
                }
                goto default;
                default:
                return;
            }
        }

        public void CheckForOtherMessage()
        {
            if (_pm.CurrentTutorialMessage == 25)
            {
                _pm.CurrentTutorialMessage = 28;
                ShowMessage();
            }
            if (_pm.CurrentTutorialMessage == 34)
            {
                _pm.CurrentTutorialMessage = 37;
                ShowMessage();
            }
        }

        public static readonly TutorialMessage[] OldMessages = 
        {
            new TutorialMessage(0, "Welcome", Tutorial, Welcome),
            new TutorialMessage(1, "To Space Center", Tutorial, GoToSpaceCenter),
            new TutorialMessage(10, "Contracts at the Space Center", Tutorial, AtSpaceCenter),
            new TutorialMessage(11, "The Projects Window", Tutorial, AfterKarman),
            new TutorialMessage(20, "Building a Rocket", Tutorial, BuildKarman),
            new TutorialMessage(21, "Developing Hardware", Tutorial, DevelopKarman),
            new TutorialMessage(30, "Passing Time and Keeping Track", Tutorial, WaitToDevelop),
            new TutorialMessage(31, "Developments Complete", Tutorial, DevelopDone),
            new TutorialMessage(40, "Integrating a Craft", Tutorial, ReadyToIntegrate),
            new TutorialMessage(60, "Hardware Deliveries", Tutorial, IntegrateWaiting),
            new TutorialMessage(61, "Integration: Stacking", Tutorial, IntegrateStacking),
            new TutorialMessage(62, "Integration: Rollout", Tutorial, IntegrateRollout),
            new TutorialMessage(63, "Integration: Launch", Tutorial, IntegrateReady),
            new TutorialMessage(70, "Completing a Contract", Tutorial, EndKarman),
            new TutorialMessage(200, "Engine Reliability", Reference, EndKarman),
            new TutorialMessage(200, "Parts and Families", Reference, EndKarman),
        };
                     
        public const string Tutorial = "Tutorial Message";
        public const string Reference = "Reference";

        public const string Welcome = @"Welcome to your SpaceRace career.  These hints should help you get started with the game.
        
The first thing you should do is head to the Tech Tree and purchase the 'I want to cheat a little' node.  This is a buggy mod.  You'll want to undo.

When you've done that, come back here and accept the 'Space Center' contract";

        public const string GoToSpaceCenter = @"SpaceRace does not have a launch button in the menu screen.  Instead it has a button to go to the sapce center.  Since going to the space center is your contract, you should click it now.";

        public const string AtSpaceCenter = @"Welcome to the space center on Cape Canaveral.  The date is January 1, 1950.  This is where you'll manage your space program.  Open the contracts view and accept the Karman Line contract.";

        public const string AfterKarman = @"You will notice that contract had no advance payment.  I have worse news, it also won't pay you the promised $2m on completion.  Instead, that two million dollars will be disbursed to you over time.  If the projects view is not already open, open it at the bottom of the screen.  Click the button to view funding.  You should see you budget here, which shows two sources of funding, base funding and funding from your contract.
        
If you scroll down you'll see details on the contract funding, including how much money is being disbursed to you per day and how many days remain before the funding is depleted.

When you're done contemplating your financial situation, it's time to design a rocket to complete this contract.  Click Build at the bottom of the screen.";

        public const string BuildKarman = @"Now it's time to build a rocket that can reach 100km and return.  You'll notice the fly button is now a simulate button.  If you're feeling confident, go design a rocket and simulate until you're confident it can complete the contract.  When you're happy with it, click the Development button of the flyout at the left side of the screen to start developing your design.
        
If you want some tips, here are a few:

Funds are limited.  Make something cheap and small.

If you customize an engine you'll need to develop it.  You don't have engine devlopment money yet.  Stick to the available models.

With an engine selected, the performance analyzer will give you reliability data.  Don't pick anything too cutting-edge.  Make note of the rated run time.

Unguided avionics save mass and money.  Use fins to keep your rocket flying straight.

Steel tanks require less development time than more advanced types.  Stick with steel for now.

If you're out of ideas, go look up the Aerobee rocket online.  Its engines are among those available to you.  It can fulfill this contract affordably with performance to spare.";

        public const string DevelopKarman = @"Hopefully you have a craft that you'd like to develop now.  If not you'll have to remember this information for later.
        
This flyout is where you develop the parts and stages for your rocket.  Parts that are already developed or in progress do not appear here.  Stages turn green when they are being developed.  

You'll need to choose development contracts for each of your parts and then each of your stages.  You can open the Projects window to see your budget.  Each development has a daily cost and a projected time.  You'll also be agreeing to per unit price and initial production speed.

I suggest you choose contracts that keep your daily cashflow above 0.  You cannot pause a development project, only cancel it.

When all of your parts and stages are under development.  Go to the menu and return to the space center.";

        public const string WaitToDevelop = @"Welcome back.  If you click the show development button on the projects window, you can see the progress of your development projects.
        
The projects window has a button to engage a timewarp until the next project is complete.  You can also use the timewarp in the top right of the screen.

If you find you have extra money, you can toggle a rush order on any projects that are slowing you down.

Warp time until all your parts and stages are developed.";

        public const string DevelopDone = "It looks like all your hardware is developed.  Head back to the build screen to integrate your first rocket.";

        public const string ReadyToIntegrate = "Open the development flyout and click Integrate Craft.  The integration should show up in the project manager under Integrations.  Head back to the space center when you're done.";

        public const string IntegrateWaiting = @"Before you can assemble your rocket, the contractors need to deliver the parts. The integration in the Projects window shows you which contractors and how long they'll take.  

You can toggle rush to rush the contractors for extra money.  Only those contractors whose delivery times will affect the completion date will be paid to rush.

When you are happy with your rush decision, timewarp until the deliveries are complete.";

        public const string IntegrateStacking = @"Now the the hardware orders are fulfilled, the rocket can be stacked.  Your technicians have already been assigned.

You may adjust the number of technicians assigned to the stacking.  Using fewer will slow down progress but also reduce daily expenditure.  If you need to hire more technicians, click Unlock Staffing in the project window.
        
Toggling rush during stacking changes the maximum number of technicians that can work at once.  Technicians above the base, non-rush level will be less efficient.

When you are happy with the number of technicians, timewarp until stacking is complete.";

        public const string IntegrateRollout = @"Your rocket is stacked. In its integration in the Projects window or using the Launch button below, you can roll it out to LC-3.  This will take a few days, depending on the number of technicians assigned.  
        
Once the rollout begins, timewarp until the rocket is ready.";

        public const string IntegrateReady = @"Your rocket is ready to launch.  You can click launch in its integration in the Projects window or using the Launch button below.  This will fuel the rocket and ready it for flight.  Good luck!

If the rocket engine fails to ignite, you can recover the craft.  It will roll back, be repaired, and then you can roll it out again.

If the rocket fails in flight, you'll need to integrate a new one.  Assuming you still like your design, you can repeat the integration directly from the Projects window or the Launch button.  Repetition will speed up stacking and help with engine reliability.

Good luck!";

        public const string EndKarman =@"It looks like you did it!  Great job. The tutorial is now finished.  Recover (abandon) whatever landed and you'll find yourself back at the space center, ready to take on the next contract.  Good luck administrator!";
    }

    public class TutorialMessage
    {
        public int Index;
        public string Title;
        public string Subtitle;
        public string Text;
        public TutorialMessage(int index, string title, string subtitle, string text)
        {
            Index = index;
            Title = title;
            Subtitle = subtitle;
            Text = text;
        }
        public TutorialMessage(XElement element)
        {
            Index = element.GetIntAttribute("index");
            Title = element.GetStringAttribute("title");
            Subtitle = element.GetStringAttributeOrNullIfEmpty("subtitle");
            Text = element.Element("Text").Value;
        }
    }
}
