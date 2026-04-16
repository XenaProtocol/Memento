using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Memento.Pet;

namespace Memento.Windows.Tabs
{
    /// <summary>
    /// Settings tab — theme picker, emote tracking list, observer customisation
    /// (dot color/size, sound picker), cloud sync, and code redemption.
    /// </summary>
    public class SettingsTab : IDrawablePage
    {
        private readonly Plugin plugin;
        public string TabLabel => " Settings ";
        private string emoteSearch = "";
        private readonly System.Collections.Generic.List<string> allEmotes = new();

        // Stable buffers for cloud sync (prevents ImGui crash)
        private string tokenBuf = "";
        private string charBuf = "";
        private bool bufsLoaded = false;

        // Redemption / easter egg code input
        private string codeInput = "";
        private string codeMessage = "";
        private float codeMsgTimer = 0f;

        private Vector4 AccentColor => plugin.Config.Theme switch
        {
            var t when t.StartsWith("Pretty in Pink") => new(0.98f, 0.25f, 0.58f, 1f),
            var t when t.StartsWith("Glamourous Green") => new(0.22f, 0.82f, 0.50f, 1f),
            var t when t.StartsWith("Beautifully Blue") => new(0.28f, 0.60f, 1.00f, 1f),
            _ => new(0.55f, 0.36f, 0.96f, 1f),
        };
        private Vector4 TextColor => plugin.Config.Theme switch
        {
            "Pretty in Pink (Day)" => new(0.28f, 0.06f, 0.14f, 1f),
            "Glamourous Green (Day)" => new(0.04f, 0.18f, 0.09f, 1f),
            "Beautifully Blue (Day)" => new(0.04f, 0.10f, 0.26f, 1f),
            "Perfectly Purple (Day)" => new(0.18f, 0.09f, 0.32f, 1f),
            _ => new(0.94f, 0.91f, 1.00f, 1f),
        };
        private Vector4 TextDimColor => plugin.Config.Theme switch
        {
            "Pretty in Pink (Day)" => new(0.55f, 0.22f, 0.36f, 1f),
            "Glamourous Green (Day)" => new(0.16f, 0.42f, 0.24f, 1f),
            "Beautifully Blue (Day)" => new(0.20f, 0.35f, 0.60f, 1f),
            "Perfectly Purple (Day)" => new(0.40f, 0.28f, 0.58f, 1f),
            _ => new(0.61f, 0.50f, 0.83f, 1f),
        };
        private Vector4 PanelColor => plugin.Config.Theme switch
        {
            "Pretty in Pink (Day)" => new(0.99f, 0.84f, 0.90f, 0.80f),
            "Glamourous Green (Day)" => new(0.80f, 0.94f, 0.84f, 0.80f),
            "Beautifully Blue (Day)" => new(0.80f, 0.88f, 0.99f, 0.80f),
            "Perfectly Purple (Day)" => new(0.86f, 0.80f, 0.95f, 0.80f),
            _ => new(0.18f, 0.10f, 0.35f, 0.80f),
        };
        private static readonly Vector4 RedSoft = new(0.97f, 0.44f, 0.44f, 1f);

        private static readonly string[] NightThemes = { "Perfectly Purple", "Pretty in Pink", "Glamourous Green", "Beautifully Blue" };
        private static readonly Vector4[] NightSwatches = { new(0.55f, 0.36f, 0.96f, 1f), new(0.98f, 0.25f, 0.58f, 1f), new(0.22f, 0.80f, 0.50f, 1f), new(0.28f, 0.60f, 1.00f, 1f) };
        private static readonly string[] DayThemes = { "Perfectly Purple (Day)", "Pretty in Pink (Day)", "Glamourous Green (Day)", "Beautifully Blue (Day)" };
        private static readonly Vector4[] DaySwatches = { new(0.76f, 0.62f, 0.99f, 1f), new(0.99f, 0.70f, 0.84f, 1f), new(0.55f, 0.90f, 0.68f, 1f), new(0.62f, 0.82f, 1.00f, 1f) };

        public SettingsTab(Plugin plugin) { this.plugin = plugin; }

        public void Draw()
        {
            if (!bufsLoaded) { tokenBuf = plugin.Config.EnzirionToken; charBuf = plugin.Config.EnzirionCharacterName; bufsLoaded = true; }

            float pH = ImGui.GetContentRegionAvail().Y - 4f;
            var accent = AccentColor;
            var textPri = TextColor;
            var textDim = TextDimColor;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
            if (ImGui.BeginChild("##SetScroll", new Vector2(0, pH), false))
            {
                float W = ImGui.GetContentRegionAvail().X; // inside child = excludes scrollbar
                var dl0 = ImGui.GetWindowDrawList();
                dl0.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), true);
                ImGui.Spacing();

                // 1. NOTIFICATIONS
                DrawSectionTitle("Notifications", W, textDim);
                ImGui.Spacing();
                bool showEmote = plugin.Config.ShowEmoteChat;
                DrawToggleRow("Print emotes to chat", "Shows emotes as echo messages", ref showEmote, textPri, textDim, accent);
                if (showEmote != plugin.Config.ShowEmoteChat) { plugin.Config.ShowEmoteChat = showEmote; plugin.Config.Save(); }

                bool showTarget = plugin.CharData.ObserverSettings.ChatNotification;
                DrawToggleRow("Observer chat alerts", "Notify when someone targets you", ref showTarget, textPri, textDim, accent);
                if (showTarget != plugin.CharData.ObserverSettings.ChatNotification) { plugin.CharData.ObserverSettings.ChatNotification = showTarget; plugin.SaveCharData(); }

                ImGui.Spacing(); ImGui.Spacing();

                // 1B. DOT OVERHEAD CUSTOMIZER
                DrawSectionTitle("Dot Overhead", W, textDim);
                ImGui.Spacing();
                {
                    var obs = plugin.CharData.ObserverSettings;

                    // Dot color picker
                    ImGui.TextColored(textDim, "Dot color:");
                    ImGui.SameLine();
                    var dotCol = new Vector3(obs.DotColorR, obs.DotColorG, obs.DotColorB);
                    ImGui.SetNextItemWidth(180f);
                    if (ImGui.ColorEdit3("##DotColor", ref dotCol, ImGuiColorEditFlags.NoInputs))
                    { obs.DotColorR = dotCol.X; obs.DotColorG = dotCol.Y; obs.DotColorB = dotCol.Z; plugin.SaveCharData(); }

                    // Y offset slider
                    ImGui.TextColored(textDim, "Height above head:");
                    ImGui.SameLine();
                    float yOff = obs.DotYOffset;
                    ImGui.SetNextItemWidth(160f);
                    if (ImGui.SliderFloat("##DotY", ref yOff, 0.5f, 5.0f, "%.1f"))
                    { obs.DotYOffset = yOff; plugin.SaveCharData(); }

                    // Radius slider
                    ImGui.TextColored(textDim, "Dot size:");
                    ImGui.SameLine();
                    float dotR = obs.DotRadius;
                    ImGui.SetNextItemWidth(160f);
                    if (ImGui.SliderFloat("##DotR", ref dotR, 2f, 20f, "%.0f"))
                    { obs.DotRadius = dotR; plugin.SaveCharData(); }

                    ImGui.Spacing();
                    ImGui.TextColored(textDim, "Observer toggles:");
                    ImGui.Spacing();

                    // 2×2 grid of toggles — Chat, Sound, Draw Dot, Highlight Nameplate
                    float togW2 = MathF.Floor((W - 8f) / 2f);
                    var startLocalPos = ImGui.GetCursorPos();
                    bool chat = obs.ChatNotification, sound = obs.PlaySound,
                         dot = obs.DrawDotOverhead, plate = obs.HighlightNameplate;

                    // Row 1
                    ImGui.SetCursorPos(startLocalPos);
                    DrawSettingsToggle("Chat notification", "Echo when targeted", ref chat, togW2);
                    ImGui.SetCursorPos(new Vector2(startLocalPos.X + togW2 + 8f, startLocalPos.Y));
                    DrawSettingsToggle("Play sound", "Ping on new target", ref sound, togW2);

                    // Row 2
                    ImGui.SetCursorPos(new Vector2(startLocalPos.X, startLocalPos.Y + 44f + 4f));
                    DrawSettingsToggle("Draw dot overhead", "Marker above players", ref dot, togW2);
                    ImGui.SetCursorPos(new Vector2(startLocalPos.X + togW2 + 8f, startLocalPos.Y + 44f + 4f));
                    DrawSettingsToggle("Highlight nameplate", "Colour their name", ref plate, togW2);

                    ImGui.SetCursorPos(new Vector2(startLocalPos.X, startLocalPos.Y + 44f * 2f + 4f + 4f));

                    if (chat != obs.ChatNotification || sound != obs.PlaySound ||
                        dot != obs.DrawDotOverhead || plate != obs.HighlightNameplate)
                    {
                        obs.ChatNotification = chat; obs.PlaySound = sound;
                        obs.DrawDotOverhead = dot; obs.HighlightNameplate = plate;
                        plugin.SaveCharData();
                    }
                }

                ImGui.Spacing(); ImGui.Spacing();

                // 1C. SOUND CUSTOMIZER
                DrawSectionTitle("Targeting Sound", W, textDim);
                ImGui.Spacing();
                {
                    var obs = plugin.CharData.ObserverSettings;

                    // SE picker — common useful FFXIV SEs with labels
                    string[] seLabels = { "6: Chime", "10: Click", "25: Pop", "29: Alert", "36: Notify",
                                          "37: Ding", "42: Sparkle", "44: Bell", "46: Ping", "51: Soft" };
                    int[] seValues = { 6, 10, 25, 29, 36, 37, 42, 44, 46, 51 };
                    int curIdx = Array.IndexOf(seValues, obs.SoundEffectId);
                    if (curIdx < 0) curIdx = 4; // default to 36

                    ImGui.TextColored(textDim, "Sound:");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150f);
                    if (ImGui.BeginCombo("##SoundSE", seLabels[curIdx]))
                    {
                        for (int i = 0; i < seLabels.Length; i++)
                            if (ImGui.Selectable(seLabels[i], i == curIdx))
                            { obs.SoundEffectId = seValues[i]; plugin.SaveCharData(); }
                        ImGui.EndCombo();
                    }
                    ImGui.SameLine(0, 8f);
                    ImGui.PushStyleColor(ImGuiCol.Button, accent with { W = 0.25f });
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, accent with { W = 0.40f });
                    ImGui.PushStyleColor(ImGuiCol.Text, textPri);
                    if (ImGui.Button("Test##se", new Vector2(50f, 0f)))
                    {
                        try
                        {
                            unsafe
                            {
                                var addon = FFXIVClientStructs.FFXIV.Component.GUI.AtkStage.Instance()
                                    ->RaptureAtkUnitManager->GetAddonByName("_ToDoList");
                                if (addon != null) addon->PlaySoundEffect(obs.SoundEffectId);
                            }
                        }
                        catch { }
                    }
                    ImGui.PopStyleColor(3);
                }

                ImGui.Spacing(); ImGui.Spacing();

                // 2. COLOR THEME
                DrawSectionTitle("Color Theme", W, textDim);
                ImGui.Spacing();
                DrawSwatchRow(NightThemes, NightSwatches, "Night", W, textDim);
                ImGui.Spacing();
                DrawSwatchRow(DayThemes, DaySwatches, "Day", W, textDim);

                ImGui.Spacing(); ImGui.Spacing();

                // 3. TRACKED EMOTES (moved up, min 5 rows)
                DrawSectionTitle("Tracked Emotes", W, textDim);
                ImGui.Spacing();
                string l1 = "Tracked emotes appear in the Social tab log.";
                string l2 = "All emotes affect your pet and appear in Recent Events.";
                ImGui.SetCursorPosX((W - ImGui.CalcTextSize(l1).X) / 2f); ImGui.TextColored(textDim, l1);
                ImGui.SetCursorPosX((W - ImGui.CalcTextSize(l2).X) / 2f); ImGui.TextColored(textDim with { W = 0.60f }, l2);
                ImGui.Spacing();

                // Search above list
                LoadEmotes();
                ImGui.SetNextItemWidth(W - 84f);
                ImGui.InputTextWithHint("##EmoteSearch", "Search to add an emote...", ref emoteSearch, 64);
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, accent with { W = 0.25f });
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, accent with { W = 0.40f });
                ImGui.PushStyleColor(ImGuiCol.Text, textPri);
                if (ImGui.Button("+ Add##emote", new Vector2(68f, 24f)))
                {
                    string trimmed = emoteSearch.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !plugin.Config.TrackedEmotes.Contains(trimmed))
                    { plugin.Config.TrackedEmotes.Add(trimmed); emoteSearch = ""; plugin.Config.Save(); }
                }
                ImGui.PopStyleColor(3);

                // Autocomplete
                if (!string.IsNullOrEmpty(emoteSearch))
                {
                    var sugs = allEmotes.Where(e => e.Contains(emoteSearch, StringComparison.OrdinalIgnoreCase) && !plugin.Config.TrackedEmotes.Contains(e)).Take(6).ToList();
                    if (sugs.Any())
                    {
                        ImGui.PushStyleColor(ImGuiCol.ChildBg, PanelColor);
                        if (ImGui.BeginChild("##Sug", new Vector2(W - 84f, sugs.Count * 22f + 6f), false))
                        {
                            foreach (var s in sugs)
                                if (ImGui.Selectable(s) && !plugin.Config.TrackedEmotes.Contains(s))
                                { plugin.Config.TrackedEmotes.Add(s); plugin.Config.Save(); emoteSearch = ""; }
                        }
                        ImGui.EndChild();
                        ImGui.PopStyleColor();
                    }
                }

                ImGui.Spacing();
                // Min 5 rows tall
                const float rowH = 32f;
                float listH = Math.Max(5 * rowH + 20f, plugin.Config.TrackedEmotes.Count * rowH + 20f);
                listH = Math.Min(listH, 220f); // cap at ~7 rows before scrolling

                ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
                if (ImGui.BeginChild("##EmoteList", new Vector2(0, listH), false))
                {
                    string? toRemove = null;
                    foreach (var emote in plugin.Config.TrackedEmotes)
                        DrawEmoteRow(emote, ref toRemove, accent, textPri);
                    if (toRemove != null) { plugin.Config.TrackedEmotes.Remove(toRemove); plugin.Config.Save(); }
                    if (!plugin.Config.TrackedEmotes.Any())
                    {
                        ImGui.SetCursorPosX((W - ImGui.CalcTextSize("No emotes tracked yet. Add one above!").X) / 2f);
                        ImGui.TextColored(textDim, "No emotes tracked yet. Add one above!");
                    }
                }
                ImGui.EndChild();
                ImGui.PopStyleColor();

                ImGui.Spacing(); ImGui.Spacing();

                // 4. UI SCALE
                DrawSectionTitle("UI Scale", W, textDim);
                ImGui.Spacing();
                float scaleW = Math.Min(300f, W * 0.6f);
                ImGui.SetCursorPosX((W - scaleW - 60f) / 2f);
                ImGui.SetNextItemWidth(scaleW);
                float scale = plugin.Config.FontScale;
                if (ImGui.SliderFloat("##FontScale", ref scale, 0.7f, 1.5f, "%.2fx"))
                { plugin.Config.FontScale = scale; plugin.Config.Save(); }

                ImGui.Spacing(); ImGui.Spacing();

                // 5. PET
                DrawSectionTitle("Pet", W, textDim);
                ImGui.Spacing();
                if (plugin.PetManager.Pet != null && plugin.PetManager.Pet.IsAlive)
                {
                    float petIndent = (W - 320f) / 2f;
                    ImGui.SetCursorPosX(petIndent);
                    ImGui.TextColored(textDim, "Rename your pet:");
                    ImGui.SameLine();
                    string petName = plugin.PetManager.Pet.Name;
                    ImGui.SetNextItemWidth(170f);
                    if (ImGui.InputText("##PetRename", ref petName, 32))
                    { plugin.PetManager.Pet.Name = petName; plugin.SaveCharData(); }
                    ImGui.Spacing();
                    ImGui.SetCursorPosX((W - 140f) / 2f);
                    ImGui.PushStyleColor(ImGuiCol.Button, RedSoft with { W = 0.18f });
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RedSoft with { W = 0.34f });
                    ImGui.PushStyleColor(ImGuiCol.Text, RedSoft);
                    if (ImGui.Button("Release Pet", new Vector2(140f, 26f)))
                        plugin.PetManager.KillPet("Released");
                    ImGui.PopStyleColor(3);
                }
                else
                {
                    ImGui.SetCursorPosX((W - ImGui.CalcTextSize("Hatch a new pet from the Pet tab.").X) / 2f);
                    ImGui.TextColored(textDim, "Hatch a new pet from the Pet tab.");
                }

                ImGui.Spacing(); ImGui.Spacing();

                // 6. CLOUD SYNC
                DrawSectionTitle("Cloud Sync (Optional)", W, textDim);
                ImGui.Spacing();
                DrawCloudSyncSection(W, accent, textPri, textDim);

                ImGui.Spacing(); ImGui.Spacing();

                // 7. EASTER EGGS / REDEMPTION CODES
                DrawSectionTitle("Easter Eggs", W, textDim);
                ImGui.Spacing();
                DrawRedemptionSection(W, accent, textPri, textDim);

                ImGui.Dummy(new Vector2(W, 16f));
                dl0.PopClipRect();
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        private void DrawCloudSyncSection(float W, Vector4 accent, Vector4 textPri, Vector4 textDim)
        {
            bool enabled = plugin.Config.EnzirionEnabled;
            ImGui.SetCursorPosX((W - ImGui.CalcTextSize("Sync your stats and pet history to enzirion.com").X) / 2f);
            ImGui.TextColored(textDim, "Sync your stats and pet history to enzirion.com");
            ImGui.Spacing();
            ImGui.SetCursorPosX((W - ImGui.CalcTextSize("Get your plugin token at: enzirion.com/plugin-token").X) / 2f);
            ImGui.TextColored(textDim with { W = 0.60f }, "Get your plugin token at: enzirion.com/plugin-token");
            ImGui.Spacing();
            DrawToggleRow("Enable cloud sync", "Saves lifetime stats + pet history online", ref enabled, TextColor, TextDimColor, accent);
            if (enabled != plugin.Config.EnzirionEnabled) { plugin.Config.EnzirionEnabled = enabled; plugin.Config.Save(); }
            ImGui.Spacing();

            // Always rendered (no conditional) — greyed when disabled
            float fieldW = 280f, indentX = (W - fieldW - 110f) / 2f;
            if (!enabled) ImGui.PushStyleColor(ImGuiCol.Text, TextDimColor with { W = 0.35f });
            ImGui.SetCursorPosX(indentX);
            ImGui.TextColored(enabled ? textDim : TextDimColor with { W = 0.35f }, "Character:");
            ImGui.SameLine(); ImGui.SetNextItemWidth(fieldW);
            if (ImGui.InputText("##EnzChar", ref charBuf, 64) && enabled)
            { plugin.Config.EnzirionCharacterName = charBuf; plugin.Config.Save(); }
            ImGui.Spacing();
            ImGui.SetCursorPosX(indentX);
            ImGui.TextColored(enabled ? textDim : TextDimColor with { W = 0.35f }, "Token:    ");
            ImGui.SameLine(); ImGui.SetNextItemWidth(fieldW);
            if (ImGui.InputText("##EnzToken", ref tokenBuf, 256, ImGuiInputTextFlags.Password) && enabled)
            { plugin.Config.EnzirionToken = tokenBuf; plugin.Config.Save(); }
            if (!enabled) ImGui.PopStyleColor();

            ImGui.Spacing();
            bool canSync = enabled && !string.IsNullOrWhiteSpace(tokenBuf) && !string.IsNullOrWhiteSpace(charBuf);
            float btnW = 160f;
            ImGui.SetCursorPosX((W - btnW) / 2f);
            ImGui.PushStyleColor(ImGuiCol.Button, canSync ? accent with { W = 0.25f } : new Vector4(0.2f, 0.15f, 0.3f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, canSync ? accent with { W = 0.40f } : new Vector4(0.2f, 0.15f, 0.3f, 0.40f));
            ImGui.PushStyleColor(ImGuiCol.Text, canSync ? textPri : TextDimColor with { W = 0.35f });
            if (ImGui.Button("Sync Now##cloud", new Vector2(btnW, 28f)) && canSync)
            { plugin.Config.LastSyncTime = DateTime.Now; plugin.Config.LastSyncStatus = "Pending implementation"; plugin.Config.Save(); }
            ImGui.PopStyleColor(3);

            ImGui.Spacing();
            string statusStr = plugin.Config.LastSyncTime != DateTime.MinValue
                ? $"{plugin.Config.LastSyncStatus}  ·  {plugin.Config.LastSyncTime:h:mm tt}"
                : (!enabled ? "Enable sync and enter your token to get started." : "");
            if (!string.IsNullOrEmpty(statusStr))
            {
                ImGui.SetCursorPosX((W - ImGui.CalcTextSize(statusStr).X) / 2f);
                ImGui.TextColored(textDim with { W = 0.60f }, statusStr);
            }
        }

        private void DrawSwatchRow(string[] themes, Vector4[] swatches, string label, float W, Vector4 textDim)
        {
            float swSize = 32f, swGap = 14f;
            float rowW = themes.Length * swSize + (themes.Length - 1) * swGap;
            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            var origin = ImGui.GetCursorScreenPos();
            var localOrigin = ImGui.GetCursorPos();
            string rowLabel = $"{label}:";
            float labelW = ImGui.CalcTextSize(rowLabel).X + 10f;
            dl.AddText(new Vector2(origin.X, origin.Y + (swSize - ImGui.GetTextLineHeight()) / 2f), MementoColors.ToU32(textDim with { W = 0.75f }), rowLabel);
            float startX = origin.X + labelW + (W - labelW - rowW) / 2f;
            for (int i = 0; i < themes.Length; i++)
            {
                var center = new Vector2(startX + i * (swSize + swGap) + swSize / 2f, origin.Y + swSize / 2f);
                bool active = plugin.Config.Theme == themes[i];
                dl.AddCircleFilled(center, swSize / 2f + 1.5f, MementoColors.ToU32(new Vector4(0, 0, 0, 0.25f)));
                dl.AddCircleFilled(center, swSize / 2f, MementoColors.ToU32(swatches[i]));
                if (active) { dl.AddCircle(center, swSize / 2f + 3.5f, MementoColors.ToU32(new Vector4(1f, 1f, 1f, 0.90f)), 0, 2f); dl.AddCircleFilled(center, 5.5f, MementoColors.ToU32(new Vector4(1f, 1f, 1f, 0.95f))); }
                ImGui.SetCursorScreenPos(new Vector2(center.X - swSize / 2f, origin.Y));
                if (ImGui.InvisibleButton($"##sw_{themes[i]}", new Vector2(swSize, swSize))) { plugin.Config.Theme = themes[i]; plugin.Config.Save(); }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip(themes[i]);
            }
            ImGui.SetCursorPos(new Vector2(localOrigin.X, localOrigin.Y + swSize + 6f));
            ImGui.Dummy(new Vector2(1, 1));
        }

        private void DrawEmoteRow(string emoteName, ref string? toRemove, Vector4 accent, Vector4 textPri)
        {
            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            var localPos = ImGui.GetCursorPos();
            float w = ImGui.GetContentRegionAvail().X, h = 30f;
            dl.AddRectFilled(pos, pos + new Vector2(w, h), MementoColors.ToU32(PanelColor), 6f);
            dl.AddRect(pos, pos + new Vector2(w, h), MementoColors.ToU32(accent with { W = 0.25f }), 6f, ImDrawFlags.None, 0.8f);
            dl.AddText(new Vector2(pos.X + 8f, pos.Y + 8f), MementoColors.ToU32(TextDimColor with { W = 0.40f }), "⠿");
            dl.AddText(new Vector2(pos.X + 26f, pos.Y + 8f), MementoColors.ToU32(TextColor), emoteName);
            var (wText, wColor) = GetWeightInfo(emoteName);
            float tagX = pos.X + 26f + ImGui.CalcTextSize(emoteName).X + 10f;
            var tagSz = ImGui.CalcTextSize(wText);
            var tagTL = new Vector2(tagX, pos.Y + 6f); var tagBR = new Vector2(tagX + tagSz.X + 12f, pos.Y + h - 6f);
            dl.AddRectFilled(tagTL, tagBR, MementoColors.ToU32(wColor with { W = 0.15f }), 8f);
            dl.AddRect(tagTL, tagBR, MementoColors.ToU32(wColor with { W = 0.35f }), 8f, ImDrawFlags.None, 0.7f);
            dl.AddText(tagTL + new Vector2(6f, (h - 12f - tagSz.Y) / 2f), MementoColors.ToU32(wColor), wText);
            ImGui.SetCursorScreenPos(new Vector2(pos.X + w - 28f, pos.Y + 5f));
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero); ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RedSoft with { W = 0.20f }); ImGui.PushStyleColor(ImGuiCol.Text, RedSoft);
            if (ImGui.Button($"x##rm_{emoteName}", new Vector2(22f, 20f))) toRemove = emoteName;
            ImGui.PopStyleColor(3);
            ImGui.SetCursorPos(new Vector2(localPos.X, localPos.Y + h));
            ImGui.Spacing();
        }

        private (string text, Vector4 color) GetWeightInfo(string emote) => emote switch
        {
            "Dote" => ("+3 Love", MementoColors.Pink),
            "Embrace" => ("+2 Health", MementoColors.Purple5),
            "Pet" => ("+2 Love", MementoColors.Pink),
            "Blow Kiss" => ("+2 Love", MementoColors.Pink),
            "Flower Shower" => ("+2 Joy", MementoColors.Teal),
            "Furious" => ("-2 Joy", RedSoft),
            "Slap" => ("-1 Joy", RedSoft),
            _ => ("+1 Joy", MementoColors.Purple5),
        };

        private void DrawToggleRow(string label, string sublabel, ref bool value, Vector4 textPri, Vector4 textDim, Vector4 accent)
        {
            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            float w = ImGui.GetContentRegionAvail().X;
            var pos = ImGui.GetCursorScreenPos();
            var localPos = ImGui.GetCursorPos();
            float h = 40f, toggleW = 40f, toggleH = 20f;
            var togglePos = new Vector2(pos.X + w - toggleW - 8f, pos.Y + (h - toggleH) / 2f);
            dl.AddText(new Vector2(pos.X + 6f, pos.Y + 8f), MementoColors.ToU32(textPri), label);
            dl.AddText(new Vector2(pos.X + 6f, pos.Y + 24f), MementoColors.ToU32(textDim with { W = 0.70f }), sublabel);
            dl.AddRectFilled(togglePos, togglePos + new Vector2(toggleW, toggleH), MementoColors.ToU32(value ? accent : new Vector4(0.30f, 0.30f, 0.40f, 0.45f)), 10f);
            float thumbX = value ? togglePos.X + toggleW - toggleH + 2f : togglePos.X + 2f;
            dl.AddCircleFilled(new Vector2(thumbX + toggleH / 2f - 2f, togglePos.Y + toggleH / 2f), toggleH / 2f - 2f, MementoColors.ToU32(new Vector4(1f, 1f, 1f, 0.95f)));
            ImGui.Dummy(new Vector2(w, h));
            ImGui.SetCursorScreenPos(togglePos);
            if (ImGui.InvisibleButton($"##tog_{label}", new Vector2(toggleW, toggleH))) value = !value;
            ImGui.SetCursorPos(new Vector2(localPos.X, localPos.Y + h + 2f));
        }

        private void DrawRedemptionSection(float W, Vector4 accent, Vector4 textPri, Vector4 textDim)
        {
            if (codeMsgTimer > 0f) codeMsgTimer -= ImGui.GetIO().DeltaTime;
            if (codeMsgTimer <= 0f) codeMessage = "";

            ImGui.TextColored(textDim with { W = 0.70f }, "Enter a special code to unlock cosmetics and features.");
            ImGui.Spacing();

            ImGui.SetNextItemWidth(W - 100f);
            bool entered = ImGui.InputText("##code", ref codeInput, 48, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.SameLine(0, 8f);
            ImGui.PushStyleColor(ImGuiCol.Button, accent with { W = 0.25f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, accent with { W = 0.45f });
            ImGui.PushStyleColor(ImGuiCol.Text, textPri);
            bool pressed = ImGui.Button("Redeem##code", new Vector2(80f, 0f));
            ImGui.PopStyleColor(3);

            if ((pressed || entered) && !string.IsNullOrWhiteSpace(codeInput))
                TryRedeemCode(codeInput.Trim().ToUpperInvariant());

            if (!string.IsNullOrEmpty(codeMessage))
            {
                ImGui.Spacing();
                ImGui.TextColored(codeMessage.StartsWith("✓") ? MementoColors.Green : MementoColors.Red, codeMessage);
            }

            // Active unlocks with revoke
            bool anyUnlock = plugin.Config.UnlockedRgbCat || plugin.Config.UnlockedSecretEgg || plugin.Config.DevUnlocked;
            if (anyUnlock)
            {
                ImGui.Spacing();
                ImGui.TextColored(textDim, "Active unlocks:"); ImGui.Spacing();

                if (plugin.Config.DevUnlocked)
                    DrawUnlockRow("Dev Tab", MementoColors.Purple5, "Unlocks the sandbox Dev tab", () => {
                        plugin.Config.DevUnlocked = false;
                        plugin.Config.RedeemedCodes.Remove("XENADEV2026");
                        plugin.Config.RedeemedCodes.Remove("MEMENTODEBUG");
                        plugin.Config.Save();
                    }, W);

                if (plugin.Config.UnlockedRgbCat)
                    DrawUnlockRow("RGB Fat Cat", MementoColors.Gold, "Available as hatching option", () => {
                        plugin.Config.UnlockedRgbCat = false;
                        plugin.Config.RedeemedCodes.Remove("RGBFATCAT");
                        plugin.Config.RedeemedCodes.Remove("PURRSEIPHONE");
                        plugin.Config.Save();
                    }, W);

                if (plugin.Config.UnlockedSecretEgg)
                    DrawUnlockRow("Secret Egg", MementoColors.Teal, "Mystery pet at next hatch", () => {
                        plugin.Config.UnlockedSecretEgg = false;
                        plugin.Config.RedeemedCodes.Remove("SECRETEGG");
                        plugin.Config.Save();
                    }, W);
            }
        }

        private void DrawUnlockRow(string name, Vector4 color, string desc, System.Action onRevoke, float W)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            var localPos = ImGui.GetCursorPos();
            float h = 32f;
            dl.AddRectFilled(pos, pos + new Vector2(W, h), MementoColors.ToU32(color, 0.08f), 6f);
            dl.AddRect(pos, pos + new Vector2(W, h), MementoColors.ToU32(color, 0.30f), 6f, ImDrawFlags.None, 0.8f);
            dl.AddText(pos + new Vector2(10f, 8f), MementoColors.ToU32(color), name);
            float nameW = ImGui.CalcTextSize(name).X;
            dl.AddText(pos + new Vector2(10f + nameW + 12f, 10f), MementoColors.ToU32(MementoColors.Dim, 0.65f), desc);
            float btnW = 64f;
            ImGui.SetCursorScreenPos(new Vector2(pos.X + W - btnW - 6f, pos.Y + 5f));
            ImGui.PushStyleColor(ImGuiCol.Button, MementoColors.Red with { W = 0.15f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, MementoColors.Red with { W = 0.30f });
            ImGui.PushStyleColor(ImGuiCol.Text, MementoColors.Red);
            if (ImGui.Button($"Revoke##{name}", new Vector2(btnW, 22f))) onRevoke();
            ImGui.PopStyleColor(3);
            ImGui.SetCursorPos(new Vector2(localPos.X, localPos.Y + h));
            ImGui.Spacing();
        }

        private void TryRedeemCode(string code)
        {
            if (plugin.Config.RedeemedCodes.Contains(code))
            {
                codeMessage = "✕ Already redeemed.";
                codeMsgTimer = 3f;
                codeInput = "";
                return;
            }

            // ── Code registry ────────────────────────────────────────────
            // Dev tab unlock — static password for now, will be rotated via DB
            bool success = false;
            switch (code)
            {
                case "XENADEV2026":
                case "MEMENTODEBUG":
                    plugin.Config.DevUnlocked = true;
                    codeMessage = "✓ Dev Tab unlocked! Check the tab bar.";
                    success = true;
                    break;

                case "RGBFATCAT":
                case "PURRSEIPHONE":
                    plugin.Config.UnlockedRgbCat = true;
                    codeMessage = "✓ RGB Fat Cat unlocked! Available at next hatch.";
                    success = true;
                    break;

                case "SECRETEGG":
                    plugin.Config.UnlockedSecretEgg = true;
                    codeMessage = "✓ Secret Egg unlocked — mystery pet incoming!";
                    success = true;
                    break;

                default:
                    codeMessage = "✕ Invalid code. Codes are case-insensitive.";
                    codeMsgTimer = 3f;
                    codeInput = "";
                    return;
            }

            if (success)
            {
                plugin.Config.RedeemedCodes.Add(code);
                plugin.Config.Save();
                codeMsgTimer = 5f;
                codeInput = "";
            }
        }

        private void DrawSectionTitle(string title, float W, Vector4 textDim)
        {
            ImDrawListPtr dl = ImGui.GetWindowDrawList(); var pos = ImGui.GetCursorScreenPos();
            float lh = ImGui.GetTextLineHeight(); var sz = ImGui.CalcTextSize(title.ToUpper());
            float tx = pos.X + (W - sz.X) / 2f;
            dl.AddText(new Vector2(tx, pos.Y), MementoColors.ToU32(textDim), title.ToUpper());
            float ly = pos.Y + lh / 2f; uint lc = MementoColors.ToU32(textDim with { W = 0.20f });
            dl.AddLine(new Vector2(pos.X, ly), new Vector2(tx - 10f, ly), lc, 1f);
            dl.AddLine(new Vector2(tx + sz.X + 10f, ly), new Vector2(pos.X + W, ly), lc, 1f);
            ImGui.Dummy(new Vector2(W, lh + 2f));
        }

        private void DrawSettingsToggle(string label, string sub, ref bool value, float w)
        {
            var dl = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            var localPos = ImGui.GetCursorPos();
            float h = 44f, togW = 32f, togH = 18f;

            dl.AddRectFilled(pos, pos + new Vector2(w, h), MementoColors.ToU32(MementoColors.Panel2), 8f);
            dl.AddRect(pos, pos + new Vector2(w, h), MementoColors.ToU32(MementoColors.Border), 8f, ImDrawFlags.None, 0.8f);

            // Truncate label to fit
            string lbl = label;
            float maxLblW = w - togW - 24f;
            while (lbl.Length > 4 && ImGui.CalcTextSize(lbl).X > maxLblW) lbl = lbl[..^1];

            dl.AddText(new Vector2(pos.X + 10f, pos.Y + 8f), MementoColors.ToU32(MementoColors.Text), lbl);
            dl.AddText(new Vector2(pos.X + 10f, pos.Y + 24f), MementoColors.ToU32(MementoColors.Dim with { W = 0.65f }), sub.Length > 22 ? sub[..22] + "…" : sub);

            var togPos = new Vector2(pos.X + w - togW - 8f, pos.Y + (h - togH) / 2f);
            dl.AddRectFilled(togPos, togPos + new Vector2(togW, togH),
                MementoColors.ToU32(value ? MementoColors.Purple5 : new Vector4(0.30f, 0.30f, 0.40f, 0.45f)), 10f);
            float thumbX = value ? togPos.X + togW - togH + 2f : togPos.X + 2f;
            dl.AddCircleFilled(new Vector2(thumbX + togH / 2f - 2f, togPos.Y + togH / 2f),
                togH / 2f - 2f, MementoColors.ToU32(new Vector4(1f, 1f, 1f, 0.95f)));

            ImGui.Dummy(new Vector2(w, h));
            ImGui.SetCursorScreenPos(togPos);
            if (ImGui.InvisibleButton($"##obstog_{label}", new Vector2(togW, togH)))
            { value = !value; }
            ImGui.SetCursorPos(new Vector2(localPos.X, localPos.Y + h));
        }

        private void LoadEmotes()
        {
            if (allEmotes.Count > 0) return;
            allEmotes.AddRange(new[] { "Dote", "Embrace", "Pet", "Blow Kiss", "Hug", "Beckon", "Cheer", "Clap", "Bow", "Wave", "Wink", "Thumbsup", "Thumbsdown", "Amazed", "Angry", "Blush", "Cry", "Comfort", "Doze", "Furious", "Grin", "Happy", "Kneel", "Laugh", "Poke", "Pray", "Salute", "Scared", "Sit", "Slap", "Soothe", "Surprised", "Throw", "Upset", "Flower Shower", "Joy", "Sulk", "Grovel", "Fume", "Panic", "Shrug", "Shiver", "Deride", "Doubt", "Taunt", "Psych", "Haunt", "Braver", "Lookout" });
        }
    }
}
