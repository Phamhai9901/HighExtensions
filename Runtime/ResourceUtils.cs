using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace High
{
    public static class ResourceUtils
    {
#if UNITY_EDITOR
        public static List<T> LoadAllAssetsAtPath<T>(string path, string format = "*.asset") where T : Object
        {
            if (!Directory.Exists(path))
                return null;

            List<T> list = new List<T>();

            List<string> allFile = new List<string>(Directory.EnumerateFiles(path, format));
            for (int i = 0; i < allFile.Count; i++)
            {
                list.Add(AssetDatabase.LoadAssetAtPath<T>(allFile[i]));
            }

            List<string> allFolder = new List<string>(Directory.EnumerateDirectories(path));
            for (int i = 0; i < allFolder.Count; i++)
            {
                list.AddRange(LoadAllAssetsAtPath<T>(allFolder[i]));
            }

            return list;
        }
        public static T LoadAssetsAtPath<T>(string path, string fileName, string format = "*.asset") where T : Object
        {
            if (!Directory.Exists(path))
                return null;

            string fullPath = Path.Combine(path, fileName + format);
            fullPath = fullPath.Replace("\\", "/"); // Unity cần dấu /

            if (!File.Exists(fullPath))
                return null;

            return AssetDatabase.LoadAssetAtPath<T>(fullPath);
        }
    }
#endif
}