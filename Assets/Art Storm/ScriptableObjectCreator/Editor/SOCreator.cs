using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace EA.ScriptableObjectCreator.Editor
{
    static class SOCreator
    {
        [MenuItem("Assets/Create Scriptable Object From...", validate = true)]
        static bool CreateSOFromValid()
        {
            if (Selection.activeObject == null) return false;

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (path.Equals("Assets")) return true;

            if (string.IsNullOrEmpty(path)) return false;
            if (!path.StartsWith("Assets/")) return false;

            return true;
        }

        [MenuItem("Assets/Create Scriptable Object From...")]
        static void CreateSOFrom()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (!AssetDatabase.IsValidFolder(path))
                path = Path.GetDirectoryName(path);

            WindowSOScriptSelect.Call(path, (type, fname) =>
            {
                string fpath = $"{path}/{fname}.asset";
                CreateScriptableObjectAsset(type, fpath);
            });
        }

        static void CreateScriptableObjectAsset(Type type, string filePath)
        {
            var instance = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(instance, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = instance;

            //Debug.LogFormat("ScriptableObject created at path: {0}", filePath);
        }
    }
}