using UnityEngine;
using UnityEngine.UIElements;

namespace EndlessRunner
{
    public static class UIDocumentLocator
    {
        private const string GameplayDocumentObjectName = "GameUI";
        private const string MainMenuDocumentObjectName = "MainMenuUI";

        public static UIDocument FindGameplayDocument()
        {
            return FindDocument(GameplayDocumentObjectName, "pause-button", "menu-panel");
        }

        public static UIDocument FindMainMenuDocument()
        {
            return FindDocument(MainMenuDocumentObjectName, "mainmenu-play-button", "mainmenu-collection-overlay");
        }

        public static UIDocument FindDocument(string objectName, params string[] requiredElementNames)
        {
            UIDocument namedDocument = FindNamedDocument(objectName);
            if (namedDocument != null)
            {
                return namedDocument;
            }

            UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            for (int i = 0; i < documents.Length; i++)
            {
                UIDocument document = documents[i];
                if (DocumentContainsAllElements(document, requiredElementNames))
                {
                    return document;
                }
            }

            return null;
        }

        public static UIDocument FindNamedDocument(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            GameObject root = GameObject.Find(objectName);
            return root != null ? root.GetComponent<UIDocument>() : null;
        }

        public static bool DocumentContainsElement(UIDocument document, string elementName)
        {
            if (document == null || string.IsNullOrWhiteSpace(elementName))
            {
                return false;
            }

            VisualElement root = document.rootVisualElement;
            return root != null && root.Q<VisualElement>(elementName) != null;
        }

        public static bool DocumentContainsAllElements(UIDocument document, params string[] elementNames)
        {
            if (document == null)
            {
                return false;
            }

            if (elementNames == null || elementNames.Length == 0)
            {
                return true;
            }

            VisualElement root = document.rootVisualElement;
            if (root == null)
            {
                return false;
            }

            for (int i = 0; i < elementNames.Length; i++)
            {
                string elementName = elementNames[i];
                if (string.IsNullOrWhiteSpace(elementName))
                {
                    continue;
                }

                if (root.Q<VisualElement>(elementName) == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
