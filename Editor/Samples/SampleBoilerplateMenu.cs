using System.Collections.Generic;
using AlephVault.Unity.Boilerplates.Utils;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace AlephVault.Unity.Boilerplates
{
    namespace Samples
    {
        public class SampleBoilerplateMenu : Boilerplate
        {
            // [MenuItem("Assets/Create/Sample Boilerplate/Main", priority = 101)]
            public static void CreateSampleProjectBoilerplate()
            {
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Packages/com.alephvault.unity.boilerplates/Editor/Assets/SampleScript.cs.txt"
                );
                var templateAction = InstantiateScriptCodeTemplate(
                    asset, "MyScriptName", new Dictionary<string, string> {
                        {"FOO", "2"},
                        {"BAR", "3"},
                        {"BAZ", "5"},
                        {"QOO", "7"}
                    }
                );
                new SampleBoilerplateMenu()
                    .IntoDirectory("Game")
                        .IntoDirectory("Objects")
                        .End()
                        .IntoDirectory("Maps")
                            .Do(templateAction)
                        .End()
                    .End()
                    ;
            }
        }
    }
}
#endif
