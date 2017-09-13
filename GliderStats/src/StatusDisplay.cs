using KSP.UI.Screens.Flight;
using System;
using TMPro;
using UnityEngine;

namespace GliderStats
{
    /// <summary>
    /// Shows a continuously-updated status display next to the navball.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class StatusDisplay : MonoBehaviour
    {
        private static readonly TimeSpan UPDATE_INTERVAL = new TimeSpan(0, 0, 0, 0, 250);

        // the global instance of the object
        private static StatusDisplay instance = null;

        // for tracking whether we're initialized
        private bool isInitialized = false;
        private DateTime nextUpdate = DateTime.MinValue;

        // things that get set when we're initialized
        private NavBallBurnVector burnVector = null;
        private SafeText statusText = null;

        /// <summary>
        /// Here when the add-on loads upon flight start.
        /// </summary>
        private void Start()
        {
            instance = this;
            isInitialized = false;
            AttemptInitialize();
        }

        private void OnDestroy()
        {
            if (statusText != null)
            {
                statusText.Destroy();
                statusText = null;
            }
        }

        /// <summary>
        /// Sets the displayed string.
        /// </summary>
        public static string Value
        {
            set
            {
                if (instance == null) return;
                if (!instance.AttemptInitialize()) return;
                instance.statusText.Text = value;
            }
        }

        /// <summary>
        /// Gets whether the status display is initialized and usable.
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                return (instance != null) && instance.isInitialized;
            }
        }

        /// <summary>
        /// Gets or sets whether the alternate display text is enabled.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                if (instance == null) return false;
                if (!instance.AttemptInitialize()) return false;
                return instance.statusText.Enabled;
            }
            set
            {
                if (instance == null) return;
                if (!instance.AttemptInitialize()) return;
                instance.statusText.Enabled = value;
            }
        }

        /// <summary>
        /// Try to initialize the needed components. Returns true if initialized, false if not.
        /// If this function returns true, you're guaranteed that the needed GUI text objects
        /// are available and non-null.
        /// </summary>
        /// <returns></returns>
        private bool AttemptInitialize()
        {
            if (isInitialized) return true; // already initialized
            DateTime now = DateTime.Now;
            if (now < nextUpdate) return false; // too soon to try again
            nextUpdate = now + UPDATE_INTERVAL;

            // Try to get the navball's burn vector.  This check is needed because it turns
            // out that the timing of when this object becomes available isn't super reliable,
            // so various MonoBehaviour implementations in the mod can't just initialize at
            // Start() time and use it.
            NavBallBurnVector theBurnVector = GameObject.FindObjectOfType<NavBallBurnVector>();
            if (theBurnVector == null) return false; // nope, couldn't get it yet!

            // Make sure the burn vector components that we need are there
            if (theBurnVector.ebtText == null) return false;
            if (theBurnVector.TdnText == null) return false;

            TextMeshProUGUI theStatusText = CloneBehaviour(theBurnVector.TdnText);
            if (theStatusText == null) return false;

            theStatusText.enabled = false;
            theStatusText.transform.position = Interpolate(
                theBurnVector.TdnText.transform.position,
                theBurnVector.ebtText.transform.position,
                2.0F);

            // Got everything we need!
            burnVector = theBurnVector;
            statusText = SafeText.of(theStatusText);
            isInitialized = true;

            return true;
        }

        /// <summary>
        /// Clones a behaviour.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="initialText"></param>
        /// <returns></returns>
        private static T CloneBehaviour<T>(T source) where T : Behaviour
        {
            GameObject clonedObject = UnityEngine.Object.Instantiate(
                source.gameObject,
                source.transform.position,
                source.transform.rotation) as GameObject;

            // This line will cause Unity to log an error once, each time the mod
            // starts up. The error yells about how this is an invalid thing to do, and
            // that one should call SetParent on the transform instead of setting its
            // parent property to explicitly equal the other transform.  Like this:
            // clonedObject.transform.SetParent(source.gameObject.transform.parent, false);
            //
            // However, I'm not doing it that way, on purpose.
            //
            // Rationale: Following the error message's advice causes the error to go
            // away... but breaks the navball completely whenever the impact tracker
            // or closest-approach tracker is active. Bizarre stuff starts happening--
            // the navball sphere being drawn over top of the velocity display, text
            // markers not showing, weird occlusion problems for navball markers.
            //
            // So, the error message will just have to stay, unless someone knows a way
            // to get rid of it without hopelessly breaking the navball.
            clonedObject.transform.parent = source.gameObject.transform.parent;

            T clonedBehaviour = clonedObject.GetComponent<T>();

            clonedBehaviour.enabled = false;
            return clonedBehaviour;
        }

        /// <summary>
        /// Provide a vector in between two others.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="amount">In the range 0 to 1.  0 = function returns "from", 1 = function returns "to", 0.5 = function returns midpoint</param>
        /// <returns></returns>
        private static Vector3 Interpolate(Vector3 from, Vector3 to, float amount)
        {
            float remainder = 1.0F - amount;
            float x = (from.x * amount) + (to.x * remainder);
            float y = (from.y * amount) + (to.y * remainder);
            float z = (from.z * amount) + (to.z * remainder);
            return new Vector3(x, y, z);
        }
    }
}
