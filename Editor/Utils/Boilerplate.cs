using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#if UNITY_EDITOR
namespace AlephVault.Unity.Boilerplates
{
    namespace Utils
    {
        /// <summary>
        ///   This is a DSL-like class to generate a boilerplate satisfying custom
        ///   user needs of layout.
        /// </summary>
        public class Boilerplate
        {
            /// <summary>
            ///   Exception to be raised when there is an extra call to <see cref="End"/>, with respect
            ///   to the calls to <see cref="IntoDirectory"/>.
            /// </summary>
            public class UnbalancedEndCallException : AlephVault.Unity.Support.Types.Exception
            {
                public UnbalancedEndCallException() { }
                public UnbalancedEndCallException(string message) : base(message) { }
                public UnbalancedEndCallException(string message, System.Exception inner) : base(message, inner) { }
            }

            /// <summary>
            ///   Exception to be raised when trying to dive into a non-Directory asset when performing
            ///   a call to <see cref="Boilerplate.IntoDirectory"/>.
            /// </summary>
            public class NotADirectoryException : AlephVault.Unity.Support.Types.Exception
            {
                public NotADirectoryException() { }
                public NotADirectoryException(string message) : base(message) { }
                public NotADirectoryException(string message, System.Exception inner) : base(message, inner) { }
            }

            /// <summary>
            ///   Exception to be raised when trying to dive into a non-existing Directory asset when performing
            ///   a call to <see cref="Boilerplate.IntoDirectory"/> and <c>makeIfAbsent</c> is false.
            /// </summary>
            public class DirectoryNotFoundException : AlephVault.Unity.Support.Types.Exception
            {
                public DirectoryNotFoundException() { }
                public DirectoryNotFoundException(string message) : base(message) { }
                public DirectoryNotFoundException(string message, System.Exception inner) : base(message, inner) { }
            }
            
            /**
             * The current context for the boilerplate generator.
             */
            private List<string> context;

            public Boilerplate()
            {
                context = new List<string>();
            }

            /// <summary>
            ///   Ends the active context of the current sub-directory and goes back
            ///   one level up to the parent directory.
            /// </summary>
            /// <exception cref="NotADirectoryException">
            ///   This method was invoked when no subdirectory was the boilerplate into.
            /// </exception>
            public Boilerplate End()
            {
                if (context.Count > 0)
                {
                    context.RemoveAt(context.Count - 1);
                    return this;
                }

                throw new UnbalancedEndCallException();
            }

            /// <summary>
            ///   Dives into a subdirectory, creating it if absent.
            /// </summary>
            /// <param name="directory">The subdirectory to dive into</param>
            /// <param name="makeIfAbsent">Whether to create it if it does not exist</param>
            /// <exception cref="ArgumentException">
            ///   The directory has an invalid name.
            /// </exception>
            /// <exception cref="NotADirectoryException">
            ///   The referenced existing asset is not a directory.
            /// </exception>
            /// <exception cref="DirectoryNotFoundException">
            ///   The directory does not exist (and <c>makeIfAbsent</c> is false).
            /// </exception>
            public Boilerplate IntoDirectory(string directory, bool makeIfAbsent = true)
            {
                if (directory == null) throw new ArgumentNullException(nameof(directory));
                
                directory = directory.Trim();
                var regex = new Regex("^[A-Z-a-z0-9]+([._-][A-Z-a-z0-9]+)*$");
                if (!regex.Match(directory).Success)
                {
                    throw new ArgumentException(
                        "Invalid directory name. It must consist of letters and numbers, " +
                        "perhaps separated by single instances of '-', '_', or '.'"
                    );
                }

                // Get the current directory path.
                string currentPath = context.Count == 0 ? "Assets" : $"Assets/{string.Join("/", context)}";

                // Compose the full directory of this.
                string fullPath = $"{currentPath}/{directory}";

                // Try to retrieve the asset.
                DefaultAsset dirAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(fullPath);
                if (dirAsset != null)
                {
                    if (!Directory.Exists(fullPath))
                    {
                        throw new NotADirectoryException(
                            $"The current asset '{fullPath}' is not a directory"
                        );
                    }
                    Debug.Log($"Using the directory: {currentPath}:{directory}");
                    context.Add(directory);
                    return this;
                }
                if (!makeIfAbsent)
                {
                    throw new DirectoryNotFoundException(
                        $"The directory '{fullPath}' does not exist"
                    );
                }
                Debug.Log($"Creating the directory: {currentPath}:{directory}");
                AssetDatabase.CreateFolder(currentPath, directory);
                context.Add(directory);
                return this;
            }

            /// <summary>
            ///   Executes An action in the given directory.
            /// </summary>
            /// <param name="onFolder">The action to execute</param>
            public Boilerplate Do(params Action<Boilerplate, string>[] onFolder)
            {
                if (onFolder == null) throw new ArgumentNullException(nameof(onFolder));
                
                // Compose the full directory of this.
                string fullPath = $"Assets/{string.Join("/", context)}";
                
                // Invoke the callbacks, in order.
                foreach (var callback in onFolder)
                {
                    callback(this, fullPath);
                }

                return this;
            }

            /// <summary>
            ///   Returns an action that instantiates a script (i.e. a ".cs" asset) from
            ///   a code template. The criteria to do this involves the following:
            ///   - #SCRIPTNAME# corresponds to the new script name (without .cs extension).
            ///   - #SCRIPTNAME_LOWER# corresponds to the camelCase (instead of PascalCase)
            ///     name of the script.
            ///   - #NAME# is an alias to #SCRIPTNAME#.
            ///   - ## corresponds to a single #.
            ///   - #CUSTOM_KEYWORDS# can be added as well. The entries must satisfy the expression:
            ///     [A-Z](_*[A-Z0-9]+)* or it will be an error.
            /// </summary>
            /// <returns>An action to be used inside of <see cref="Do" /></returns>
            public static Action<Boilerplate, string> InstantiateScriptCodeTemplate(
                TextAsset source, string targetScriptName, Dictionary<string, string> replacements,
                bool errorOnIncompleteTags = false
            ) {
                if (source == null) throw new ArgumentNullException(nameof(source));
                if (targetScriptName == null) throw new ArgumentNullException(nameof(targetScriptName));
                string contents = source.text;
                string[] parts = source.name.Split(new[] {'.'}, 2);
                string fileName = parts.Length == 1 ? targetScriptName : $"{targetScriptName}.{parts[1]}";
                
                return delegate(Boilerplate boilerplate, string directoryPath)
                {
                    string fullFilePath = $"{directoryPath}/{fileName}";
                    Debug.Log($"Target file: {fullFilePath}");
                    using (StreamWriter outfile = new StreamWriter(fullFilePath))
                    {
                        outfile.Write(TemplateReplacer.ProcessScriptTemplateContents(
                            targetScriptName, contents, replacements, errorOnIncompleteTags
                        ));
                    }
                    AssetDatabase.Refresh();
                };
            }
        }
    }
}
#endif
