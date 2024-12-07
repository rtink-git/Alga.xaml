using Microsoft.Extensions.Logging;
using System.Text;

namespace Alga.xaml;
public class Scheme {
    readonly string Doc;
    readonly ILogger? Logger;
    public List<Models.Simple> Simple { get; private set; }

    public Scheme(string doc, ILoggerFactory? loggerFactory = null) {
        this.Doc = doc ?? throw new ArgumentNullException(nameof(doc));
        Logger = loggerFactory?.CreateLogger<Scheme>();
        this.Simple = GetList();
    }

    public string GetInnerText(int descriptorIndex) {
        const string mn = $"{nameof(GetInnerText)}()";

        if (descriptorIndex < 0 || descriptorIndex >= Simple.Count) {
            Logger?.LogError($"{mn} Input param \"descriptorIndex\" is out of range.");
            return string.Empty;
        }

        var descriptor = Simple[descriptorIndex];
        if (descriptor.open != descriptorIndex) return string.Empty;

        try { 
            int start = descriptor.finish + 1;
            int length = Simple[descriptor.close].start - descriptor.finish - 1;
            return length > 0 ? Doc.Substring(start, length) : string.Empty;
        }
        catch (Exception ex) {
            Logger?.LogError($"{mn} Exception message: {ex.Message}");
            return string.Empty;
        }
    }

    public string GetInnerOnlyText(int descriptorIndex) {
        const string mn = $"{nameof(GetInnerOnlyText)}()";

        if (descriptorIndex < 0 || descriptorIndex >= Simple.Count) {
            Logger?.LogError($"{mn} Input param \"descriptorIndex\" is out of range.");
            return string.Empty;
        }

        var descriptor = Simple[descriptorIndex];
        if (descriptor.open != descriptorIndex) return string.Empty;

        var result = new StringBuilder();
        bool insideText = false;

        try {
            for (int i = descriptor.finish + 1; i < Simple[descriptor.close].start; i++) {
                char c = Doc[i];

                if (c == '<') insideText = false;
                if (insideText) result.Append(Doc[i]);
                if (c == '>') insideText = true;
            }
        } catch (Exception ex) { Logger?.LogError($"{mn} Catch. Exception: " + ex.Message); }

        return result.ToString();
    }

    // оптимизировать отдельно и еще раз
    public List<Models.Attribute> GetAttributes(int descriptorIndex) {
        const string mn = $"{nameof(GetAttributes)}()";

        var attributes = new List<Models.Attribute>();

        if (descriptorIndex < 0 || descriptorIndex >= Simple.Count) {
            Logger?.LogError($"{mn} Input param \"descriptorIndex\" is out of range.");
            return attributes;
        }

        var descriptor = Simple[descriptorIndex];
        if (descriptor.open != descriptorIndex || Doc[descriptor.start] != '<') return attributes;

        try {
            string? attrName = null;
            string? attrValue = null;

            for (int i = descriptor.start + descriptor.name.Length + 1; i < descriptor.finish; i++) {
                char c = Doc[i];

                if (attrValue == null) {
                    if (attrValue == null) {
                        if (c == '=') attrValue = string.Empty;
                        else if (char.IsWhiteSpace(c) || c == '>') {
                            if (!string.IsNullOrWhiteSpace(attrName)) {
                                attributes.Add(new Models.Attribute { name = attrName });
                                attrName = string.Empty;
                            }
                        }
                        else attrName += c;
                    }
                    else if (c == '"' || c == '\'') continue;
                }
                else
                    if (attrName != null)
                        if (Doc[i] != '\n' && Doc[i] != '>' && Doc[i] != '"') attrValue += Doc[i];
                        else if (Doc[i - 1] != '=' && Doc[i - 2] != '=') {
                            if (attrValue.Length > 0) {
                                if (attrValue[0] == '\'' || attrValue[0] == '\"') attrValue = attrValue.Substring(1);
                                if (attrValue.Length > 0)
                                    if (attrValue[attrValue.Length - 1] == '\'' || attrValue[attrValue.Length - 1] == '\"')
                                        attrValue = attrValue.Substring(0, attrValue.Length - 1);
                            }
                            attributes.Add(new Models.Attribute() { name = attrName, value = attrValue });
                            attrName = null;
                            attrValue = null;
                        }
            }
        } catch (Exception ex) { Logger?.LogError($"{mn} Catch. Exception: " + ex.Message); }

        return attributes;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="xpath">
    /// examples:
    /// h1
    /// h1[@class="produnctName"]
    /// </param>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    /// <returns></returns>
    public int FirstFoundElementIndex(string elementName= "", List<Alga.xaml.Models.Attribute>? attributes = null,  int startIndex = 0, int endIndex = int.MaxValue) {
        const string mn = $"{nameof(FirstFoundElementIndex)}()";

        if (elementName.Length == 0 && (attributes == null || attributes.Count == 0)) return -1;

        try {
            endIndex = Math.Min(Simple.Count, endIndex);

            for (var i = startIndex; i < endIndex; i++) {
                var element = Simple[i];
                if (element.open != i) continue;

                var isNameMatch = string.IsNullOrEmpty(elementName) || elementName == element.name;
                if (!isNameMatch) continue;

                if (attributes == null || attributes.Count == 0) return i;

                var elementAttributes = GetAttributes(i);
                var isAttributeMatch = attributes.All(attr => elementAttributes.Any(ea => ea.name == attr.name && ea.value == attr.value));
                if (isAttributeMatch) return i;
            }
        } catch (Exception ex) { Logger?.LogError($"{mn} Catch. Exception: " + ex.Message); }

        return -1;
    }

    // Task #1: не понятна оптимизация предложенная hatGpt
    List<Models.Simple> GetList() {
        const string mn = $"{nameof(GetList)}()";

        var dxml = new List<Models.Simple>();

        try {
            var dms = GetDs();

            var nl = dms
                .GroupBy(d => new { d.name, d.type })
                .Where(g => g.Key.name == "enclosure" || g.Count() > 1)
                .Select(g => g.Key.name)
                .ToHashSet();

            foreach (var i in dms.Where(i => nl.Contains(i.name))) {
                var open = -1;
                var close = -1;
                var depth = 0;

                if (i.type) close = dxml.Count;
                else open = dxml.Count;

                if (i.name == "enclosure") {
                    close = dxml.Count;
                    open = close;

                    if (dxml.LastOrDefault()?.name == "enclosure") depth = dxml.Last().depth;
                    else {
                        var last = dxml.LastOrDefault();
                        depth = (last?.open > -1 && last?.close == -1)
                            ? last.depth + 1
                            : (last?.depth ?? 0) - 1;
                    }
                }
                else 
                    for (var h = dxml.Count - 1; h >= 0; h--) {
                        if (open > -1) {
                            depth = dxml[h].close == -1
                                ? dxml[h].depth + 1
                                : dxml[h].depth - 1;
                            break;
                        }

                        if (i.name == dxml[h].name && dxml[h].open > -1 && dxml[h].close == -1) {
                            depth = dxml[h].depth + 1;
                            dxml[h].close = dxml.Count;
                            open = h;
                            break;
                        }
                    }

                dxml.Add(new Models.Simple {
                    name = i.name,
                    start = i.startIndex,
                    finish = i.endIndex,
                    open = open,
                    close = close,
                    depth = depth
                });
            }
        } catch (Exception ex) { Logger?.LogError($"{mn} Catch. Exception: " + ex.Message); }

        return dxml;
    }

    /// <summary>
    /// Task: Optimiztion
    /// </summary>
    /// <returns></returns>
    List<Models.D> GetDs() {
        const string mn = $"{nameof(GetList)}()";

        var dms = new List<Models.D>();

        try {
            var cont = new StringBuilder();
            var tagName = new StringBuilder();
            bool tagNameF = false;
            int left = 0;

            for (var i = 0; i < Doc.Length; i++) {
                char currentChar = Doc[i];
                cont.Append(currentChar);

                if (tagNameF) {
                    if (currentChar == ' ' || currentChar == '>' || currentChar == '\n' || currentChar == '-' || (currentChar == '/' && Doc[i - 1] != '<'))
                        tagNameF = false;
                    else tagName.Append(currentChar);
                }

                if (currentChar == '<') {
                    if (!tagName.ToString().Equals("!")) {
                        cont.Clear().Append('<');
                        tagName.Clear();
                        tagNameF = true;
                        left = i;
                    }
                }
                else if (currentChar == '>') {
                    string tagNameStr = tagName.ToString();
                    if (cont[0] == '<' && tagNameStr.Length > 0 && (!tagNameStr.Equals("!") || cont[^2] == '-')) {
                        if (tagNameStr.StartsWith('/')) {
                            tagNameStr = tagNameStr[1..]; // Удаляем `/` в начале
                            dms.Add(new Models.D {
                                name = tagNameStr,
                                startIndex = left,
                                endIndex = i,
                                type = true
                            });
                        }
                        else if (!tagNameStr.StartsWith('!') && !tagNameStr.StartsWith('?'))
                            dms.Add(new Models.D {
                                name = tagNameStr,
                                startIndex = left,
                                endIndex = i
                            });

                        cont.Clear();
                        tagName.Clear();
                        tagNameF = false;
                        left = i + 1;
                    }
                }
            }
        } catch (Exception ex) { Logger?.LogError($"{mn} Catch. Exception: " + ex.Message); }

        return dms;
    }
}