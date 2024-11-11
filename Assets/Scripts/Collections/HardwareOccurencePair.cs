namespace Assets.Scripts.SpaceRace.Collections
{
    using System;
    using System.Collections.Generic;
    using Assets.Scripts.SpaceRace.Hardware;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using System.Xml.Linq;
    using System.Linq;
    using System.Xml.Serialization;
    using Unity.Mathematics;

    public class HardwareOccurencePair
    {
        [XmlAttribute]
        public int Id;
        [XmlAttribute]
        public int Occurences;

        public HardwareOccurencePair(){}

        public HardwareOccurencePair(int id, int occurences)
        {
            Id = id;
            Occurences = occurences;
        }
        public override string ToString()
        {
            return string.Format("({0:D}, {1:D})", Id, Occurences);
        }
    }

    public class HardwareOccurenceList : List<HardwareOccurencePair>
    {
        public HardwareOccurenceList(){}

        public HardwareOccurenceList(IEnumerable<HardwareOccurenceList> list)
        {
            foreach (HardwareOccurenceList hol in list)
            {
                foreach (HardwareOccurencePair hop in hol) AddPair(hop);
            }
        }
        public HardwareOccurenceList(IEnumerable<HardwareOccurencePair> pairs)
        {
                foreach (HardwareOccurencePair hop in pairs) AddPair(hop);
        }
        public bool IsSame(HardwareOccurenceList list)
        {
            if (Count != list.Count) return false;
            // note this check assumes no duplicate entries
            foreach (HardwareOccurencePair hop in this)
            {
                if (list.Find(h => h.Id == hop.Id && h.Occurences == hop.Occurences) == null) return false;
            }
            return true;
        }
        /// <summary>
        /// Adds a hardware occurence pair to a list, merging it with a matching pair if one exists.
        /// </summary>
        /// <param name="pair">The pair</param>
        /// <returns>True if this added a new pair, false if it incremented an existing pair.</returns>
        /// <exception cref="ArgumentException">Negative occurences.</exception>
        public bool AddPair(HardwareOccurencePair pair)
        {
            if (pair.Occurences == 0) return false;
            if (pair.Occurences < 0) throw new ArgumentException("Addpair used for a negative number of occurences");
            HardwareOccurencePair hop = Find(p => p.Id == pair.Id);
            if (hop == null) 
            {
                Add(new HardwareOccurencePair(pair.Id, pair.Occurences));
                return true;
            }
            else 
            {
                hop.Occurences += pair.Occurences;
                return false;
            }
        }
        public void AddPair(int id, int occurences)
        {
            if (occurences == 0) return;
            if (occurences < 0) throw new ArgumentException("Addpair used for a negative number of occurences");
            HardwareOccurencePair hop = Find(p => p.Id == id);
            if (hop == null) Add(new HardwareOccurencePair(id, occurences));
            else hop.Occurences += occurences;
        }
        public new string ToString()
        {
            return String.Join(" ", this.Select(pair => pair.ToString()));
        }
        public float Similarity(HardwareOccurenceList list)
        {
            int size = Math.Max(this.TotalOccurences(), list.TotalOccurences());
            return (size > 0)? this.Sum(hop => MathF.Min((float)hop.Occurences, (float)list.Occurences(hop.Id))) / (float)size :0F;
        }
        public void SubtractPair(int id, int occurences)
        {
            if (occurences == 0) return;
            if (occurences < 0) throw new ArgumentException("Subtractpair used for a negative number of occurences");
            HardwareOccurencePair hop = Find(p => p.Id == id);
            if (hop == null) throw new ArgumentException("No occurences to subtract from");
            if (hop.Occurences < occurences) throw new ArgumentException("Not enough occurences to subtract from");
            if (hop.Occurences == occurences) Remove(hop);
            if (hop.Occurences > occurences) hop.Occurences -= occurences;
            return;
        }

        public int TotalOccurences()
        {
            return this.Sum(hop=>hop.Occurences);
        }
        /// <summary>
        /// Get the number of occurences in the list of a given id.
        /// </summary>
        /// <param name="id">The id</param>
        /// <returns>The number of occurences of that Id</returns>
        public int Occurences(int id)
        {
            HardwareOccurencePair hop = Find(p => p.Id == id);
            return hop?.Occurences ?? 0;
        }

    }

    public class FamilyOccurenceList : HardwareOccurenceList
    {
        public FamilyOccurenceList(){}
        public FamilyOccurenceList(StageOccurenceList list, IHardwareManager db)
        {
            foreach (HardwareOccurencePair sop in list)
            {
                SRStage stage = db.Get<SRStage>(s => s.Id == sop.Id);
                if (stage != null)
                {
                    foreach (HardwareOccurencePair hop in stage.PartFamilies)
                    {
                        AddPair(hop);
                    }
                }
            }
        }
        public void AddFam(SRPartFamily fam)
        {
            AddPair(new HardwareOccurencePair(fam.Id, 1));
        }
    }


    public class StageOccurenceList : HardwareOccurenceList
    {
    }


}