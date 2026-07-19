using System.Collections.Generic;
using System.Text;
using Verse;

namespace RebalancePatches
{
    public static class ModEligibility
    {
        private static readonly Dictionary<string, bool> activeCache = new Dictionary<string, bool>();

        private static readonly Dictionary<string, string> names = new Dictionary<string, string>
        {
            { "ludeon.rimworld.royalty", "Royalty" },
            { "ludeon.rimworld.ideology", "Ideology" },
            { "ludeon.rimworld.biotech", "Biotech" },
            { "ludeon.rimworld.anomaly", "Anomaly" },
            { "ludeon.rimworld.odyssey", "Odyssey" },
            { "cn.rimiot", "RimIOT - Logistic Matrix" },
            { "hlx.ultratechalteredcarbon", "Altered Carbon 2" },
            { "moistestwhale.gitscyberbrains", "GiTS Cyberbrains" },
            { "redmattis.bigsmall.core", "Big and Small - Genes & More" },
            { "redmattis.bigsmall", "Big and Small - Races" },
            { "redmattis.yokai", "Big and Small - Yokai" },
            { "redmattis.bsslimes", "Big and Small - Slimes" },
            { "redmattis.heaven", "Big and Small - Heaven and Hell" },
            { "redmattis.morexenos", "Big and Small - More Xenotypes" },
            { "redmattis.lamiasandothersnakes", "Big and Small - Lamias" },
            { "redmattis.geneextractor", "Gene Extractor Tiers" },
            { "redmattis.genenodes", "Gene Nodes" },
            { "wvc.sergkart.races.biotech", "WVC - Xenotypes and Genes" },
            { "oskarpotocki.vfe.pirates", "VFE - Pirates" },
            { "oskarpotocki.vfe.empire", "VFE - Empire" },
            { "oskarpotocki.vfe.insectoid2", "VFE - Insectoids 2" },
            { "vanillaexpanded.vwe", "Vanilla Weapons Expanded" },
            { "vanillaexpanded.vwec", "VWE - Coilguns" },
            { "vanillaexpanded.skills", "Vanilla Skills Expanded" },
            { "vanillaexpanded.vmemese", "VIE - Memes and Structures" },
            { "vanillaexpanded.vaewaste", "VAE - Waste Animals" },
            { "vanillaexpanded.vaeaccessories", "VAE - Accessories" },
            { "vanillaexpanded.gravship", "Vanilla Gravship Expanded - Chapter 1" },
            { "vanillaexpanded.vanillasocialinteractionsexpanded", "Vanilla Social Interactions Expanded" },
            { "vanillaracesexpanded.highmate", "VRE - Highmate" },
            { "vanillaracesexpanded.archon", "VRE - Archon" },
            { "vanillaracesexpanded.insector", "VRE - Insector" },
            { "vanillaracesexpanded.saurid", "VRE - Saurid" },
            { "vanillaracesexpanded.genie", "VRE - Genie" },
            { "vanillaracesexpanded.sanguophage", "VRE - Sanguophage" },
            { "vanillaracesexpanded.hussar", "VRE - Hussar" },
            { "rimsenal.core", "Rimsenal - Core" },
            { "rimsenal.spacer", "Rimsenal - Spacer Faction Pack" },
            { "rimsenal.federation", "Rimsenal - Federation" },
            { "rimsenal.harana", "Rimsenal - Harana" },
            { "rimsenal.askbarn", "Rimsenal - Askbarn" },
            { "sarg.alphagenes", "Alpha Genes" },
            { "sarg.alphamemes", "Alpha Memes" },
            { "sarg.alphamechs", "Alpha Mechs" },
            { "lts.i", "Integrated Implants" },
            { "detvisor.impactweaponryreloaded", "Impact Weaponry - Reloaded" },
            { "det.spacerarsenal", "Spacer Arsenal" },
            { "det.keshig", "Det's Keshig" },
            { "det.brawnum", "Det's Brawnum" },
            { "det.stoneborn", "Det's Stoneborn" },
            { "det.boglegs", "Det's Boglegs" },
            { "elsov.highborn", "Highborn Xenotype" },
            { "vanillaquestsexpanded.ancients", "VQE - Ancients" },
            { "zal.eltexweaponry", "Eltex Weaponry" },
            { "vat.epoeforked", "EPOE-Forked" },
            { "resplice.xotr.core", "ReSplice: Core" },
            { "defi.generipper", "Gene Ripper" },
            { "danielwedemeyer.generipper", "Gene Ripper" },
            { "amch.eragon.hcgenefabrication", "Gene Fabrication" },
            { "owlchemist.cherrypicker", "Cherry Picker" },
        };

        public static bool Active(string packageId)
        {
            if (!activeCache.TryGetValue(packageId, out bool active))
            {
                active = ModsConfig.IsActive(packageId);
                activeCache[packageId] = active;
            }
            return active;
        }

        public static bool AllActive(string[] packageIds)
        {
            foreach (string id in packageIds)
                if (!Active(id))
                    return false;
            return true;
        }

        public static bool AnyActive(string[] packageIds)
        {
            if (packageIds.Length == 0)
                return true;
            foreach (string id in packageIds)
                if (Active(id))
                    return true;
            return false;
        }

        public static string NameOf(string packageId)
        {
            if (names.TryGetValue(packageId, out string name))
                return name;
            return ModLister.GetModWithIdentifier(packageId, true)?.Name ?? packageId;
        }

        public static string MissingNames(string[] packageIds)
        {
            StringBuilder sb = null;
            foreach (string id in packageIds)
            {
                if (Active(id))
                    continue;
                if (sb == null)
                    sb = new StringBuilder();
                else
                    sb.Append(", ");
                sb.Append(NameOf(id));
            }
            return sb?.ToString();
        }

        public static string AllNames(string[] packageIds)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < packageIds.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(NameOf(packageIds[i]));
            }
            return sb.ToString();
        }
    }
}
