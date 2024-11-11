namespace Assets.Scripts.SpaceRace.Collections
{
    using System.Collections.Generic;
    using Assets.Scripts.SpaceRace.Hardware;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using System.Xml.Linq;
    using System.Linq;
    using ModApi.Common.Extensions;
    using System;
    using System.Xml.Serialization;
    using System.Windows.Forms;

    public class Technology
    {
        [XmlAttribute]
        public string Mod;
        [XmlAttribute]
        public string Tech;
        [XmlAttribute]
        public string Description;
        public Technology(){}
        public Technology(string mod, string tech, string description = null)
        {
            Mod = mod;
            Tech = tech;
            Description = description;
        }
    }
    
    public class TechOccurenceTriple
    {
        [XmlAttribute]
        public string Mod;
        [XmlAttribute]
        public string Tech;
        [XmlAttribute]
        public int Occurences;
        public TechOccurenceTriple(){}
        public TechOccurenceTriple(string mod, string tech, int occurences)
        {
            Mod = mod;
            Tech = tech;
            Occurences = occurences;
        }
    }
    /// <summary>
    /// List of technologies related to a part, stage, craft or project.
    /// Should never have duplicates.
    /// The AddTech method prevents duplicates being added.
    /// </summary>
    public class TechList : List<Technology>
    {
        public void AddTech(string mod, string tech, string description = null)
        {
            if (mod != null && tech != null && !this.Any(tot=> tot.Mod==mod&& tot.Tech==tech))
            {
                Add(new Technology(mod, tech, description));
            }
        }
        public void Merge(IEnumerable<Technology> techs)
        {
            foreach (Technology tot in techs)
            {
                AddTech(tot.Mod, tot.Tech);
            }
        }
        public TechList(){}
        public TechList(string mod, IEnumerable<string> techs) : this(techs.Select(str => new Technology(mod, str))){}

        public TechList(IEnumerable<Technology> list)
        {
            Merge(list);
        }

        public TechList(TechList list)
        {
            Merge(list);
        }

        public TechList(IEnumerable<TechList> list)
        {
            foreach (TechList techlist in list)
            {
                Merge(techlist); 
            }
        }
        public new string ToString()
        {
            return string.Join(", ", this.Select(tech => string.Format("{0}:{1}", tech.Mod, tech.Tech)));
        }
        public new bool Contains(Technology triple)
        {
            return this.Any(tot => tot.Mod == triple.Mod && tot.Tech == triple.Tech);
        }
        public bool Contains(string mod, string tech)
        {
            return this.Any(tot => tot.Mod == mod && tot.Tech == tech);
        }
    }

    public class TechOccurenceList : List<TechOccurenceTriple>
    {
        public bool IsSame(TechOccurenceList list)
        {
            if (Count != list.Count) return false;
            // note this check assumes no duplicate entries
            foreach (TechOccurenceTriple tot in this)
            {
                if (list.Find(h => h.Mod == tot.Mod && h.Tech == tot.Tech && h.Occurences == tot.Occurences) == null) return false;
            }
            return true;
        }
        public void IncrementTech(Technology tech)
        {
            TechOccurenceTriple match = Find(t => t.Mod==tech.Mod && t.Tech == tech.Tech);
            if (match == null) Add(new TechOccurenceTriple(tech.Mod, tech.Tech, 1));
            else match.Occurences += 1;
        }
        public void AddTriple(TechOccurenceTriple tot)
        {
            if (tot.Occurences == 0) return;
            if (tot.Occurences < 0) throw new ArgumentException("Addtriple used for a negative number of occurences");
            TechOccurenceTriple match = Find(t => t.Mod==tot.Mod && t.Tech == tot.Tech);
            if (match == null) Add(new TechOccurenceTriple(tot.Mod, tot.Tech, tot.Occurences));
            else match.Occurences += tot.Occurences;
        }
        public void AddTriple(string mod, string tech, int occurences = 1)
        {
            AddTriple(new TechOccurenceTriple(mod, tech, occurences));
        }
        public void AddTechList(TechList techs)
        {
            foreach (Technology tot in techs)
            {
                AddTriple(tot.Mod, tot.Tech, 1);
            }
        }
        public override string ToString()
        {
            return string.Join(", ", this.Select(tot =>string.Format("{0}:{1} x{2:D}", tot.Mod, tot.Tech, tot.Occurences)));
        }
        public int Occurences(string mod, string tech)
        {
            return this.Find(tot => tot.Mod == mod && tot.Tech == tech)?.Occurences ?? 0;
        }
        public int Occurences(Technology tech)
        {
            return this.Find(tot => tot.Mod == tech.Mod && tot.Tech == tech.Tech)?.Occurences ?? 0;
        }
    }
}