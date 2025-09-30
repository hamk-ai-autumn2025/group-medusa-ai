#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using dev.susybaka.TurnBasedGame.Battle.Data;
using dev.susybaka.TurnBasedGame.Items;

namespace dev.susybaka.TurnBasedGame.Editor 
{
    [CustomEditor(typeof(ItemData))]
    public class ItemDataEditor : UnityEditor.Editor
    {
        SerializedProperty p_type, p_displayName, p_description, p_stackable, p_maxStack, p_value;
        SerializedProperty p_canBeUsed, p_canBeEquipped, p_needsTarget, p_targetGroup, p_consumeOnUse, p_useAbility;

        void OnEnable()
        {
            p_type = serializedObject.FindProperty("type");
            p_displayName = serializedObject.FindProperty("displayName");
            p_description = serializedObject.FindProperty("description");
            p_stackable = serializedObject.FindProperty("stackable");
            p_maxStack = serializedObject.FindProperty("maxStack");
            p_value = serializedObject.FindProperty("value");

            p_canBeUsed = serializedObject.FindProperty("canBeUsed");
            p_canBeEquipped = serializedObject.FindProperty("canBeEquipped");
            p_needsTarget = serializedObject.FindProperty("needsTarget");
            p_targetGroup = serializedObject.FindProperty("targetGroup");
            p_consumeOnUse = serializedObject.FindProperty("consumeOnUse");
            p_useAbility = serializedObject.FindProperty("useAbility");

            ItemAuthoringUtility.EditorUpdateHook();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // --- Data ---
            EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p_type);
            EditorGUILayout.PropertyField(p_displayName);
            EditorGUILayout.PropertyField(p_description);
            EditorGUILayout.PropertyField(p_stackable);
            using (new EditorGUI.DisabledScope(!p_stackable.boolValue))
                EditorGUILayout.PropertyField(p_maxStack);
            EditorGUILayout.PropertyField(p_value);

            GUILayout.Space(8);

            // --- Logic ---
            EditorGUILayout.LabelField("Logic", EditorStyles.boldLabel);
            bool prevCanBeUsed = p_canBeUsed.boolValue;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(p_canBeUsed);
            bool canBeUsedChanged = EditorGUI.EndChangeCheck();

            // If toggled OFF -> clean + reset
            if (canBeUsedChanged && prevCanBeUsed && !p_canBeUsed.boolValue)
            {
                // reset values
                p_needsTarget.boolValue = true; // Reset back to true
                p_consumeOnUse.intValue = 1; // Reset back to 1
                p_targetGroup.enumValueIndex = 3; // Reset back to enemy

                serializedObject.ApplyModifiedProperties(); // apply before cleaning assets
                ItemAuthoringUtility.CleanUseAbility((ItemData)target);
                serializedObject.Update(); // refresh after external changes
            }
            else if (canBeUsedChanged && !prevCanBeUsed && p_canBeUsed.boolValue)
            {
                // Just enabled - generate new sub-assets
                serializedObject.ApplyModifiedProperties();
                ItemAuthoringUtility.GenerateOrUpdateAbility((ItemData)target);
                serializedObject.Update();
            }

            if (p_canBeUsed.boolValue)
            {
                EditorGUILayout.PropertyField(p_canBeEquipped);
                EditorGUILayout.PropertyField(p_needsTarget);
                EditorGUILayout.PropertyField(p_targetGroup);
                EditorGUILayout.PropertyField(p_consumeOnUse);

                // Show ref (read-only)
                if (p_useAbility.objectReferenceValue != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField("Use Ability", p_useAbility.objectReferenceValue, typeof(AbilityData), false);

                    if (GUILayout.Button("Open", GUILayout.Width(60)))
                    {
                        Selection.activeObject = p_useAbility.objectReferenceValue;
                        EditorGUIUtility.PingObject(p_useAbility.objectReferenceValue);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(p_useAbility);
                }

                GUILayout.Space(6);
                if (p_useAbility.objectReferenceValue == null)
                {
                    if (GUILayout.Button("Generate Use Ability"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        ItemAuthoringUtility.GenerateOrUpdateAbility((ItemData)target);
                        serializedObject.Update();
                    }
                }
                else
                {
                    if (GUILayout.Button("Update Use Ability"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        ItemAuthoringUtility.GenerateOrUpdateAbility((ItemData)target);
                        serializedObject.Update();
                    }
                }
            }
            else
            {
                // Hide all fields below; also show explicit cleaner if something remains (edge cases)
                if (p_useAbility.objectReferenceValue != null)
                {
                    EditorGUILayout.HelpBox("Use data exists but canBeUsed is OFF. It will be cleaned automatically. You can also force-clean now.", MessageType.Info);
                    if (GUILayout.Button("Force Clean Use Ability & Sub-assets"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        ItemAuthoringUtility.CleanUseAbility((ItemData)target);
                        serializedObject.Update();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    public static class ItemAuthoringUtility
    {
        // Avoid heavy work every domain reload
        private static bool _hooked = false;
        public static void EditorUpdateHook()
        {
            if (_hooked)
                return;
            _hooked = true;
            EditorApplication.projectChanged += OnProjectChanged;
        }
        private static void OnProjectChanged() { /* optional batch fix */ }

        public static void GenerateOrUpdateAbility(ItemData item)
        {
            if (!item)
                return;
            var itemPath = AssetDatabase.GetAssetPath(item);
            if (string.IsNullOrEmpty(itemPath))
            {
                Debug.LogError("Save the ItemData asset to disk before generating.");
                return;
            }

            // 1) Ensure / Create ability as sub-asset
            if (item.useAbility == null)
            {
                var ability = ScriptableObject.CreateInstance<AbilityData>();
                ability.name = $"Use{item.name}";
                AssetDatabase.AddObjectToAsset(ability, itemPath);
                AssetDatabase.ImportAsset(itemPath);
                item.useAbility = ability;
                EditorUtility.SetDirty(item);
            }

            // 2) Mirror authoring values onto ability
            item.useAbility.displayName = string.IsNullOrEmpty(item.displayName) ? $"Use {item.name}" : item.displayName;
            item.useAbility.description = string.IsNullOrEmpty(item.description) ? $"Use {item.name}" : item.description;
            item.useAbility.requiresTarget = item.needsTarget;
            item.useAbility.targetGroup = item.targetGroup;

            // 3) Ensure HasItemCondition & ConsumeItemEffect (optional)
            var hasItem = FindOrCreateSub<HasItemCondition>(item.useAbility, $"Has{item.name}");
            hasItem.item = item;
            hasItem.amount = Mathf.Max(1, item.consumeOnUse);

            ConsumeItemEffect consume = null;
            if (item.consumeOnUse > 0)
            {
                consume = FindOrCreateSub<ConsumeItemEffect>(item.useAbility, $"Consume{item.name}");
                consume.item = item;
                consume.amount = Mathf.Max(1, item.consumeOnUse);
            }

            // 4) Keep lists in correct order: HasItem (for THIS item) FIRST, Consume (for THIS item) LAST
            var abilitySO = new SerializedObject(item.useAbility);
            var condProp = abilitySO.FindProperty("conditions");
            var effProp = abilitySO.FindProperty("effects");

            EnsureFirstForThisItem(condProp, hasItem, item); // pins only this item's condition
            if (consume != null)
                EnsureLastForThisItem(effProp, consume, item); // pins only this item's consume
            else
                RemoveFromListIf(effProp, o => o is ConsumeItemEffect ce && ce.item == item); // remove only this item's consume

            abilitySO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item.useAbility);
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
        }

        public static void CleanUseAbility(ItemData item)
        {
            if (!item)
                return;

            var ability = item.useAbility;
            if (ability == null)
                return;

            Undo.RecordObject(item, "Clean Use Ability");
            var assetPath = AssetDatabase.GetAssetPath(ability);
            if (string.IsNullOrEmpty(assetPath))
                assetPath = AssetDatabase.GetAssetPath(item);

            // Remove references from lists (so we don't leave dangling refs)
            var abilitySO = new SerializedObject(ability);
            var condProp = abilitySO.FindProperty("conditions");
            var effProp = abilitySO.FindProperty("effects");

            RemoveFromList(condProp, typeof(HasItemCondition));
            RemoveFromList(effProp, typeof(ConsumeItemEffect));
            abilitySO.ApplyModifiedPropertiesWithoutUndo();

            // Destroy sub-assets that belong to this ability (and are of the expected types)
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (sub is HasItemCondition c && c.item == item)
                    Object.DestroyImmediate(sub, true);
                if (sub is ConsumeItemEffect e && e.item == item)
                    Object.DestroyImmediate(sub, true);
            }

            // Finally destroy the ability sub-asset itself
            Object.DestroyImmediate(ability, true);
            item.useAbility = null;

            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);
        }

        private static T FindOrCreateSub<T>(ScriptableObject parent, string name) where T : ScriptableObject
        {
            var path = AssetDatabase.GetAssetPath(parent);
            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
                if (sub is T t)
                    return t;

            var created = ScriptableObject.CreateInstance<T>();
            created.name = name;
            AssetDatabase.AddObjectToAsset(created, path);
            AssetDatabase.ImportAsset(path);
            return created;
        }

        private static void EnsureFirstForThisItem(SerializedProperty list, ScriptableObject obj, ItemData item)
        {
            RemoveFromListIf(list, o => o is HasItemCondition hc && hc.item == item);
            InsertAt(list, 0, obj);
        }

        private static void EnsureLastForThisItem(SerializedProperty list, ScriptableObject obj, ItemData item)
        {
            RemoveFromListIf(list, o => o is ConsumeItemEffect ce && ce.item == item);
            Append(list, obj);
        }

        private static void RemoveFromListIf(SerializedProperty list, System.Predicate<Object> match)
        {
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                var el = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (el != null && match(el))
                    list.DeleteArrayElementAtIndex(i);
            }
        }

        private static void InsertAt(SerializedProperty list, int index, Object obj)
        {
            int oldSize = list.arraySize;
            list.arraySize = oldSize + 1;
            for (int i = oldSize; i > index; i--)
                list.GetArrayElementAtIndex(i).objectReferenceValue =
                    list.GetArrayElementAtIndex(i - 1).objectReferenceValue;

            list.GetArrayElementAtIndex(index).objectReferenceValue = obj;
        }

        private static void Append(SerializedProperty list, Object obj)
        {
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = obj;
        }

        private static void RemoveFromList(SerializedProperty list, System.Type type)
        {
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                var el = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (el != null && type.IsInstanceOfType(el))
                    list.DeleteArrayElementAtIndex(i);
            }
        }
    }
}
#endif