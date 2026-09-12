using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BallStacks.EditorTools
{
    /// <summary>
    /// Wires the PlayerSystems prefab's seat InputActionReferences to the
    /// SeatLeft/SeatRight actions inside the central input asset. Those
    /// references are importer-generated sub-assets, so they cannot be
    /// authored by hand — this tool fills any empty slot automatically after
    /// a reload, and can be re-run from the menu if the wiring is ever lost.
    /// </summary>
    public static class PlayerSeatWiring
    {
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string PlayerSystemsPrefabPath = "Assets/Game/Player/Prefabs/PlayerSystems.prefab";

        [InitializeOnLoadMethod]
        private static void WireAfterReload()
        {
            EditorApplication.delayCall += () => Wire(verbose: false);
        }

        [MenuItem("Ball Stacks/Wire Player Seat Inputs")]
        private static void WireFromMenu()
        {
            Wire(verbose: true);
        }

        private static void Wire(bool verbose)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSystemsPrefabPath);
            bool prefabIsMissing = prefab == null || !prefab.TryGetComponent(out PlayerSeatSpawner spawner);
            if (prefabIsMissing)
            {
                if (verbose) { Debug.LogError($"PlayerSeatWiring: no PlayerSeatSpawner prefab at {PlayerSystemsPrefabPath}."); }
                return;
            }

            var serializedSpawner = new SerializedObject(prefab.GetComponent<PlayerSeatSpawner>());
            bool changedAnything = false;
            changedAnything |= WireInputAsset(serializedSpawner);
            changedAnything |= WireSeatAction(serializedSpawner, "_leftSeatMove", "SeatLeft", "Move");
            changedAnything |= WireSeatAction(serializedSpawner, "_leftSeatJump", "SeatLeft", "Jump");
            changedAnything |= WireSeatAction(serializedSpawner, "_rightSeatMove", "SeatRight", "Move");
            changedAnything |= WireSeatAction(serializedSpawner, "_rightSeatJump", "SeatRight", "Jump");

            if (!changedAnything)
            {
                if (verbose) { Debug.Log("PlayerSeatWiring: everything already wired."); }
                return;
            }

            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("PlayerSeatWiring: wired seat input references on PlayerSystems.prefab.");
        }

        private static bool WireInputAsset(SerializedObject serializedSpawner)
        {
            SerializedProperty property = serializedSpawner.FindProperty("_inputActions");
            if (property.objectReferenceValue != null) { return false; }

            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (asset == null)
            {
                Debug.LogError($"PlayerSeatWiring: no InputActionAsset at {InputActionsPath}.");
                return false;
            }

            property.objectReferenceValue = asset;
            return true;
        }

        private static bool WireSeatAction(SerializedObject serializedSpawner, string fieldName, string mapName, string actionName)
        {
            SerializedProperty property = serializedSpawner.FindProperty(fieldName);
            if (property.objectReferenceValue != null) { return false; }

            InputActionReference reference = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(candidate =>
                    candidate.action != null &&
                    candidate.action.actionMap.name == mapName &&
                    candidate.action.name == actionName);

            if (reference == null)
            {
                Debug.LogError($"PlayerSeatWiring: action {mapName}/{actionName} not found in {InputActionsPath}.");
                return false;
            }

            property.objectReferenceValue = reference;
            return true;
        }
    }
}
