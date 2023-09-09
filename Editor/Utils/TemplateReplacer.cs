using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AlephVault.Unity.Boilerplates
{
    namespace Utils
    {
        /// <summary>
        ///   This class has some methods to process a Unity .cs.txt template
        ///   and process it properly by replacing the relevant variables.
        /// </summary>
        public static class TemplateReplacer
        {
            private static Regex KEY_FORMAT = new Regex("[A-Z][A-Z0-9_]*");
            
            /// <summary>
            ///   Exception to be raised when a key is missing on template resolution.
            /// </summary>
            public class TemplateKeyNotFoundException : AlephVault.Unity.Support.Types.Exception
            {
                /// <summary>
                ///   The missing key.
                /// </summary>
                public readonly string Key;

                public TemplateKeyNotFoundException(string key) : base("Unsatisfied template key: " + key)
                {
                    Key = key;
                }
            }

            /// <summary>
            ///   Exception to be raised when a marker is invalid (markers must start with
            ///   a letter or underscore, continue with letters, numbers or underscores, and
            ///   be enclosed in #s -- also, an empty ## marker is allowed and escapes as a
            ///   a single # character).
            /// </summary>
            public class InvalidTemplateMarkerException : AlephVault.Unity.Support.Types.Exception
            {
                /// <summary>
                ///   The invalid marker.
                /// </summary>
                public readonly string Marker;

                public InvalidTemplateMarkerException(string marker) : base("Invalid template marker: " + marker)
                {
                    Marker = marker;
                }
            }

            // Generates the replacer for the matches. Validates that the matches are valid
            // in their internal syntax (to check whether the markers are complete and valid).
            private static Func<Match, string> MakeReplacer(
                Dictionary<string, string> entries, bool errorOnIncompleteTags = false
            ) {
                return delegate(Match match) {
                    if (!match.Value.EndsWith("#"))
                    {
                        if (errorOnIncompleteTags)
                        {
                            throw new InvalidTemplateMarkerException(match.Value);
                        }
                        // Useful for Unity compatibility. Allows certain lines to be
                        // left unchanged, like this one:
                        // #region my_code_region
                        return match.Value;
                    }
                    string value = match.Value.Substring(1, match.Value.Length - 2);
                    if (value == "")
                    {
                        return "#";
                    }
                    if (KEY_FORMAT.IsMatch(value))
                    {
                        try
                        {
                            return entries[value];
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new TemplateKeyNotFoundException(value);
                        }
                    }
                    throw new InvalidTemplateMarkerException(match.Value);
                };
            }

            /// <summary>
            ///   Processes templates' contents. The syntax is akin to Unity' syntax
            ///   for code templates, but invalid tags will be signaled and raise
            ///   errors.
            /// </summary>
            /// <param name="contents">The string contents to parse</param>
            /// <param name="replacements">The replacements to apply to each #TEMPLATE_VARIABLE#</param>
            /// <param name="errorOnIncompleteTags">
            ///   Whether to treat chunks #like_this as erroneous / incomplete markers (raising
            ///   an error) or treat it like plain text (default behaviour, and compatible with
            ///   Unity code templates processing)
            /// </param>
            /// <returns>The processed template</returns>
            /// <exception cref="InvalidTemplateMarkerException">
            ///   A marker has invalid syntax, or is incomplete and <para>errorOnIncompleteTags</para>
            ///   is <c>true</c>.
            /// </exception>
            /// <exception cref="TemplateKeyNotFoundException">
            ///   A template key was not found among the replacements.
            /// </exception>
            public static string ProcessTemplateContents(
                string contents, Dictionary<string, string> replacements, bool errorOnIncompleteTags = false
            ) {
                return new Regex("#[A-Z0-9_]*([^A-Z0-9_]|$)").Replace(
                    contents, new MatchEvaluator(MakeReplacer(replacements, errorOnIncompleteTags))
                );
            }

            /// <summary>
            ///   Expands the replacements with script-related information. This invovles the gneration of 3 new
            ///   variables, which are compatible to Unity's way of generating scripts:
            ///   - SCRIPTNAME: The script name.
            ///   - NAME: The script name.
            ///   - SCRIPTNAME_LOWER: The same as SCRIPTNAME but with the first character to lowercase.
            /// </summary>
            /// <returns>The new replacements dictionary</returns>
            public static Dictionary<string, string> AddScriptTemplateVariables(
                string scriptName, Dictionary<string, string> replacements
            )
            {
                Dictionary<string, string> newReplacements = new Dictionary<string, string>(replacements);
                newReplacements["SCRIPTNAME"] = scriptName;
                newReplacements["NAME"] = scriptName;
                newReplacements["SCRIPTNAME_LOWER"] = scriptName.Substring(0, 1).ToLower() +
                                                      scriptName.Substring(1);
                return newReplacements;
            }
        }
    }
}