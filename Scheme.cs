using Microsoft.Extensions.Logging;
using System.Text;

namespace Alga.xaml;
/// <summary>
/// Represents a schema that processes a serialized XAML document and provides various utility methods for retrieving and parsing data.
/// </summary>
public class Scheme {
    readonly string Doc;
    readonly ILogger? Logger;
    /// <summary>
    /// A list of simple elements extracted from the document.
    /// </summary>
    public List<Models.Simple> Simple { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Scheme"/> class
    /// </summary>
    /// <param name="doc">Serialized XAML document content to process</param>
    /// <param name="loggerFactory">Optional logger factory for logging purposes</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided document is null</exception>
    public Scheme(string doc, ILoggerFactory? loggerFactory = null) {
        this.Doc = doc ?? throw new ArgumentNullException(nameof(doc));
        Logger = loggerFactory?.CreateLogger<Scheme>();
        this.Simple = GetList();
    }

    /// <summary>
    /// Retrieves the inner text for a specific descriptor (tag) by its index.
    /// </summary>
    /// <param name="descriptorIndex">The index of the descriptor to process.</param>
    /// <returns>The inner text if found; otherwise, an empty string.</returns>
    public string GetInnerText(int descriptorIndex) {
        const string mn = $"{nameof(GetInnerText)}()";

        if (!IsValidDescriptorIndex(descriptorIndex))
            return LogAndReturnEmpty(mn, "Input param \"descriptorIndex\" is out of range.");

        var descriptor = Simple[descriptorIndex];
        if (descriptor.open != descriptorIndex) return string.Empty;

        try {
            int start = descriptor.finish + 1;
            int end = Simple[descriptor.close].start;
            return start < end ? Doc.AsSpan(start, end - start).ToString() : string.Empty;
        } catch (Exception ex) {
            Logger?.LogError($"{mn}: Exception: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Retrieves only the inner text for a specific descriptor by its index, excluding nested tags.
    /// </summary>
    /// <param name="descriptorIndex">The index of the descriptor to process.</param>
    /// <returns>The inner text if found; otherwise, an empty string.</returns>
    public string GetInnerOnlyText(int descriptorIndex) {
        const string mn = $"{nameof(GetInnerOnlyText)}()";

        if (!IsValidDescriptorIndex(descriptorIndex))
            return LogAndReturnEmpty(nameof(GetInnerOnlyText), "Input param \"descriptorIndex\" is out of range.");

        var descriptor = Simple[descriptorIndex];
        if (descriptor.open != descriptorIndex) return string.Empty;

        var result = new StringBuilder();
        bool insideText = false;

        try {
            int start = descriptor.finish + 1;
            int end = Simple[descriptor.close].start;

            for (int i = start; i < end; i++) {
                char c = Doc[i];

                if (c == '<') insideText = false;
                if (insideText) result.Append(Doc[i]);
                if (c == '>') insideText = true;
            }
        } catch (Exception ex) { Logger?.LogError($"{mn}: Exception: {ex.Message}"); }

        return result.ToString();
    }

    /// <summary>
    /// Extracts the attributes for a descriptor by its index.
    /// </summary>
    /// <param name="descriptorIndex">The index of the descriptor to process.</param>
    /// <returns>A list of attributes associated with the descriptor.</returns>
    public List<Models.Attribute> GetAttributes(int descriptorIndex) {
        const string mn = $"{nameof(GetAttributes)}()";

        var attributes = new List<Models.Attribute>();

        if (!IsValidDescriptorIndex(descriptorIndex)) {
            Logger?.LogError($"{mn}: Input param \"descriptorIndex\" is out of range.");
            return new List<Models.Attribute>();
        }

        var descriptor = Simple[descriptorIndex];
        if (descriptor.open != descriptorIndex || Doc[descriptor.start] != '<') return attributes;

        string? attrName = null;
        string? attrValue = null;
        
        try {
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

    // Task: Есть  Есть вопросики к  соченанию elementName & attributes

    /// <summary>
    /// Finds the index of the first matching element based on the name and attributes.
    /// </summary>
    /// <param name="elementName">The name of the element to find (optional).</param>
    /// <param name="attributes">A list of attributes to match (optional).</param>
    /// <param name="startIndex">The starting index for the search.</param>
    /// <param name="endIndex">The ending index for the search.</param>
    /// <returns>The index of the first matching element, or -1 if not found.</returns>
    public int FirstFoundElementIndex(string elementName= "", List<Models.Attribute>? attributes = null,  int startIndex = 0, int endIndex = int.MaxValue) {
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
                if(attributes.All(attr => elementAttributes.Any(ea => ea.name == attr.name && ea.value == attr.value))) return i;
            }
        } catch (Exception ex) { Logger?.LogError($"{mn} Catch. Exception: " + ex.Message); }

        return -1;
    }

    // Task #1: не понятна оптимизация предложенная ChatGpt

    /// <summary>
    /// Retrieves a list of simple elements from the document
    /// </summary>
    /// <returns>A list of parsed simple elements.</returns>
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
    /// Retrieves detailed information from the document as "D" elements.
    /// </summary>
    /// <returns>A list of "D" elements extracted from the document</returns>
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

    /// <summary>
    /// Validates if the descriptor index is within range.
    /// </summary>
    bool IsValidDescriptorIndex(int index) => index >= 0 && index < Simple.Count;

    /// <summary>
    /// Logs an error and returns an empty string.
    /// </summary>
    string LogAndReturnEmpty(string methodName, string message)
    {
        Logger?.LogError($"{methodName}: {message}");
        return string.Empty;
    }
}