namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using UnityEngine;
    using Assets.Scripts.SpaceRace.Projects;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.Modifiers;
    public interface ISRPartMod
    {   
        public string TypeId{get;}
        /// <summary>
        /// If a part has more than one ISRPartMod modifier, the highest priority one will be used for development.
        /// Base SpaceRace parts are all priority 0 or 1 so far.
        /// </summary>
        public int SRPriority{get;}
        /// <summary>
        /// An easy way for modifier scripts to access whether hardware failures are enabled in the settings.
        /// </summary>
        public virtual bool HardwareFailures => ModSettings.Instance.HardwareFailures;
        /// <summary>
        /// The designer part category these parts belong to.  
        /// If null, part will not be developed separately but as part of a stage.
        /// </summary>
        public string DesignerCategory {get;}
        /// <summary>
        /// Which entries in the part generated XML are relevant to catagorizing families.  
        /// Key=modifier Value=attribute names of the properties
        /// </summary>
        public Dictionary<string, List<string>> FamilyInfo{get;}
        /// <summary>
        /// Which entries in the part generated XML are relevant to catagorizing parts.  
        /// Key=modifier Value=attribute names of the properties
        /// IMPORTANT: This may be run by a different instance than the one on the part.
        /// Use part.GetModifier<ThisModifier>() to access the relevent fields.
        /// IMPORTANT: This should contain the entries chosen for FamilyInfo.
        /// </summary>
        public Dictionary<string, List<string>> PartInfo{get;}
        /// <summary>
        /// Function that takes a part and generates a list of technologies.
        /// IMPORTANT: This may be run by a different instance than the one on the part.
        /// Use part.GetModifier<ThisModifier>() to access the relevent fields.
        /// </summary>
        /// <param name="part">The part</param>
        /// <paramref name="family">Whether to retrict to techs that a family should have in common</param>
        /// <returns>A list of technologies (strings)</returns>
        public TechList Technologies(PartData part, bool family = false);
        /// <summary>
        /// Returns the player-facing name for a technology.  Defaults to returning itself.
        /// </summary>
        /// <param name="tech">the tech name</param>
        /// <returns>the nicer looking tech name</returns>
        public virtual string TechDisplayName(string tech)
        {
            return tech;
        }
        /// <summary>
        /// Produces a efficiencyfactor for a project.
        /// Call the base method at the end of any override to provide defaults for unhandlded values.
        /// </summary>
        /// <param name="tech">the name of the tech </param>
        /// <param name="developedtech">the list of developed techs, 
        /// containing how many contractor have developed each tech</param>
        /// <param name="contractorDeveloped">Has the contractor developed this tech?  
        /// Will always be true for integrations</param>
        /// <param name="category">The type of project</param>
        /// <returns>The efficiencyfactor (name and percentage)</returns>
        public virtual EfficiencyFactor EfficiencyFactor(string tech, int timesDeveloped, bool contractorDeveloped, ProjectCategory category)
        {
            if (category == ProjectCategory.Part)
            {
                if (tech.StartsWith("PartFamily"))
                {
                    if (contractorDeveloped)
                    {
                        return new EfficiencyFactor(){
                        Text = "Variant of our developed part",
                        Penalty = -0.75f
                        };
                    }
                    else if (timesDeveloped > 0)
                    {
                        return new EfficiencyFactor(){
                        Text = "Variant of a developed part",
                        Penalty = -0.25f
                        };
                    }
                }
                else if (!contractorDeveloped)
                {
                    string techdisplayname = TechDisplayName(tech);
                    if (timesDeveloped == 0) 
                    {
                        return new EfficiencyFactor(){
                            Text = "Novel tech: "+ techdisplayname,
                            Penalty = 0.75f
                        };
                    }
                    else if (timesDeveloped == 1)
                    {
                        return new EfficiencyFactor(){
                            Text = "Proprietary tech: "+ techdisplayname,
                            Penalty = 0.5f
                        };
                    }
                    else 
                    {
                        return new EfficiencyFactor(){
                            Text = "Unfamiliar tech: " + techdisplayname,
                            Penalty = 0.25f
                        };
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Function that produced the rows in the dev panel to describe the part.
        /// IMPORTANT: This may be run by a different instance than the one on the part.
        /// Use part.GetModifier<ThisModifier>() to access the relevent fields.
        /// </summary>
        /// <param name="part">the part</param>
        /// <returns>A dictionary of propert names and values</returns>
        public Dictionary<string, string> DevPanelDisplay(PartData part);
        /// <summary>
        /// Generates a generic bids to develop a part, to be modifed for specific contractors by the CostCalculator. ProductionCapacity is currently ignored by the CostCalulator.
        /// </summary>
        /// <param name="part">the part</param>
        /// <returns>A bid with generic contractor info</returns>
        public PartDevBid GenericBid(PartData part);
        /// <summary>
        /// Called during designer changes and at launch to set reliability fields and anything else you might want to do.
        /// </summary>
        /// <param name="integratedtech">The occurence list of tech integrated by the program</param>
        public virtual void UpdateInfo(IProgramManager pm)
        {

        }
        /// <summary>
        /// If this part is not in an existing part family, this controls the name of the part family that appears for stages that contain this part.
        /// </summary>
        /// <param name="part">The part</param>
        /// <returns>The family name</returns>
        public virtual string UndevelopedFamilyName(PartData part)
        {
            return DesignerCategory == null? FamilyName(part) : "Undeveloped "+FamilyName(part);
        }
        /// <summary>
        /// If this part is not in an existing part family, this controls the name of the part family that it creates for itself when developed.
        /// </summary>
        /// <param name="part">The part</param>
        /// <returns>The family name</returns>
        public virtual string FamilyName(PartData part)
        {
            return part.Name+" family";
        }
        /// <summary>
        /// The cost contribution of a part to a stage's cost.  This should only depend on family properties, so there is no incentive to develop bogus stages with the cheapest member of a family.  This should be roughly 20% of price for parts that get developed and 100% for parts that don't.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public abstract double FamilyStageCost(PartData part);
        /// <summary>
        /// The name to use for a part that has not been assigned a permanent name by being developed.
        /// </summary>
        /// <param name="part"></param>
        /// <returns>The name</returns>
        public virtual string UndevelopedPartName(PartData part)
        {
            return part.Name;
        }
    }
}