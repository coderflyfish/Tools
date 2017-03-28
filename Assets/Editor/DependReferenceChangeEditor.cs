using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;
using System.Collections;
using Object = UnityEngine.Object;

public class DependReferenceChangeEditor
{
    private const string SearchPath = "ZZZ";
    [MenuItem("Assets/应用修改到全部依赖资源")]
    [MenuItem("UI/应用修改到全部依赖资源")]
    public static void OprDependReferncePrefabs()
    {
        List<string> needReplaceTags =new List<string>();
        GameObject[] objs = Selection.gameObjects;
        for (int i = 0; i < objs.Length; i++)
        {
            needReplaceTags.Add(objs[i].name);
        }
        Dictionary<GameObject, Dictionary<string,List<string>>> replaceObjs = searchGameObjects(needReplaceTags);

        OprDependReferncePrefab(objs, replaceObjs);
    }

    public static string[] GetPathsByDirectoryAndTail(string directory, string searchPattern)
    {
        string[] paths = Directory.GetFiles(Application.dataPath + "/" + directory,
            searchPattern, SearchOption.AllDirectories);
        for (var i = 0; i < paths.Length; i++)
        {
            paths[i] = paths[i].Replace(Application.dataPath, "Assets");
            paths[i] = paths[i].Replace("\\", "/");
        }
        return paths;
    }

    private static Dictionary<GameObject, Dictionary<string, List<string>>> searchGameObjects(List<string> tagList)
    {
        Dictionary<GameObject, Dictionary<string, List<string>>> results = new Dictionary<GameObject, Dictionary<string, List<string>>>();
        string[] paths = GetPathsByDirectoryAndTail(SearchPath,"*.prefab");
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (obj != null)
            {
                Transform[] childs = obj.GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < childs.Length; j++)
                {
                    string tag = childs[j].gameObject.tag;
                    if (tagList.Contains(tag))
                    {
                        if (!results.ContainsKey(obj))
                        {
                            results[obj] = new Dictionary<string, List<string>>();
                        }
                        if (!results[obj].ContainsKey(tag))
                        {
                            results[obj][tag] = new List<string>();
                        }
                        results[obj][tag].Add(childs[j].gameObject.name);
                    }
                }
                
            }
        }
        return results;
    }
    private static void OprDependReferncePrefab(GameObject[] objs, Dictionary<GameObject, Dictionary<string, List<string>>> replaceObjs)
    {
        if(replaceObjs==null||replaceObjs.Count==0)
            return ;
       
        for (int i = 0; i < objs.Length; i++)
        {
            List<GameObject> changeObjs =new List<GameObject>();
            string searchTag = objs[i].name;
            foreach (var kv in replaceObjs)
            {
                if (kv.Value.ContainsKey(searchTag))
                {
                    changeObjs.Add(kv.Key);
                }
            }
            for (int j = 0; j < changeObjs.Count; j++)
            {
                GameObject replaceObj = changeObjs[j];
                GameObject target = (GameObject)PrefabUtility.InstantiatePrefab(replaceObj);
                bool change = false;

               
                string logStr = "使用 ";
                List<string> childs = replaceObjs[replaceObj][searchTag];
                for (int k = 0; k < childs.Count; k++)
                {

                    Transform childGo = target.transform.FindChild(childs[k]);
                    if (childGo!=null)
                    {
                        change = true;
                        GameObject source = (GameObject)PrefabUtility.InstantiatePrefab(objs[i]);
                        if (childGo != target.transform)
                        {
                            logStr += source.name + "替换了 " + target.name +
                                    "的" + childGo.name;
                            ReplaceChangePart(source.transform, childGo);
                           
                            Debug.Log(logStr);
                        }
                    }
                }

                if (change)
                {
                   
                    var catche = replaceObjs[replaceObj];
                    replaceObjs.Remove(replaceObj);
                    replaceObj = PrefabUtility.ReplacePrefab(target, replaceObj);
                    replaceObjs.Add(replaceObj, catche);
                }
                AssetDatabase.SaveAssets();
                GameObject.DestroyImmediate(target);  
            }
        }
       
    }

    private static void ReplaceChangePart(Transform source, Transform target)
    {
        PrefabUtility.DisconnectPrefabInstance(PrefabUtility.FindPrefabRoot(source.gameObject));
        source.parent = target.parent;
        source.localPosition = target.localPosition;
        source.localRotation = target.localRotation;
        source.localScale = target.localScale;
        source.gameObject.name = target.gameObject.name;
        source.tag = target.tag;
        source.SetSiblingIndex(target.GetSiblingIndex());
        GameObject.DestroyImmediate(target.gameObject);
    }
}
