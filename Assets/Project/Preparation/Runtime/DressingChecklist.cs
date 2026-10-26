using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.InputSystem;

namespace GroupProject.Preparation
{
    /// <summary>Grouped, per-piece dressing progress. Its position and visibility belong to the journey.</summary>
    public sealed class DressingChecklist : MonoBehaviour
    {
        sealed class Item
        {
            public WearableGarment.Kind kind;
            public string confirmation;
            public Image surface, marker, accent;
            public GameObject tick;
            public TMP_Text label;
            public bool done;
        }

        static readonly Color Ink = new Color(.08f, .18f, .22f);
        static readonly Color Muted = new Color(.32f, .42f, .46f);
        static readonly Color Active = new Color(.04f, .47f, .61f);
        static readonly Color Complete = new Color(.07f, .42f, .29f);
        readonly List<Item> items = new List<Item>();
        PreparationJourney journey;
        Canvas canvas;
        RectTransform paper, tab;
        TMP_Text toggle;
        bool wasPressed;
        Material panelMaterial, textMaterial;
        TMP_Text count, status, instruction;
        RectTransform progress;
        Image feedbackStripe;
        bool hasSnapshot;
        float confirmationUntil;
        string confirmation;
        public bool Visible { get; private set; } = true;

        public void Build(PreparationJourney owner)
        {
            journey = owner;
            if (canvas) return;
            canvas = gameObject.GetComponent<Canvas>();
            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30010;
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(400, 600);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            transform.localScale = Vector3.one * .001f;
            var panelShader = Resources.Load<Shader>("ChecklistPanel");
            if (panelShader) panelMaterial = new Material(panelShader);
            var font = TMP_Settings.defaultFontAsset;
            if (font)
            {
                textMaterial = new Material(font.material);
                var textShader = Resources.Load<Shader>("ChecklistText");
                if (textShader) textMaterial.shader = textShader;
                textMaterial.renderQueue = 4010;
            }

            paper = Panel("Paper edge", rect, 0, 0, 400, 600, new Color(.10f, .25f, .30f));
            var sheet = Panel("Dressing card", paper, 3, -3, 394, 594, new Color(.96f, .98f, .97f));
            Panel("Section accent", sheet, 0, 0, 6, 66, Active);
            Text(sheet, "DRESSING CHECK", 20, -15, 354, 32, 26, Ink, FontStyles.Bold);
            count = Text(sheet, "0 / 12 COMPLETE", 21, -48, 352, 24, 16, Muted, FontStyles.Bold);
            var track = Panel("Progress track", sheet, 20, -78, 354, 4, new Color(.81f, .86f, .85f));
            progress = Panel("Completed progress", track, 0, 0, 0, 4, Complete);

            var head = Group(sheet, "01   HEAD", -95, 98);
            Add(head, WearableGarment.Kind.HairCap, "Hair cap", "Hair cap fitted.", 0, 0);
            Add(head, WearableGarment.Kind.Mask, "Face mask", "Face mask fitted.", 1, 0);
            Add(head, WearableGarment.Kind.Hood, "Hood", "Hood fitted.", 0, 1);
            Add(head, WearableGarment.Kind.SafetyGlasses, "Safety glasses", "Safety glasses fitted.", 1, 1);
            var body = Group(sheet, "02   BODY", -201, 66);
            Add(body, WearableGarment.Kind.PersonalJacket, "Jacket stored", "Personal jacket stored.", 0, 0);
            Add(body, WearableGarment.Kind.Coverall, "Coverall", "Coverall fitted.", 1, 0);
            var hands = Group(sheet, "03   HANDS", -275, 66);
            Add(hands, WearableGarment.Kind.LeftGlove, "Left glove", "Left glove fitted.", 0, 0);
            Add(hands, WearableGarment.Kind.RightGlove, "Right glove", "Right glove fitted.", 1, 0);
            var feet = Group(sheet, "04   FEET", -349, 98);
            Add(feet, WearableGarment.Kind.LeftBoot, "Left boot", "Left boot placed.", 0, 0);
            Add(feet, WearableGarment.Kind.RightBoot, "Right boot", "Right boot placed.", 1, 0);
            Add(feet, WearableGarment.Kind.LeftOvershoe, "Left overshoe", "Left overshoe fitted.", 0, 1);
            Add(feet, WearableGarment.Kind.RightOvershoe, "Right overshoe", "Right overshoe fitted.", 1, 1);

            var footer = Panel("Action feedback", sheet, 20, -461, 354, 116, new Color(.88f, .94f, .95f));
            feedbackStripe = Panel("Action state", footer, 0, 0, 4, 116, Active).GetComponent<Image>();
            status = Text(footer, "YOUR NEXT ACTION", 14, -10, 328, 22, 16, Active, FontStyles.Bold);
            instruction = Text(footer, "Store your personal jacket in the locker.", 14, -37, 326, 70, 18, Ink);
            tab = Panel("Outfit list toggle", rect, 0, 34, 400, 30, new Color(.10f, .25f, .30f));
            toggle = Text(tab, "X  Hide outfit list", 12, -3, 376, 26, 18, Color.white, FontStyles.Bold);
            SetVisible(Visible);
        }

        RectTransform Group(Transform parent, string heading, float y, float height)
        {
            var group = Panel(heading, parent, 20, y, 354, height, new Color(.90f, .94f, .93f));
            Text(group, heading, 10, -5, 332, 23, 16, Muted, FontStyles.Bold);
            return group;
        }

        void Add(Transform group, WearableGarment.Kind kind, string label, string done, int column, int row)
        {
            var cell = Panel(label, group, 7 + column * 173, -30 - row * 32, 166, 29, Color.white);
            var marker = Panel("State marker", cell, 7, -6, 18, 18, new Color(.73f, .79f, .78f));
            Panel("Empty checkbox", marker, 2, -2, 14, 14, new Color(.98f, .99f, .99f));
            var tick = Panel("Checkmark", cell, 7, -6, 18, 18, Color.clear);
            var shortStroke = Panel("Check lower stroke", tick, 3, -8, 7, 3, Color.white);
            shortStroke.localRotation = Quaternion.Euler(0, 0, -45);
            var longStroke = Panel("Check rising stroke", tick, 6, -13, 14, 3, Color.white);
            longStroke.localRotation = Quaternion.Euler(0, 0, 47);
            tick.gameObject.SetActive(false);
            var item = new Item
            {
                kind = kind,
                confirmation = done,
                surface = cell.GetComponent<Image>(),
                marker = marker.GetComponent<Image>(),
                accent = Panel("Held indicator", cell, 0, 0, 3, 29, Color.clear).GetComponent<Image>(),
                tick = tick.gameObject,
                label = Text(cell, label, 31, -4, 132, 23, 18, Ink)
            };
            items.Add(item);
        }

        public void Refresh(WearableGarment held, string hint, bool ready)
        {
            if (!journey || !canvas) return;
            int completed = 0;
            foreach (var item in items)
            {
                bool done = item.kind == WearableGarment.Kind.PersonalJacket
                    ? journey.PersonalClothesStored : journey.HasGarment(item.kind);
                if (done) completed++;
                if (hasSnapshot && done && !item.done)
                {
                    confirmation = item.confirmation;
                    confirmationUntil = Time.unscaledTime + 4f;
                }
                item.done = done;
                bool selected = held && held.kind == item.kind && !done;
                item.surface.color = done ? new Color(.84f, .94f, .87f)
                    : selected ? new Color(.77f, .91f, .97f) : new Color(.98f, .99f, .99f);
                item.marker.color = done ? Complete : selected ? Active : new Color(.67f, .75f, .74f);
                item.marker.transform.GetChild(0).gameObject.SetActive(!done);
                item.tick.SetActive(done);
                item.accent.color = selected ? Active : Color.clear;
                item.label.color = done ? Complete : Ink;
                item.label.fontStyle = selected || done ? FontStyles.Bold : FontStyles.Normal;
            }
            hasSnapshot = true;
            count.text = completed + " / " + items.Count + " COMPLETE";
            progress.sizeDelta = new Vector2(354f * completed / items.Count, 4);
            bool allDone = completed == items.Count;
            bool confirmed = Time.unscaledTime < confirmationUntil;
            status.text = allDone ? "OUTFIT COMPLETE" : held && ready ? "READY TO PLACE"
                : held ? "ITEM IN HAND" : confirmed ? "DONE" : "YOUR NEXT ACTION";
            status.color = allDone || confirmed || held && ready ? Complete : Active;
            feedbackStripe.color = status.color;
            instruction.text = allDone ? "Your outfit is ready. Continue to the air shower."
                : held && !string.IsNullOrWhiteSpace(hint) ? hint
                : confirmed ? confirmation
                : !string.IsNullOrWhiteSpace(hint) ? hint : "Take a garment from the locker using Grip.";
        }

        public void Show(bool value)
        {
            if (canvas) canvas.enabled = value;
        }

        public void SetVisible(bool value)
        {
            Visible = value;
            if (paper) paper.gameObject.SetActive(value);
            if (toggle) toggle.text = value ? "X  Hide outfit list" : "X  Show outfit list";
        }

        void Update()
        {
            if (!canvas || !canvas.enabled) { wasPressed = false; return; }
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool pressed);
            if (pressed && !wasPressed) SetVisible(!Visible);
            wasPressed = pressed;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Camera.main &&
                RectTransformUtility.RectangleContainsScreenPoint(tab, Mouse.current.position.ReadValue(), Camera.main))
                SetVisible(!Visible);
        }

        RectTransform Panel(string label, Transform parent, float x, float y, float width, float height, Color color)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            var image = go.GetComponent<Image>();
            image.color = color;
            if (panelMaterial) image.material = panelMaterial;
            image.raycastTarget = false;
            return rect;
        }

        TMP_Text Text(Transform parent, string value, float x, float y, float width, float height,
            float size, Color color, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(value, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            if (textMaterial) text.fontSharedMaterial = textMaterial;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.richText = false;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return text;
        }

        void OnDestroy()
        {
            if (panelMaterial) Destroy(panelMaterial);
            if (textMaterial) Destroy(textMaterial);
        }
    }
}
