using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KCS
{
    public struct LifeSupportSettings
    {
        public bool enabled;
        // TODO: Add other settings
    };

    public struct HealthSystemSettings
    {
        public bool enabled;
        public String medicTrait;
        public double kerbalsPerMedic;
        public double baseHours;
        public double outOfLifeSupportDeclinePerHour;
        public double fullHealthCareIncreasePerHour;
    };

    public struct DisciplineSystemSettings
    {
        public bool enabled;
        public String officerTrait;
        public double kerbalsPerOfficer;
        public double baseHours;
        public double fullDisciplineIncreasePerHour;
    };

    public struct MoraleSystemSettings
    {
        public bool enabled;
        // TODO: Add other settings
    };

    public class Settings
    {
        public static LifeSupportSettings lifeSupport;
        public static HealthSystemSettings healthSystem;
        public static DisciplineSystemSettings disciplineSystem;
        public static MoraleSystemSettings MoraleSystem;

        public static void LoadSettings()
        {
            try
            {
                ConfigNode main = ConfigNode.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/settings.cfg");
                bool success = true;

                ConfigNode sub = main.GetNode("LifeSupport");
                success &= sub.TryGetValue("enabled", ref Settings.lifeSupport.enabled);
                // TODO: Read other Life Support values

                sub = main.GetNode("Health");
                success &= sub.TryGetValue("enabled", ref Settings.healthSystem.enabled);
                success &= sub.TryGetValue("medicTrait", ref Settings.healthSystem.medicTrait);
                success &= sub.TryGetValue("kerbalsPerMedic", ref Settings.healthSystem.kerbalsPerMedic);
                success &= sub.TryGetValue("baseHours", ref Settings.healthSystem.baseHours);
                success &= sub.TryGetValue("outOfLifeSupportDeclinePerHour", ref Settings.healthSystem.outOfLifeSupportDeclinePerHour);
                success &= sub.TryGetValue("fullHealthCareIncreasePerHour", ref Settings.healthSystem.fullHealthCareIncreasePerHour);

                sub = main.GetNode("Discipline");
                success &= sub.TryGetValue("enabled", ref Settings.disciplineSystem.enabled);
                success &= sub.TryGetValue("officerTrait", ref Settings.disciplineSystem.officerTrait);
                success &= sub.TryGetValue("kerbalsPerOfficer", ref Settings.disciplineSystem.kerbalsPerOfficer);
                success &= sub.TryGetValue("baseHours", ref Settings.disciplineSystem.baseHours);
                success &= sub.TryGetValue("fullDisciplineIncreasePerHour", ref Settings.disciplineSystem.fullDisciplineIncreasePerHour);

                // TODO: Add others Systems here ...

                if (!success) throw new Exception("missing or invalid settings attribute");
            }
            catch (Exception e)
            {
                Debug.LogError("[KCS] LoadSettings(): " + e.ToString());
            }
        }
    }
}
