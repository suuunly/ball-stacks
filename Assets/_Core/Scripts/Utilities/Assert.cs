using UnityEngine;

namespace Gaman
{
    public static class Assert
    {
        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void IsTrue(bool condition, string message = "Assertion failed.")
        {
            if (condition) { return; }

            Debug.LogError($"ASSERTION FAILED: {message}");
            Debug.Break(); // Pauses the editor immediately
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void IsNotNull(object obj, string message = "Value must not be null.")
        {
            IsTrue(obj != null, message);
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void IsNull(object obj, string message = "Value must be null.")
        {
            IsTrue(obj == null, message);
        }

        // REQUIRED: UnityEngine.Object needs its own overloads. Unity returns a "fake null"
        // wrapper for a missing component or a destroyed object — the managed reference is
        // NOT null, and only Unity's own == operator sees through it. The object-typed
        // versions above box the value, which bypasses that operator. Without these,
        // IsNull can never pass and IsNotNull can never fail for any Unity type.
        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void IsNotNull(UnityEngine.Object obj, string message = "Value must not be null.")
        {
            IsTrue(obj != null, message);
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void IsNull(UnityEngine.Object obj, string message = "Value must be null.")
        {
            IsTrue(obj == null, message);
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void AreEqual<T>(T expected, T actual, string message = "Values must be equal.")
        {
            IsTrue(
                expected != null && expected.Equals(actual),
                $"{message} Expected: {expected}, Actual: {actual}"
            );
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void IsInRange(float value, float min, float max, string message = "Value out of range.")
        {
            IsTrue(
                value >= min && value <= max,
                $"{message} Value: {value}, Range: [{min}, {max}]"
            );
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void IsEmpty<T>(System.Collections.Generic.ICollection<T> collection, string message = "Collection must be empty.")
        {
            IsTrue(collection != null && collection.Count == 0, message);
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        public static void IsNotEmpty<T>(System.Collections.Generic.ICollection<T> collection, string message = "Collection must not be empty.")
        {
            IsTrue(collection != null && collection.Count > 0, message);
        }
    }
}
