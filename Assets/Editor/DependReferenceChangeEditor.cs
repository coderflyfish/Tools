using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;
using System.Collections;

public class DependReferenceChangeEditor
{
    private const string SearchPath = "TestSearch";
    [MenuItem("Assets/应用修改到全部依赖资源")]
    [MenuItem("UI/应用修改到全部依赖资源")]
    public static void OprDependReferncePrefabs()
    {
       List<GameObject> replaceObjs = searchGameObjects();
        GameObject[] objs = Selection.gameObjects;
        for (int i = 0; i < objs.Length; i++)
        {
            OprDependReferncePrefab(objs[i], replaceObjs);
        }
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

    private static List<GameObject> searchGameObjects( )
    {

        List<GameObject> results = new List<GameObject>();
        string[] paths = GetPathsByDirectoryAndTail(SearchPath,"*.prefab");
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (obj != null)
            {
                results.Add(obj);
            }
        }
        return results;
    }
    private static void OprDependReferncePrefab(GameObject prefab,List<GameObject> replaceObjs )
    {
        if(replaceObjs==null||replaceObjs.Count==0)
            return;
       CMonoBehaviour[] cMono = prefab.GetComponentsInChildren<CMonoBehaviour>();
       Dictionary<Type, CMonoBehaviour> needChanges = new Dictionary<Type, CMonoBehaviour>();
       for (int i = 0; i < cMono.Length; i++)
       {
           if (cMono[i].ConnectRefPrefab)
           {
               needChanges.Add(cMono[i].GetType(), cMono[i]);
           }
       }
        GameObject replaceInstance = null;
       for (int j = 0; j < replaceObjs.Count; j++)
       {
           bool change = false;
           if (prefab != replaceObjs[j])
           {
               replaceInstance =(GameObject)PrefabUtility.InstantiatePrefab(replaceObjs[j]);
               CMonoBehaviour[] cMonoReplace = replaceInstance.GetComponentsInChildren<CMonoBehaviour>();
               for (int i = 0; i < cMonoReplace.Length; i++)
               {
                   if (cMonoReplace[i].ConnectRefPrefab)
                   {
                       Type type = cMonoReplace[i].GetType();
                       if (needChanges.ContainsKey(type))
                       {
                           change = true;
                           GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(PrefabUtility.FindPrefabRoot(needChanges[type].gameObject));
                           var components =instance.GetComponentsInChildren(type, true);
                           string logStr = "使用 ";
                           Component useCom = null;
                           if (components.Length == 1)
                           {
                               useCom = components[0];
                           }
                           else
                           {
                               for (int k = 0; k < components.Length; k++)
                               {
                                   if (components[k].gameObject.name == cMonoReplace[i].name)
                                   {
                                       useCom = components[k];
                                       
                                       break;
                                   }
                               }
                           }
                           if (useCom != null)
                           {
                               logStr += instance.name + "的 " + useCom.gameObject.name + "替换了 " + replaceInstance.name +
                                         "的" + cMonoReplace[i].name;
                               Debug.Log(logStr);
                               cMonoReplace[i] = ReplaceChangePart(useCom.transform, cMonoReplace[i].transform);
                           }
                           
                           if (cMonoReplace[i].gameObject != instance)
                           {
                               GameObject.DestroyImmediate(instance);
                           }
                           if (cMonoReplace[i].transform.parent == null)
                           {
                               replaceInstance = cMonoReplace[i].gameObject;
                               break;
                           }
                           
                       }
                   }
               }
               if (change)
               {
                   replaceObjs[j] = PrefabUtility.ReplacePrefab(replaceInstance, replaceObjs[j]);
               }
               AssetDatabase.SaveAssets();
               GameObject.DestroyImmediate(replaceInstance);  
              
           }
       }
    }

    private static CMonoBehaviour ReplaceChangePart(Transform source, Transform target)
    {
        PrefabUtility.DisconnectPrefabInstance(PrefabUtility.FindPrefabRoot(source.gameObject));
        source.parent = target.parent;
        source.localPosition = target.localPosition;
        source.localRotation = target.localRotation;
        source.localScale = target.localScale;
        source.gameObject.name = target.gameObject.name;
        GameObject.DestroyImmediate(target.gameObject); 
        
        return source.GetComponent<CMonoBehaviour>();
    }
}
