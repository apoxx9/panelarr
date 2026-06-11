using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Qualities
{
    public class Quality : IEmbeddedDocument, IEquatable<Quality>
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Quality()
        {
        }

        private Quality(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public bool Equals(Quality other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as Quality);
        }

        public static bool operator ==(Quality left, Quality right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Quality left, Quality right)
        {
            return !Equals(left, right);
        }

        // Quality means SOURCE fidelity, not container: paper sources rank
        // below unknown provenance, unknown below known-digital sources.
        // Container preference (.cbz vs .cbr) belongs to custom formats.
        // Ids 3-7 were the retired container qualities (CBR/CBZ/CB7/Web/HD);
        // migration 010 remapped them — do not reuse those ids.
        public static Quality Unknown => new Quality(0, "Unknown");
        public static Quality PDF => new Quality(1, "PDF");
        public static Quality EPUB => new Quality(2, "EPUB");
        public static Quality Scan => new Quality(8, "Scan");
        public static Quality C2C => new Quality(9, "C2C");
        public static Quality Archive => new Quality(10, "Archive");
        public static Quality WebRip => new Quality(11, "WebRip");
        public static Quality Digital => new Quality(12, "Digital");

        static Quality()
        {
            All = new List<Quality>
            {
                Unknown,
                PDF,
                EPUB,
                Scan,
                C2C,
                Archive,
                WebRip,
                Digital
            };

            AllLookup = new Quality[All.Select(v => v.Id).Max() + 1];
            foreach (var quality in All)
            {
                AllLookup[quality.Id] = quality;
            }

            DefaultQualityDefinitions = new HashSet<QualityDefinition>
            {
                new QualityDefinition(Quality.Unknown)  { Weight = 1,  MinSize = 0, MaxSize = null, GroupWeight = 1 },
                new QualityDefinition(Quality.PDF)      { Weight = 10, MinSize = 0, MaxSize = null, GroupWeight = 10 },
                new QualityDefinition(Quality.EPUB)     { Weight = 20, MinSize = 0, MaxSize = null, GroupWeight = 20 },
                new QualityDefinition(Quality.Scan)     { Weight = 30, MinSize = 0, MaxSize = null, GroupWeight = 30 },
                new QualityDefinition(Quality.C2C)      { Weight = 35, MinSize = 0, MaxSize = null, GroupWeight = 35 },
                new QualityDefinition(Quality.Archive)  { Weight = 40, MinSize = 0, MaxSize = null, GroupWeight = 40 },
                new QualityDefinition(Quality.WebRip)   { Weight = 45, MinSize = 0, MaxSize = null, GroupWeight = 45 },
                new QualityDefinition(Quality.Digital)  { Weight = 50, MinSize = 0, MaxSize = null, GroupWeight = 50 },
            };
        }

        public static readonly List<Quality> All;

        public static readonly Quality[] AllLookup;

        public static readonly HashSet<QualityDefinition> DefaultQualityDefinitions;

        public static Quality FindById(int id)
        {
            if (id == 0)
            {
                return Unknown;
            }
            else if (id > AllLookup.Length)
            {
                throw new ArgumentException("ID does not match a known quality", nameof(id));
            }

            var quality = AllLookup[id];

            if (quality == null)
            {
                throw new ArgumentException("ID does not match a known quality", nameof(id));
            }

            return quality;
        }

        public static explicit operator Quality(int id)
        {
            return FindById(id);
        }

        public static explicit operator int(Quality quality)
        {
            return quality.Id;
        }
    }
}
