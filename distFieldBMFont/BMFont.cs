using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

/**
* @brief Extracted From tk2dFontEditor.cs 
*/
public class BMFont
{
    // Internal structures to fill and process
    public class IntChar
    {
        public int id = 0, x = 0, y = 0, width = 0, height = 0, xoffset = 0, yoffset = 0, xadvance = 0;
    };
    
    public class IntKerning
    {
        public int first = 0, second = 0, amount = 0;
    };
    
    public class IntFontInfo
    {
        public string texName;
        public int scaleW = 0, scaleH = 0;
        public int lineHeight = 0;
        
        public List<IntChar> chars = new List<IntChar>();
        public List<IntKerning> kernings = new List<IntKerning>();
    };

    static IntFontInfo ParseBMFontXml(XmlDocument doc)
    {
        IntFontInfo fontInfo = new IntFontInfo();
        
        XmlNode nodeCommon = doc.SelectSingleNode("/font/common");
        fontInfo.scaleW = ReadIntAttribute(nodeCommon, "scaleW");
        fontInfo.scaleH = ReadIntAttribute(nodeCommon, "scaleH");
        fontInfo.lineHeight = ReadIntAttribute(nodeCommon, "lineHeight");
        int pages = ReadIntAttribute(nodeCommon, "pages");
        if (pages != 1)
        {
            EditorUtility.DisplayDialog("Fatal error", "Only one page supported in font. Please change the setting and re-export.", "Ok");
            return null;
        }

        foreach (XmlNode node in doc.SelectNodes(("/font/pages/page")))
        {
            fontInfo.texName = ReadStringAttribute(node, "file");
        }

        foreach (XmlNode node in doc.SelectNodes(("/font/chars/char")))
        {
            IntChar thisChar = new IntChar();
            thisChar.id = ReadIntAttribute(node, "id");
            thisChar.x = ReadIntAttribute(node, "x");
            thisChar.y = ReadIntAttribute(node, "y");
            thisChar.width = ReadIntAttribute(node, "width");
            thisChar.height = ReadIntAttribute(node, "height");
            thisChar.xoffset = ReadIntAttribute(node, "xoffset");
            thisChar.yoffset = ReadIntAttribute(node, "yoffset");
            thisChar.xadvance = ReadIntAttribute(node, "xadvance");
            
            fontInfo.chars.Add(thisChar);
        }
        
        foreach (XmlNode node in doc.SelectNodes("/font/kernings/kerning"))
        {
            IntKerning thisKerning = new IntKerning();
            thisKerning.first = ReadIntAttribute(node, "first");
            thisKerning.second = ReadIntAttribute(node, "second");
            thisKerning.amount = ReadIntAttribute(node, "amount");
            
            fontInfo.kernings.Add(thisKerning);
        }

        return fontInfo;
    }

    static string ReadStringAttribute(XmlNode node, string attribute)
    {
        return node.Attributes[attribute].Value.Replace("\"", "");
    }

    static int ReadIntAttribute(XmlNode node, string attribute)
    {
        return int.Parse(node.Attributes[attribute].Value, System.Globalization.NumberFormatInfo.InvariantInfo);
    }

    static float ReadFloatAttribute(XmlNode node, string attribute)
    {
        return float.Parse(node.Attributes[attribute].Value, System.Globalization.NumberFormatInfo.InvariantInfo);
    }
    Vector2 ReadVector2Attributes(XmlNode node, string attributeX, string attributeY)
    {
        return new Vector2(ReadFloatAttribute(node, attributeX), ReadFloatAttribute(node, attributeY));
    }

    static string FindKeyValue(string[] tokens, string key)
    {
        string keyMatch = key + "=";
        for (int i = 0; i < tokens.Length; ++i)
        {
            if (tokens[i].Length > keyMatch.Length && tokens[i].Substring(0, keyMatch.Length) == keyMatch)
                return tokens[i].Substring(keyMatch.Length).Replace("\"", "");
        }
        
        return "";
    }
    
    static IntFontInfo ParseBMFontText(string path)
    {
        IntFontInfo fontInfo = new IntFontInfo();
        
        FileInfo finfo = new FileInfo(path);
        StreamReader reader = finfo.OpenText();
        string line;
        while ((line = reader.ReadLine()) != null) 
        {
            string[] tokens = line.Split( ' ' );
            
            if (tokens[0] == "common")
            {
                fontInfo.lineHeight = int.Parse( FindKeyValue(tokens, "lineHeight") );
                fontInfo.scaleW = int.Parse( FindKeyValue(tokens, "scaleW") );
                fontInfo.scaleH = int.Parse( FindKeyValue(tokens, "scaleH") );
                int pages = int.Parse( FindKeyValue(tokens, "pages") );
                if (pages != 1)
                {
                    EditorUtility.DisplayDialog("Fatal error", "Only one page supported in font. Please change the setting and re-export.", "Ok");
                    return null;
                }
            }
            else if (tokens[0] == "char")
            {
                IntChar thisChar = new IntChar();
                thisChar.id = int.Parse(FindKeyValue(tokens, "id"));
                thisChar.x = int.Parse(FindKeyValue(tokens, "x"));
                thisChar.y = int.Parse(FindKeyValue(tokens, "y"));
                thisChar.width = int.Parse(FindKeyValue(tokens, "width"));
                thisChar.height = int.Parse(FindKeyValue(tokens, "height"));
                thisChar.xoffset = int.Parse(FindKeyValue(tokens, "xoffset"));
                thisChar.yoffset = int.Parse(FindKeyValue(tokens, "yoffset"));
                thisChar.xadvance = int.Parse(FindKeyValue(tokens, "xadvance"));
                fontInfo.chars.Add(thisChar);
            }
            else if (tokens[0] == "kerning")
            {
                IntKerning thisKerning = new IntKerning();
                thisKerning.first = int.Parse(FindKeyValue(tokens, "first"));
                thisKerning.second = int.Parse(FindKeyValue(tokens, "second"));
                thisKerning.amount = int.Parse(FindKeyValue(tokens, "amount"));
                fontInfo.kernings.Add(thisKerning);
            }
            else if (tokens[0] == "page")
            {
                fontInfo.texName = FindKeyValue(tokens, "file");
            }
        }
        reader.Close();
        
        return fontInfo;
    }
    
    public static IntFontInfo ParseFromPath(string path)
    {
        IntFontInfo fontInfo = null;
        
        try
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            fontInfo = ParseBMFontXml(doc);
        }
        catch
        {
            fontInfo = ParseBMFontText(path);
        }
        
        if (fontInfo == null || fontInfo.chars.Count == 0)
            return null;

        return fontInfo;
    }

    public static void SaveToXML(IntFontInfo fnt, string outPath)
    {
        XmlDocument doc = new XmlDocument();
        XmlDeclaration decl = doc.CreateXmlDeclaration("1.0","utf-8",null);
        XmlElement root = doc.CreateElement("font");
        doc.InsertBefore(decl, doc.DocumentElement);
        doc.AppendChild(root);
        
        XmlElement common = doc.CreateElement("common");
        common.SetAttribute("lineHeight", fnt.lineHeight.ToString());
        common.SetAttribute("scaleW", fnt.scaleW.ToString());
        common.SetAttribute("scaleH", fnt.scaleH.ToString());
        common.SetAttribute("pages", "1");
        root.AppendChild(common);

        XmlElement pages = doc.CreateElement("pages");
        XmlElement page1 = doc.CreateElement("page");
        page1.SetAttribute("id", "0");
        page1.SetAttribute("file", fnt.texName);
        pages.AppendChild(page1);
        root.AppendChild(pages);

        XmlElement chars = doc.CreateElement("chars");
        chars.SetAttribute("count", fnt.chars.Count.ToString());
        foreach(IntChar c in fnt.chars){
            XmlElement cNode = doc.CreateElement("char");
            cNode.SetAttribute("id", c.id.ToString());
            cNode.SetAttribute("x", c.x.ToString());
            cNode.SetAttribute("y", c.y.ToString());
            cNode.SetAttribute("width", c.width.ToString());
            cNode.SetAttribute("height", c.height.ToString());
            cNode.SetAttribute("xoffset", c.xoffset.ToString());
            cNode.SetAttribute("yoffset", c.yoffset.ToString());
            cNode.SetAttribute("xadvance", c.xadvance.ToString());
            chars.AppendChild(cNode);
        }
        root.AppendChild(chars);

        XmlElement kernings = doc.CreateElement("kernings");
        kernings.SetAttribute("count", fnt.kernings.Count.ToString());
        foreach(IntKerning k in fnt.kernings){
            XmlElement kNode = doc.CreateElement("kerning");
            kNode.SetAttribute("first", k.first.ToString());
            kNode.SetAttribute("second", k.second.ToString());
            kNode.SetAttribute("amount", k.amount.ToString());
            kernings.AppendChild(kNode);
        }
        root.AppendChild(kernings);
        doc.Save(outPath);
    }
}
