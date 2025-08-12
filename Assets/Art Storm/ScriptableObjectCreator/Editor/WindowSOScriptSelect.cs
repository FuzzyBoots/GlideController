using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace EA.ScriptableObjectCreator.Editor
{
    class WindowSOScriptSelect : EditorWindow
    {
        // ---------------- Static Access

        static readonly Type typeSO = typeof(ScriptableObject);
        static readonly Type typeEditorWindow = typeof(EditorWindow);
        static readonly List<string> sosAll = new();
        static Texture iconMono;

        public static void Call(string folderPath, Action<Type, string> onSelect)
        {
            iconMono = EditorGUIUtility.IconContent("cs Script Icon").image;

            FindAssets();

            var win = GetWindow<WindowSOScriptSelect>(true, "Select Scriptable Object Script");
            win.minSize = new Vector2(800, 480);

            win.folderPath = folderPath;
            win.onSelect = onSelect;

            win.Show();
            win.Search(true);
        }

        static void FindAssets()
        {
            if (sosAll.Count == 0)
            {
                var guids = AssetDatabase.FindAssets("t:MonoScript");

                for (int a = 0; a < guids.Length; a++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[a]);
                    var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    var type = asset.GetClass();

                    if (type == null) continue;
                    if (!type.IsSubclassOf(typeSO)) continue;
                    if (type.IsSubclassOf(typeEditorWindow)) continue;
                    if (type.IsAbstract) continue;
                    if (type.Namespace != null && type.Namespace.StartsWith("UnityEditor.")) continue;

                    sosAll.Add(path);
                }
            }
        }

        // ---------------- Private Fields

        Action<Type, string> onSelect;
        List<string> sosFiltered = new();
        string filterLast = string.Empty;
        string filter = string.Empty;
        string folderPath = string.Empty;
        string fileName = "ScriptableObjectAsset";
        bool wasControlFocused = false;
        const string focusControlOnEnable = "filterField";
        string badChars = "/?<>\\:*|\"";

        Vector2 scroll;

        // ---------------- Private Methods

        void OnGUI()
        {
            string assetPathSelected = null;
            string error = null;

            var alignment = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Filter By Path:", GUILayout.Width(150));
                GUI.SetNextControlName(focusControlOnEnable);
                filter = GUILayout.TextField(filter);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("New Asset File Name:", GUILayout.Width(150));
                fileName = EditorGUILayout.TextField(fileName);
                GUILayout.EndHorizontal();

                if (!wasControlFocused)
                {
                    wasControlFocused = true;
                    EditorGUI.FocusTextInControl(focusControlOnEnable);
                }

                Search();

                error = Validate();

                if (string.IsNullOrEmpty(error))
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.HelpBox($"New asset will be created at path: {folderPath}/{fileName}.asset", MessageType.Info, true);
                    EditorGUILayout.Space(2);
                }
                else

                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.HelpBox(error, MessageType.Error, true);
                    EditorGUILayout.Space(2);
                }
            }
            GUI.skin.label.alignment = alignment;

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            {
                GUI.skin.button.alignment = TextAnchor.MiddleLeft;

                for (int a = 0; a < sosFiltered.Count; a++)
                {
                    var path = sosFiltered[a];

                    if (GUILayout.Button(new GUIContent(path, iconMono), GUILayout.Width(position.width - 20), GUILayout.Height(20)))
                        assetPathSelected = path;
                }

                GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            }
            EditorGUILayout.EndScrollView();

            if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(assetPathSelected))
            {
                Close();
                var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPathSelected);
                onSelect?.Invoke(mono.GetClass(), fileName.Trim());
            }
        }

        void Search(bool force = false)
        {
            string search = string.Empty;

            if (force || !string.Equals(filterLast, filter))
            {
                filterLast = filter;
                sosFiltered.Clear();

                if (string.IsNullOrEmpty(filter))
                    sosFiltered.AddRange(sosAll);
                else
                    for (int a = 0; a < sosAll.Count; a++)
                    {
                        string path = sosAll[a];
                        if (path.Contains(filter, StringComparison.OrdinalIgnoreCase))
                            sosFiltered.Add(path);
                    }
            }
        }

        string Validate()
        {
            if (string.IsNullOrEmpty(fileName.Trim()))
                return "File name needs at least one character.";

            bool containsBadChars = false;
            for (int a = 0; a < badChars.Length; a++)
                if (fileName.Contains(badChars[a]))
                {
                    containsBadChars = true;
                    break;
                }

            if (containsBadChars)
                return $"A file name can't contain any of the following characters: {badChars}";

            string fpath = $"{folderPath}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<UObject>(fpath) != null)
                return "Asset already exist at path: " + fpath;

            return null;
        }
    }
}