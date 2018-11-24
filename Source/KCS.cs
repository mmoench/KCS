using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP;
using KSP.UI.Screens;

namespace KCS
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class KCS : UnityEngine.MonoBehaviour
    {
        static bool initialized = false;

        // A list of all vessels this mod should keep track of (flags, asteroids, etc are filtered out):
        public static List<TrackedVessel> trackedVessels = null;

        // A list of the individual Kerbal's stats who are on the tracked vessels and we want to keep track of,
        // using the key as the kerbals unique name for faster lookups:
        public static Dictionary<String, TrackedKerbal> trackedKerbals = null;

        /**
         * KSP has some kind of secret backup-list in which they store which kerbal had which status and was sitting in which
         * seat of a craft that is currently not active (or I haven't found the right setting yet). When we kill a kerbal,
         * he will sometimes get resurected by KSP with the following message before saving the game:
         * 
         * Crewmember X Kerman found inside a part but status is set as missing. Vessel must have failed to save earlier. Restoring assigned status.
         * 
         * To circumvent this we keep our own list and re-kill any kerbal that comes back to live. Not pretty, but when you
         * actually quit the game after we've killed a kerbel and then start again from the last save, the kerbal will remain daed,
         * so we probably already do all we can do and KSP is just messing things up for us with some inaccesible function ...
         **/
        public static List<string> killList = null;

        /**
         * Whenever something happens which can change the state of the (game-) world (eg an event is triggered, a save is loaded),
         * we set this variable to signal to the timer-function to update all vessels during the next run. We do this in lieu of
         * calling the update function directly to avoid spamming updated (one event comes seldom alone) and because the time handles
         * the delayed call of the updates in case the game isn't properly initialized yet.
         **/
        public static bool forceGlobalUpdate = false;

        // Install handler for continous updates:
        public void Awake()
        {
            if (initialized) return;

            DontDestroyOnLoad(this);
            if (!IsInvoking("Timer")) InvokeRepeating("Timer", 1, 1); // once every second.
            if (trackedVessels == null) trackedVessels = new List<TrackedVessel>();
            if (trackedKerbals == null) trackedKerbals = new Dictionary<string, TrackedKerbal>();
            if (killList == null) killList = new List<string>();
            Settings.LoadSettings();

            // Whenever something relevant happens, we want to update our tracked vessels:
            GameEvents.onVesselCreate.Add(this.OnVesselUpdate);
            GameEvents.onNewVesselCreated.Add(this.OnVesselUpdate);
            GameEvents.onVesselChange.Add(this.OnVesselUpdate);
            GameEvents.onVesselCrewWasModified.Add(this.OnVesselUpdate);
            GameEvents.onVesselPartCountChanged.Add(this.OnVesselUpdate);
            GameEvents.onVesselWasModified.Add(this.OnVesselUpdate);
            GameEvents.onVesselDestroy.Add(this.OnVesselUpdate);
            GameEvents.onVesselRecovered.Add(this.OnVesselRecovered);
            GameEvents.onVesselTerminated.Add(this.OnVesselTerminated);

            initialized = true;
        }

        private void OnVesselTerminated(ProtoVessel data)
        {
            forceGlobalUpdate = true;
        }

        private void OnVesselRecovered(ProtoVessel data0, bool data1)
        {
            forceGlobalUpdate = true;
        }

        private void OnVesselUpdate(Vessel data)
        {
            forceGlobalUpdate = true;
        }

        public static int GetDayLength()
        {
            int dayLength = 24;
            if (GameSettings.KERBIN_TIME) dayLength = 6;
            return dayLength;
        }

        public static TrackedVessel GetTrackedVessel(Vessel vessel)
        {
            foreach (TrackedVessel trackedVessel in trackedVessels) if (trackedVessel.vessel == vessel) return trackedVessel;
            // Maybe the vessel was just created, check and try again:
            UpdateAllTrackedVessels();
            foreach (TrackedVessel trackedVessel in trackedVessels) if (trackedVessel.vessel == vessel) return trackedVessel;
            return null;
        }

        // Updates the list of tracked vessels as well as all the vessels on it (updates the resources in the vessels,
        // updates the crew's stats and kills them if necessary):
        public static void UpdateAllTrackedVessels()
        {
            try
            {
                long time = DateTime.Now.Ticks;
                int trackingsAdded = 0;
                int trackingsRemoved = 0;

                // This should only happen when the game is still loading, at least there should be some asteroids:
                if (FlightGlobals.Vessels.Count == 0)
                {
                    trackedVessels.Clear();
                    return;
                }

                // Find new vessels, which are not yet tracked and track them:
                List<Guid> trackedIds = new List<Guid>();
                foreach (TrackedVessel trackedVessel in trackedVessels) trackedIds.Add(trackedVessel.vessel.id);
                List<Vessel> missingVessels = FlightGlobals.Vessels.FindAll(x => !trackedIds.Contains(x.id));
                foreach (Vessel vessel in missingVessels)
                {
                    if (vessel.vesselType == VesselType.Flag || vessel.vesselType == VesselType.SpaceObject || vessel.vesselType == VesselType.Unknown) continue;
                    trackedVessels.Add(TrackedVessel.CreateFromVessel(vessel));
                    trackingsAdded++;
                }

                // Find vessels, which were removed:
                List<Guid> existingIds = new List<Guid>();
                foreach (Vessel vessel in FlightGlobals.Vessels) existingIds.Add(vessel.id);
                List<TrackedVessel> trackedVesselsToRemove = trackedVessels.FindAll(x => x?.vessel?.id == null || !existingIds.Contains(x.vessel.id));
                foreach (TrackedVessel trackedVessel in trackedVesselsToRemove)
                {
                    trackedVessels.Remove(trackedVessel);
                    trackingsRemoved++;
                }

                // Update all the tracked vessels:
                foreach (TrackedVessel trackedVessel in trackedVessels) trackedVessel.Update();

                // Update list of tracked Crews:
                Dictionary<String, TrackedVessel> kerbalsVessel = new Dictionary<string, TrackedVessel>();
                foreach (TrackedVessel trackedVessel in trackedVessels)
                {
                    // TODO: Update Kerbal-List ...
                    // TODO: Fill the Dictionary ...
                }

                // Update the cached Kerbal-Stats:
                foreach (TrackedKerbal trackedKerbal in trackedKerbals.Values)
                {
                    if (!kerbalsVessel.ContainsKey(trackedKerbal.name)) throw new Exception("tracked-kerbal '" + trackedKerbal.name.ToString() + "' without tracked-vessel");
                    trackedKerbal.Update(); // Calculate the Kerbal's stats up until now
                    trackedKerbal.UpdateDeltas(kerbalsVessel[trackedKerbal.name]); // Generate new cached stats
                }

                time = (DateTime.Now.Ticks - time) / TimeSpan.TicksPerSecond;
                Debug.Log("[KCS] tracked " + trackingsAdded.ToString() + " new, removed " + trackingsRemoved.ToString() + " old and updated " + trackedVessels.Count.ToString() + " vessels in " + time.ToString("0.000s"));
            }
            catch (Exception e)
            {
                Debug.LogError("[KCS] UpdateAllTrackedVessels(): " + e.ToString());
            }
        }

        // Called reguarly to check for vessels which have run out of life-support etc:
        public void Timer()
        {
            try
            {
                // Don't update in main menu:
                if (HighLogic.LoadedScene == GameScenes.MAINMENU || HighLogic.LoadedScene == GameScenes.CREDITS || HighLogic.LoadedScene == GameScenes.SETTINGS) return;

                // For reasons unknown this mod loads before all assets are properly initialized, so we have to wait a little bit:
                if (FlightGlobals.Vessels.Count == 0 || !ApplicationLauncher.Ready) return;

                // This should only happen when the game was just loaded or a vessel was modified externaly:
                if (forceGlobalUpdate || (FlightGlobals.Vessels.Count > 0 && trackedVessels.Count == 0))
                {
                    UpdateAllTrackedVessels();
                    forceGlobalUpdate = false;
                }

                // Update the life-support of the current vessel each tick (necessary because we can actually see the ressources when clicking on
                // parts and looking at the resource monitor:
                foreach (TrackedVessel trackedVessel in trackedVessels)
                {
                    if (trackedVessel.vessel.isActiveVessel) trackedVessel.Update();
                }

                // Update the stats of all Kerbals (are based on their cached values):
                List<TrackedKerbal> kerbalsToKill = new List<TrackedKerbal>();
                foreach (TrackedKerbal trackedKerbal in trackedKerbals.Values)
                {
                    trackedKerbal.Update();
                    if (trackedKerbal.ShouldDie()) kerbalsToKill.Add(trackedKerbal);
                }

                // Maybe kill kerbals:
                foreach (TrackedKerbal kerbalToKill in kerbalsToKill)
                {
                    // TODO: Kill the kerbal via the vessel ...
                    trackedKerbals.Remove(kerbalToKill.name);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KCS] Timer(): " + e.ToString());
            }
        }
    }

    // This class handels load- and save-operations.
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    class KCSScenarioModule : ScenarioModule
    {
        public override void OnSave(ConfigNode node)
        {
            try
            {
                // Check for zombies:
                if (KCS.killList.Count > 0)
                {
                    foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew))
                    {
                        if (KCS.killList.Contains(kerbal.name) && kerbal.rosterStatus != ProtoCrewMember.RosterStatus.Dead)
                        {
                            Debug.Log("[KCS] killing zombie " + kerbal.name);
                            kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                        }
                    }
                }

                // Update all vessels before their stats are made persistant. This way we don't have to store our
                // tracking-list with all the meta-information about the vessels:
                KCS.UpdateAllTrackedVessels();

                // TODO: Update all tracked kerbals before persisiting
                // TODO: Persist Kerbal-List

                // Save the kill-list so that a re-load of a save in the same session (or a switching of the game-scene,
                // which works by saving and then loading) causes dead kerbals to become alive again:
                foreach (string kerbalName in KCS.killList)
                {
                    node.AddValue("kill_list", kerbalName);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KCS] OnSave(): " + e.ToString());
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                // Retrieve our kill-list:
                if (KCS.killList == null) KCS.killList = new List<string>();
                KCS.killList.Clear();
                foreach (string deadName in node.GetValues("kill_list"))
                {
                    KCS.killList.Add(deadName);
                }

                // TODO: Load Tracked Kerbals ...

                // Update and rebuild all tracked vessels as soon as possible:
                if (KCS.trackedVessels == null) KCS.trackedVessels = new List<TrackedVessel>();
                KCS.trackedVessels.Clear();
                KCS.forceGlobalUpdate = true;
            }
            catch (Exception e)
            {
                Debug.LogError("[KCS] OnLoad(): " + e.ToString());
            }
        }
    }
}
