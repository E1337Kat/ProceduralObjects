﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ColossalFramework.PlatformServices;
using System.IO;
using UnityEngine;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using ProceduralObjects.UI;

namespace ProceduralObjects.ProceduralText
{
    public class FontManager
    {
        public FontManager()
        {
            m_fonts = new List<TextureFont>();
            LoadFonts();
            arial = m_fonts.FirstOrDefault(font => font.m_fontName.ToLower() == "arial");
            window = new Rect(555, 200, 400, 395);
            scrollFonts = Vector2.zero;
            previewTex = null;
            previewField = "";
        }
        public TextureFont Arial
        {
            get { return arial; }
        }
        public static FontManager instance;

        private TextureFont arial;
        public List<TextureFont> m_fonts;
        public bool showWindow = false;
        private Rect window;
        private Vector2 scrollFonts;
        private TextureFont selectedFont;
        private Texture2D previewTex;
        private string previewField;
        
        public void DrawWindow()
        {
            if (showWindow)
                window = GUIUtils.ClampRectToScreen(GUIUtils.Window(953011083, window, draw, LocalizationManager.instance.current["font_management"]));
        }
        private void draw(int id)
        {
            if (GUIUtils.CloseHelpButtons(window, "Font_Management"))
                showWindow = false;
            GUI.DragWindow(new Rect(0, 0, 344, 23));

            GUI.Label(new Rect(10, 25, 380, 25), "<b><size=13>" + LocalizationManager.instance.current["installed_fonts"] + " :</size></b>");

            GUI.Box(new Rect(10, 52, 355, 200), string.Empty);
            scrollFonts = GUI.BeginScrollView(new Rect(12, 54, 376, 196), scrollFonts, new Rect(0, 0, 354, m_fonts.Count * 28 + 2));
            for (int i = 0; i < m_fonts.Count; i++)
            {
                var font = m_fonts[i];
                if (GUI.Button(new Rect(0, i * 28, 352, 23), "<b>" + font.m_fontName + "</b> (" + LocalizationManager.instance.current[font.file_id == PublishedFileId.invalid ?
                    "local" : "from_wk"] + ")"))
                {
                    ProceduralObjectsLogic.PlaySound();
                    selectedFont = font;
                    previewField = font.m_fontName;
                    previewTex = GetPreviewTex();
                }
            }
            GUI.EndScrollView();

            GUI.BeginGroup(new Rect(10, 258, 380, 130));
            GUI.Box(new Rect(0, 0, 380, 130), selectedFont == null ? LocalizationManager.instance.current["no_font_selected"] : string.Empty);
            if (selectedFont != null)
            {
                GUI.Label(new Rect(2, 2, 376, 27), "<size=14>" + selectedFont.m_fontName + "</size> (" + LocalizationManager.instance.current[selectedFont.file_id == PublishedFileId.invalid ?
                    "local" : "from_wk"] + ")");

                if (GUI.Button(new Rect(2, 30, 124, 25), LocalizationManager.instance.current["refresh"]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    LoadSingleFont(selectedFont.m_path, selectedFont);
                    previewTex = GetPreviewTex();
                }
                if (GUI.Button(new Rect(128, 30, 124, 25), LocalizationManager.instance.current["open_font_folder"]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    var dir = Path.GetDirectoryName(selectedFont.m_path);
                    if (Directory.Exists(dir))
                        Application.OpenURL("file://" + dir);
                }
                if (selectedFont.file_id != PublishedFileId.invalid)
                {
                    if (GUI.Button(new Rect(254, 30, 124, 25), LocalizationManager.instance.current["show_font_wk"]))
                    {
                        ProceduralObjectsLogic.PlaySound();
                        PlatformService.ActivateGameOverlayToWorkshopItem(selectedFont.file_id);
                    }
                }

                var newTextField = GUI.TextField(new Rect(3, 60, 220, 22), previewField);
                if (previewField != newTextField)
                {
                    previewField = newTextField;
                    previewTex = GetPreviewTex();
                }

                GUI.DrawTexture(new Rect(3, 85, 374, 42), previewTex, ScaleMode.ScaleToFit);
            }
            GUI.EndGroup();
        }

        public void SetPosition(float x, float y)
        {
            selectedFont = null;
            previewTex = null;
            previewField = "";
            scrollFonts = Vector2.zero;
            window.position = new Vector2(x, y);
        }
        private Texture2D GetPreviewTex()
        {
            var tex = selectedFont.GetString(previewField, FontStyle.Normal, selectedFont.m_defaultSpacing);
         //   if (selectedFont.m_charSize != 50)
         //       TextureScale.Bilinear(tex, (int)(tex.width * 50 / selectedFont.m_charSize), (int)50);
            return tex;
        }

        public TextureFont GetNextFont(TextureFont f)
        {
            if (m_fonts == null)
                return arial;
            if (!m_fonts.Contains(f))
                return arial;
            if (nextFontExists(f))
                return m_fonts[m_fonts.IndexOf(f) + 1];
            return arial;
        }
        public TextureFont GetPreviousFont(TextureFont f)
        {
            if (m_fonts == null)
                return arial;
            if (!m_fonts.Contains(f))
                return arial;
            if (previousFontExists(f))
                return m_fonts[m_fonts.IndexOf(f) - 1];
            return arial;
        }
        public bool previousFontExists(TextureFont f)
        {
            if (m_fonts.IndexOf(f) == 0)
                return false;
            return true;
        }
        public bool nextFontExists(TextureFont f)
        {
            if (m_fonts.IndexOf(f) == m_fonts.Count - 1)
                return false;
            return true;
        }

        public void LoadFonts()
        {
            m_fonts = new List<TextureFont>();
            if (!Directory.Exists(ProceduralObjectsMod.FontsPath))
            {
                Directory.CreateDirectory(ProceduralObjectsMod.FontsPath);
                Debug.Log("[ProceduralObjects] Local fonts path did not exist. Created directory and skipped.");
            }
            else
            {
                foreach (string file in Directory.GetFiles(ProceduralObjectsMod.FontsPath, "*.pofont", SearchOption.AllDirectories))
                {
                    TextureFont _font = new TextureFont(file);
                    if (LoadSingleFont(file, _font))
                    {
                        _font.file_id = PublishedFileId.invalid;
                        m_fonts.Add(_font);
                    }
                }
            }
            foreach (PublishedFileId fileId in PlatformService.workshop.GetSubscribedItems())
            {
                string path = PlatformService.workshop.GetSubscribedItemPath(fileId);
                if (!Directory.Exists(path))
                    continue;
                var wkFiles = Directory.GetFiles(path, "*.pofont", SearchOption.AllDirectories);
                if (wkFiles.Any())
                {
                    for (int i = 0; i < wkFiles.Count(); i++)
                    {
                        if (!File.Exists(wkFiles[i]))
                            continue;
                        TextureFont _font = new TextureFont(wkFiles[i]);
                        if (LoadSingleFont(wkFiles[i], _font))
                        {
                            _font.file_id = fileId;
                            m_fonts.Add(_font);
                        }
                    }
                }
            }
        }
        public bool LoadSingleFont(string infoFilePath, TextureFont font)
        {
            try
            {
                string dir = Path.GetDirectoryName(infoFilePath);

                string[] lines = File.ReadAllLines(infoFilePath);
                string name = "", normal = "", italic = "", bold = "", orderedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzàáäåâãìíïîòóöøôõœèéëêùúü-ûð&ÿ'ñßç,!.?:0123456789+=%µ#§€$£;<>\"°@(){}/\\_*[]¤^²|~";
                uint spaceWidth = 0, defaultSpacing = 2, charSize = 50;
                bool disableOverwriting = false;
                var kerningNormal = new Dictionary<Vector2, int>();
                var kerningItalic = new Dictionary<Vector2, int>();
                var kerningBold = new Dictionary<Vector2, int>();
                string[] stylesNames = null;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("name = "))
                        name = lines[i].Replace("name = ", "");
                    else if (lines[i].Contains("normal = "))
                        normal = lines[i].Replace("normal = ", "");
                    else if (lines[i].Contains("italic = "))
                        italic = lines[i].Replace("italic = ", "");
                    else if (lines[i].Contains("bold = "))
                        bold = lines[i].Replace("bold = ", "");
                    else if (lines[i].Contains("spaceWidth = "))
                        spaceWidth = uint.Parse(lines[i].Replace("spaceWidth = ", "").Trim());
                    else if (lines[i].Contains("defaultSpacing = "))
                        defaultSpacing = uint.Parse(lines[i].Replace("defaultSpacing = ", "").Trim());
                    else if (lines[i].Contains("charSize = "))
                        charSize = uint.Parse(lines[i].Replace("charSize = ", "").Trim());
                    else if (lines[i].Contains("characters = "))
                    {
                        orderedChars = "";
                        var charUnicodes = lines[i].Replace("characters = ", "").Trim().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                        for (int j = 0; j < charUnicodes.Length; j++)
                        {
                            orderedChars += "\\u" + charUnicodes[j];
                        }
                        orderedChars = Regex.Unescape(orderedChars);
                    }
                    else if (lines[i].Contains("stylesNames = "))
                    {
                        stylesNames = lines[i].GetStringAfter(" = ").Split(new string[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    else if (lines[i].Contains("krng"))
                    {
                        // kerning
                        string[] chars = lines[i].GetStringBetween("(", ")").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        Vector2 charIndexes = new Vector2(int.Parse(chars[0]), int.Parse(chars[1]));
                        if (lines[i].Contains("krngNormal("))
                            kerningNormal[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                        else if (lines[i].Contains("krngItalic("))
                            kerningItalic[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                        else if (lines[i].Contains("krngBold("))
                            kerningBold[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                        else if (lines[i].Contains("krngNormalItalic("))
                        {
                            kerningNormal[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                            kerningItalic[charIndexes] = kerningNormal[charIndexes];
                        }
                        else if (lines[i].Contains("krngNormalItalicBold("))
                        {
                            kerningNormal[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                            kerningItalic[charIndexes] = kerningNormal[charIndexes];
                            kerningBold[charIndexes] = kerningNormal[charIndexes];
                        }
                        else if (lines[i].Contains("krngNormalBold("))
                        {
                            kerningNormal[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                            kerningBold[charIndexes] = kerningNormal[charIndexes];
                        }
                    }
                    else if (lines[i].Replace(" ", "").ToLower() == "disablecoloroverwriting=true")
                        disableOverwriting = true;
                }
                if (normal == "")
                {
                    Debug.LogError("[ProceduralObjects] Font error : No normal style specified for a font, couldn't create it. Occured at path " + infoFilePath);
                    return false;
                }
                Texture2D normaltex = null, italictex = null, boldtex = null;
                string normalpath = Path.Combine(dir, normal) + ".png";
                if (!File.Exists(normalpath))
                {
                    Debug.LogError("[ProceduralObjects] Font error : Normal style for a font does not exist at the specified path, couldn't create it. Occured at path " + infoFilePath);
                    return false;
                }
                else
                    normaltex = TextureUtils.LoadPNG(normalpath);
                if (italic != "")
                {
                    string italicpath = Path.Combine(dir, italic) + ".png";
                    if (File.Exists(italicpath))
                    {
                        italictex = TextureUtils.LoadPNG(italicpath);
                    }
                }
                if (bold != "")
                {
                    string boldpath = Path.Combine(dir, bold) + ".png";
                    if (File.Exists(boldpath))
                    {
                        boldtex = TextureUtils.LoadPNG(boldpath);
                    }
                }
                if (italictex != null && boldtex != null)
                    font.Initialize(name, normaltex, italictex, boldtex, spaceWidth);
                else
                    font.Initialize(name, normaltex, spaceWidth);
                font.m_orderedChars = orderedChars;
                font.m_disableColorOverwriting = disableOverwriting;
                font.m_charSize = charSize;
                font.BuildFont();
                font.m_defaultSpacing = defaultSpacing;
                font.m_kerningNormal = kerningNormal;
                font.m_kerningItalic = kerningItalic;
                font.m_kerningBold = kerningBold;
                font.m_stylesNames = stylesNames;
                return true;
            }
            catch (Exception e)
            {
                Debug.Log("[ProceduralObjects] Unable to load a font, skipping : " + e);
                return false;
            }
        }
    }
}
