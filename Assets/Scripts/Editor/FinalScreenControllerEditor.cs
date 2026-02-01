using System.Collections.Generic;
using System.Text.RegularExpressions;
using DialogueSystem;
using Round;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(FinalScreenController))]
    public class FinalScreenControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var controller = (FinalScreenController)target;

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(controller.RoundSequence == null))
            {
                if (GUILayout.Button("Extract Words from Dialogues", GUILayout.Height(30)))
                {
                    ExtractWordsFromDialogues(controller);
                }
            }

            if (controller.RoundSequence == null)
            {
                EditorGUILayout.HelpBox("Assign a RoundSequence to enable word extraction.", MessageType.Info);
            }
        }

        void ExtractWordsFromDialogues(FinalScreenController controller)
        {
            if (controller.RoundSequence == null)
            {
                Debug.LogWarning("No RoundSequence assigned.");
                return;
            }

            var extractedWords = new List<WordEntry>();
            var seenIds = new HashSet<string>();

            // Pattern to match <link=ID>TEXT</link>
            var linkRegex = new Regex(
                @"<link\s*=\s*[""']?(?<id>[^""'>\s]+)[""']?\s*>(?<text>.*?)</link>",
                RegexOptions.Singleline
            );

            foreach (var dialogue in controller.RoundSequence.dialogues)
            {
                if (dialogue == null || dialogue.nodes == null) continue;

                foreach (var node in dialogue.nodes)
                {
                    if (string.IsNullOrEmpty(node.text)) continue;

                    var matches = linkRegex.Matches(node.text);
                    foreach (Match match in matches)
                    {
                        string id = match.Groups["id"].Value;
                        string text = match.Groups["text"].Value;

                        if (seenIds.Contains(id)) continue;
                        seenIds.Add(id);

                        extractedWords.Add(new WordEntry
                        {
                            id = id,
                            title = text,
                            type = WordType.Subject
                        });
                    }
                }
            }

            // Record undo
            Undo.RecordObject(controller, "Extract Words from Dialogues");

            // Preserve existing words that are not in the extracted set
            var existingWords = controller.Words ?? new List<WordEntry>();
            foreach (var existing in existingWords)
            {
                if (!seenIds.Contains(existing.id))
                {
                    extractedWords.Add(existing);
                }
            }

            controller.Words = extractedWords;

            EditorUtility.SetDirty(controller);

            Debug.Log($"Extracted {seenIds.Count} words from dialogues. Total words: {extractedWords.Count}");
        }
    }
}
