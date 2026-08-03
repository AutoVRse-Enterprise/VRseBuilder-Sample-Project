using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using VRseBuilder.Tools.Editor;

namespace VRseBuilder.RadiatorAssembly.Editor
{
    [InitializeOnLoad]
    internal static class TwoButtonsTouchTriggerInitializer
    {
        private const string HelperPath = "Assets/VRseBuilder/ExtendedScripts/CustomJsonHelper.cs";
        private const string TemplatesPath = "Assets/VRseBuilder/ExtendedScripts/CustomNodeTemplatesData.asset";
        private const string EnsureExtensionFilesMenu =
            "Tools/VRseBuilder/Extensions/Ensure Project Extension Files";

        private const string TriggerName = "TwoButtonsTouchTrigger";
        private const string PressedOptionName = "TwoButtonsPressed";
        private const string NotPressedOptionName = "TwoButtonsNotPressed";
        private const string TriggerDescription =
            "Triggers when both configured buttons are or are not pressed within the allowed time.";
        private const string PressedOptionDescription =
            "Both buttons were pressed within the allowed time.";
        private const string NotPressedOptionDescription =
            "Both buttons were not pressed within the allowed time.";

        static TwoButtonsTouchTriggerInitializer()
        {
            QueueEnsureRegistered();
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            QueueEnsureRegistered();
        }

        private static void QueueEnsureRegistered()
        {
            EditorApplication.delayCall -= EnsureRegistered;
            EditorApplication.delayCall += EnsureRegistered;
        }

        private static void EnsureRegistered()
        {
            EditorApplication.delayCall -= EnsureRegistered;

            try
            {
                if (!ExtensionFilesExist())
                {
                    if (EditorApplication.ExecuteMenuItem(EnsureExtensionFilesMenu))
                        QueueEnsureRegistered();
                    else
                        Debug.LogError($"[Radiator Assembly] Could not run '{EnsureExtensionFilesMenu}'.");

                    return;
                }

                bool sourceChanged = EnsureSourceRegistration();
                bool templateChanged = EnsureTemplateRegistration();

                if (templateChanged)
                    AssetDatabase.SaveAssets();

                if (sourceChanged)
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                if (sourceChanged || templateChanged)
                    Debug.Log("[Radiator Assembly] Registered TwoButtonsTouchTrigger custom-node extensions.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Radiator Assembly] Failed to register TwoButtonsTouchTrigger: {exception}");
            }
        }

        private static bool ExtensionFilesExist()
        {
            return File.Exists(HelperPath)
                   && AssetDatabase.LoadAssetAtPath<CustomNodeTemplatesData>(TemplatesPath) != null;
        }

        private static bool EnsureSourceRegistration()
        {
            SourceDocument helper = ReadSource(HelperPath);
            string updatedHelper = AddMissingFactoryArm(helper.Text);
            bool helperChanged = !string.Equals(helper.Text, updatedHelper, StringComparison.Ordinal);

            if (helperChanged)
                File.WriteAllText(HelperPath, updatedHelper, helper.Encoding);

            return helperChanged;
        }

        private static string AddMissingFactoryArm(string source)
        {
            Match methodMatch = Regex.Match(source, @"\bTryCreateTrigger\s*\(");
            if (!methodMatch.Success)
                throw new InvalidOperationException($"Could not find TryCreateTrigger in '{HelperPath}'.");

            int openingBrace = source.IndexOf('{', methodMatch.Index + methodMatch.Length);
            int closingBrace = FindMatchingBrace(source, openingBrace);
            if (openingBrace < 0 || closingBrace < 0)
                throw new InvalidOperationException($"Could not locate the TryCreateTrigger body in '{HelperPath}'.");

            string methodBody = source.Substring(openingBrace, closingBrace - openingBrace + 1);
            if (Regex.IsMatch(methodBody,
                    "\"TwoButtonsTouchTrigger\"\\s*=>\\s*new\\s+(?:global::)?TwoButtonsTouchTrigger\\s*\\("))
            {
                return source;
            }

            Match fallbackMatch = Regex.Match(
                methodBody,
                @"(?m)^(?<indent>[ \t]*)_\s*=>\s*null\s*,?\s*$");
            if (!fallbackMatch.Success)
            {
                throw new InvalidOperationException(
                    $"Could not find the trigger switch fallback in '{HelperPath}'; the file was not changed.");
            }

            string newline = DetectNewline(source);
            string indentation = fallbackMatch.Groups["indent"].Value;
            string factoryArm = indentation
                                + "\"TwoButtonsTouchTrigger\" => "
                                + "new global::TwoButtonsTouchTrigger(),"
                                + newline;
            int insertionIndex = openingBrace + fallbackMatch.Index;
            return source.Insert(insertionIndex, factoryArm);
        }

        private static bool EnsureTemplateRegistration()
        {
            CustomNodeTemplatesData templates =
                AssetDatabase.LoadAssetAtPath<CustomNodeTemplatesData>(TemplatesPath);
            if (templates == null)
                throw new InvalidOperationException($"Could not load '{TemplatesPath}'.");

            templates.actionTemplates ??= new List<NodeTemplatesData.NodeData>();
            templates.triggerTemplates ??= new List<NodeTemplatesData.NodeData>();

            NodeTemplatesData.NodeData actionWithSameName = templates.actionTemplates.Find(
                node => node != null && string.Equals(node.Name, TriggerName, StringComparison.Ordinal));
            if (actionWithSameName != null)
            {
                throw new InvalidOperationException(
                    $"'{TriggerName}' is already registered as an action in '{TemplatesPath}'.");
            }

            bool changed = false;
            NodeTemplatesData.NodeData trigger = templates.triggerTemplates.Find(
                node => node != null && string.Equals(node.Name, TriggerName, StringComparison.Ordinal));

            if (trigger == null)
            {
                trigger = new NodeTemplatesData.NodeData
                {
                    Name = TriggerName,
                    BackendId = string.Empty,
                    Type = "custom",
                    Description = TriggerDescription,
                    Options = new List<NodeTemplatesData.OptionData>()
                };
                templates.triggerTemplates.Add(trigger);
                changed = true;
            }
            else
            {
                if (string.IsNullOrEmpty(trigger.Type))
                {
                    trigger.Type = "custom";
                    changed = true;
                }

                if (string.IsNullOrEmpty(trigger.Description))
                {
                    trigger.Description = TriggerDescription;
                    changed = true;
                }

                if (trigger.Options == null)
                {
                    trigger.Options = new List<NodeTemplatesData.OptionData>();
                    changed = true;
                }
            }

            changed |= EnsureOption(trigger.Options, PressedOptionName, PressedOptionDescription);
            changed |= EnsureOption(trigger.Options, NotPressedOptionName, NotPressedOptionDescription);

            if (changed)
                EditorUtility.SetDirty(templates);

            return changed;
        }

        private static bool EnsureOption(
            List<NodeTemplatesData.OptionData> options,
            string optionName,
            string description)
        {
            NodeTemplatesData.OptionData option = options.Find(
                candidate => candidate != null
                             && string.Equals(candidate.Name, optionName, StringComparison.Ordinal));
            if (option == null)
            {
                options.Add(new NodeTemplatesData.OptionData
                {
                    Name = optionName,
                    Description = description,
                    Parameters = new List<NodeTemplatesData.ParameterData>(),
                    NestedParameters = new List<NodeTemplatesData.NestedParameterData>()
                });
                return true;
            }

            bool changed = false;
            if (string.IsNullOrEmpty(option.Description))
            {
                option.Description = description;
                changed = true;
            }

            if (option.Parameters == null)
            {
                option.Parameters = new List<NodeTemplatesData.ParameterData>();
                changed = true;
            }

            if (option.NestedParameters == null)
            {
                option.NestedParameters = new List<NodeTemplatesData.NestedParameterData>();
                changed = true;
            }

            return changed;
        }

        private static SourceDocument ReadSource(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8, true);
            string text = reader.ReadToEnd();
            return new SourceDocument(text, reader.CurrentEncoding);
        }

        private static string DetectNewline(string source)
        {
            return source.Contains("\r\n") ? "\r\n" : "\n";
        }

        private static int FindLineStart(string source, int index)
        {
            int newlineIndex = source.LastIndexOf('\n', Math.Max(0, index - 1));
            return newlineIndex < 0 ? 0 : newlineIndex + 1;
        }

        private static int FindMatchingBrace(string source, int openingBrace)
        {
            if (openingBrace < 0 || openingBrace >= source.Length || source[openingBrace] != '{')
                return -1;

            int depth = 0;
            bool inLineComment = false;
            bool inBlockComment = false;
            bool inString = false;
            bool inCharacter = false;
            bool verbatimString = false;

            for (int index = openingBrace; index < source.Length; index++)
            {
                char current = source[index];
                char next = index + 1 < source.Length ? source[index + 1] : '\0';

                if (inLineComment)
                {
                    if (current == '\n')
                        inLineComment = false;
                    continue;
                }

                if (inBlockComment)
                {
                    if (current == '*' && next == '/')
                    {
                        inBlockComment = false;
                        index++;
                    }
                    continue;
                }

                if (inString)
                {
                    if (verbatimString && current == '"' && next == '"')
                    {
                        index++;
                        continue;
                    }

                    if (!verbatimString && current == '\\')
                    {
                        index++;
                        continue;
                    }

                    if (current == '"')
                        inString = false;
                    continue;
                }

                if (inCharacter)
                {
                    if (current == '\\')
                    {
                        index++;
                        continue;
                    }

                    if (current == '\'')
                        inCharacter = false;
                    continue;
                }

                if (current == '/' && next == '/')
                {
                    inLineComment = true;
                    index++;
                    continue;
                }

                if (current == '/' && next == '*')
                {
                    inBlockComment = true;
                    index++;
                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    verbatimString = index > 0 && source[index - 1] == '@';
                    continue;
                }

                if (current == '\'')
                {
                    inCharacter = true;
                    continue;
                }

                if (current == '{')
                {
                    depth++;
                }
                else if (current == '}' && --depth == 0)
                {
                    return index;
                }
            }

            return -1;
        }

        private readonly struct SourceDocument
        {
            public SourceDocument(string text, Encoding encoding)
            {
                Text = text;
                Encoding = encoding;
            }

            public string Text { get; }
            public Encoding Encoding { get; }
        }
    }
}
