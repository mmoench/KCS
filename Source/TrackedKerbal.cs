using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KCS
{
    /**
     * We use this class to store all the tracked data about individual Kerbals which are part of currently active crews.
     **/
    public class TrackedKerbal
    {
        public String name = null;          // The gobally unique name of the tracked kerbal
        public String reasonForDeath = "no reason"; // Helper-variable for logging

        // Current stats [0..100%]:
        public double health = 1;
        public double discipline = 1;
        public double morale = 1;

        // UTC when the kerbal was last updated:
        public double lastUpdate = 0;

        // Cached deltas per hour:
        public double deltaPerHourHealth = 0;
        public double deltaPerHourDiscipline = 0;
        public double deltaPerHourMorale = 0;
        

        // Used for loading games.
        public static TrackedKerbal CreateFromConfigNode(ConfigNode node)
        {
            TrackedKerbal trackedKerbal = new TrackedKerbal();
            // TODO: Set the attributes
            return trackedKerbal;
        }

        // Used for saving games.
        public ConfigNode SaveToNode()
        {
            ConfigNode node = new ConfigNode();
            // TODO: Export all attributes
            return node;
        }

        // This function will recalculate the delta values for the various stats by looking at the underlying metrics
        // (like number of crew-members, available livingspace, etc). For performance reasons this should not be done
        // every tick, but instead only if somethin happens which could affect these unerlying factors and ideally
        // only after the vessel itself was updated.
        public void UpdateDeltas(TrackedVessel vessel)
        {
            deltaPerHourHealth = 0;
            deltaPerHourDiscipline = 0;
            deltaPerHourMorale = 0;

            if (vessel.cachedCrewCount <= 0) throw new Exception("kerbal '" +  name + "' on board of a vessel without any crew");

            // Health-System //////////////////////////////////////////////////////////////////////////////////////////
            if (Settings.healthSystem.enabled)
            {
                // If there are no more Supplies, the Kerbal is starving:
                if (Settings.lifeSupport.enabled && vessel.cachedLifeSupport <= 0) deltaPerHourHealth -= Settings.healthSystem.outOfLifeSupportDeclinePerHour;

                double hoursOfHealth = Settings.healthSystem.baseHours / (1 - (vessel.cachedMedicCount * Settings.healthSystem.kerbalsPerMedic) / vessel.cachedCrewCount);
                if (hoursOfHealth < 0)
                {
                    // More medics than necessary, increase health each hour:
                    deltaPerHourHealth += Settings.healthSystem.fullHealthCareIncreasePerHour;
                } else {
                    // Decrease Kerbals health by this delta each hour, so that he hits 0% after "hoursOfHealth" hours:
                    deltaPerHourHealth -= 1 / hoursOfHealth;
                }
            }

            // Discipline-System //////////////////////////////////////////////////////////////////////////////////////
            if (Settings.disciplineSystem.enabled)
            {
                double hoursOfDiscipline = Settings.disciplineSystem.baseHours / (1 - (vessel.cachedOfficerCount * Settings.disciplineSystem.kerbalsPerOfficer) / vessel.cachedCrewCount);
                if (hoursOfDiscipline < 0)
                {
                    // More Officers than necessary, raise the discipline:
                    deltaPerHourDiscipline += Settings.disciplineSystem.fullDisciplineIncreasePerHour;
                } else {
                    // Decrease Kerbals discipline by this delta each hour, so that he hits 0% after "hoursOfDiscipline" hours:
                    deltaPerHourDiscipline -= 1 / hoursOfDiscipline;
                }
            }

            // Morale-System //////////////////////////////////////////////////////////////////////////////////////////
            if (Settings.MoraleSystem.enabled)
            {
                // TODO: Implement this ...
            }
        }

        // Returns true if this kerbal should die. The helper-variable "reasonForDeath" will contain a valid reason for logging.
        public bool ShouldDie()
        {
            if (health > 0 && discipline > 0 && morale > 0) return false;
            List<String> reasons = new List<String>();
            if (health <= 0) reasons.Add("low health");
            if (discipline <= 0) reasons.Add("low discipline");
            if (morale <= 0) reasons.Add("low morale");
            reasonForDeath = reasons[0];
            for (int i = 1; i < reasons.Count; i++) reasonForDeath += ", " + reasons[i];
            return true;
        }

        // Updates all tracked attributes of this kerbal, using the cached deltas.
        public void Update()
        {
            try
            {
                double currentUTC = Planetarium.GetUniversalTime();
                double timeDeltaSeconds = currentUTC - lastUpdate;
                if (timeDeltaSeconds <= 0) return;

                health += deltaPerHourHealth / (60 * 60) * timeDeltaSeconds;
                if (health < 0) health = 0;
                if (health > 1) health = 1;

                discipline += deltaPerHourDiscipline / (60 * 60) * timeDeltaSeconds;
                if (discipline < 0) discipline = 0;
                if (discipline > 1) discipline = 1;

                morale += deltaPerHourMorale / (60 * 60) * timeDeltaSeconds;
                if (morale < 0) morale = 0;
                if (morale > 1) morale = 1;

                lastUpdate = currentUTC;
            }
            catch (Exception e)
            {
                Debug.LogError("[KCS] TrackedKerbal.Update(" + name.ToString() + "): " + e.ToString());
            }
        }
    }
}
