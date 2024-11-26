using Microsoft.Extensions.Logging;
using System.Text;

namespace Alga.xaml;

public class Scheme
{
    public List<Models.Simple> simple;
    ILogger? _Logger;
    string _Doc;

    public Scheme(string doc, ILoggerFactory? loggerFactory = null)
    {
        this._Logger = loggerFactory?.CreateLogger<Scheme>();
        simple = new List<Models.Simple>();

        this._Doc = doc;
        this.simple = GetList();
    }

    public string GetInnerText(int descriptorIndex)
    {
        var sb = new StringBuilder();

        try {
            var simpleDescriptor = simple[descriptorIndex];
            if (simpleDescriptor.open == descriptorIndex)
                for (var i = simpleDescriptor.finish + 1; i <= simple[simpleDescriptor.close].start - 1; i++)
                    sb.Append(this._Doc[i]);
        }
        catch (Exception ex) { this._Logger?.LogError($"GetInnerText() failed. DescriptorIndex: {descriptorIndex}, Error: {ex.Message}"); }

        return sb.ToString();
    }

    public string GetInnerOnlyText(int descriptorIndex)
    {
        var sb = new StringBuilder();

        try  {
            var simpleDescriptor = simple[descriptorIndex];

            if (simpleDescriptor.open == descriptorIndex) {
                bool insideTag = false;

                for (int i = simpleDescriptor.finish + 1; i <= simple[simpleDescriptor.close].start; i++) {
                    var currentChar = this._Doc[i];

                    if (currentChar == '<' && insideTag) insideTag = false;
                    if (insideTag) sb.Append(currentChar);
                    if (currentChar == '>' && !insideTag) insideTag = true;
                }
            }
        }
        catch (Exception ex) { this._Logger?.LogError($"GetInnerOnlyText() failed. DescriptorIndex: {descriptorIndex}, Error: {ex.Message}"); }

        return sb.ToString();
    }

    public List<Models.Attribute> GetAttributes(int descriptorIndex)
    {
        var attributes = new List<Models.Attribute>();

        try {
            var d = simple[descriptorIndex];

            if (descriptorIndex == d.open && this._Doc[d.start] == '<') {
                string? attrValue = null;
                string? attrName = null;
                var sb = new StringBuilder();

                for (var i = d.start + d.name.Length + 1; i < d.finish; i++) {
                    char currentChar = this._Doc[i];

                    // Начинаем с обработки имени атрибута
                    if (attrValue == null) {
                        if (currentChar == '=') attrValue = "";
                        else if (currentChar == '>' || currentChar == ' ') {
                            // Конец имени атрибута, добавляем его в список
                            if (!string.IsNullOrEmpty(attrName))
                            {
                                attributes.Add(new Models.Attribute() { name = attrName });
                                attrName = null;
                            }
                        }
                        else { 
                            sb.Append(currentChar);
                            attrName = sb.ToString(); // Собираем имя атрибута
                        }
                    }
                    else {
                        // Обрабатываем значение атрибута
                        if (attrName != null) {
                            if (currentChar != '\n' && currentChar != '>' && currentChar != '"')
                            {
                                sb.Append(currentChar);
                                attrValue += sb.ToString();
                                sb.Clear();
                            }
                            else if (this._Doc[i - 1] != '=' && this._Doc[i - 2] != '=')
                            {
                                // Завершаем обработку атрибута
                                if (attrValue?.Length > 0)
                                {
                                    // Удаляем возможные кавычки с начала и конца значения
                                    attrValue = this._RemoveQuotes(attrValue);
                                    attributes.Add(new Models.Attribute() { name = attrName, value = attrValue });
                                    attrName = null;
                                    attrValue = null;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex) { this._Logger?.LogError($"GetAttributes() failed. DescriptorIndex: {descriptorIndex}, Error: {ex.Message}"); }

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
        public int FirstFoundElementIndex(string elementName= "", List<Models.Attribute>? attributes = null,  int startIndex = 0, int endIndex = int.MaxValue)
        {
            int resultIndex = -1;

            try {
                // Если элемент или атрибуты не заданы, выполняем проверку
                if (elementName.Length > 0 || (attributes?.Count ?? 0) > 0) {
                    // Обновление endIndex для предотвращения выхода за пределы массива
                    endIndex = Math.Min(endIndex, simple.Count);

                    // Перебор элементов с startIndex по endIndex
                    for (int i = startIndex; i < endIndex; i++) {
                        if (simple[i].open == i) {
                            // Если элемент совпадает с именем или имя пустое
                            if ((string.IsNullOrEmpty(elementName) || simple[i].name == elementName))
                            {
                                bool isMatch = attributes == null || this._AttributesMatch(i, attributes);

                                if (isMatch)
                                {
                                    resultIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { this._Logger?.LogError($"FirstFoundElementIndex() failed. Error: {ex.Message}"); }

            return resultIndex;
        }

        List<Models.Simple> GetList()
        {
            var dxml = new List<Models.Simple>();

            try {
                List<Models.D> dms = GetDs();
                List<string> nl = new List<string>();

                // Создаем список qml, сортируем по имени и типу
                var qml = dms
                    .Select(i => new Models.Q() { name = i.name, type = i.type })
                    .OrderBy(q => q.name)
                    .ThenBy(q => q.type)
                    .ToList();

                // Формируем список nl, добавляем имена, которые должны быть обработаны
                for (int i = 0; i < qml.Count - 1; i++)
                    if ((qml[i].type != qml[i + 1].type && qml[i].name == qml[i + 1].name) || qml[i].name == "enclosure")
                        nl.Add(qml[i].name);

                // Кэшируем размер dxml, чтобы избежать многократных вычислений
                int dxmlCount = 0;

                foreach (var i in dms)
                    foreach (var j in nl)
                        if (i.name == j) {
                            int open = -1;
                            int close = -1;
                            int depth = 0;

                            if (i.type) close = dxmlCount;  // если тип "true", то закрываем элемент
                            else open = dxmlCount;  // если тип "false", то открываем элемент


                            if (i.name == "enclosure")
                            {
                                // Специальная логика для "enclosure"
                                close = open = dxmlCount;

                                bool hasEnclosureTag = dxml.Count > 0 && dxml[dxml.Count - 1].name == "enclosure";
                                depth = hasEnclosureTag ? dxml[dxml.Count - 1].depth : dxmlCount > 0 && dxml[dxml.Count - 1].open > -1 && dxml[dxml.Count - 1].close == -1 ? dxml[dxml.Count - 1].depth + 1 : dxml[dxml.Count - 1].depth - 1;
                            }
                            else
                                for (int h = dxml.Count - 1; h >= 0; h--)
                                {
                                    if (open > -1)
                                    {
                                        // Открытие тега
                                        depth = dxml[h].close == -1 ? dxml[h].depth + 1 : dxml[h].depth - 1;
                                        break;
                                    }
                                    else if (i.name == dxml[h].name && dxml[h].open > -1 && dxml[h].close == -1)
                                    {
                                        // Закрытие тега
                                        depth = dxml[h].depth + 1;
                                        dxml[h].close = dxmlCount; // Закрываем предыдущий элемент
                                        open = h;
                                        break;
                                    }
                                }

                            // Добавляем новый элемент в dxml
                            dxml.Add(new Models.Simple
                            {
                                name = i.name,
                                start = i.startIndex,
                                finish = i.endIndex,
                                open = open,
                                close = close,
                                depth = depth
                            });

                            // Обновляем счетчик
                            dxmlCount++;
                            break;
                        }
            }
            catch (Exception ex) { this._Logger?.LogError($"GetList() failed: {ex.Message}"); }

            return dxml;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        List<Models.D> GetDs()
        {
            var dms = new List<Models.D>();
            var sb = new StringBuilder();  // Используем StringBuilder для эффективной конкатенации строк

            try {
                var tagNameF = false;
                var tagName = string.Empty;
                var left = 0;

                for (var i = 0; i < this._Doc.Length; i++) {
                    sb.Append(this._Doc[i]);  // Добавляем текущий символ в StringBuilder

                    // Обработка имени тега
                    if (tagNameF)
                        if (this._IsTagNameTerminator(this._Doc[i])) tagNameF = false;
                        else tagName += this._Doc[i];

                    // Начало тега
                    if (this._Doc[i] == '<')
                    {
                        if (tagName != "!")
                        {
                            sb.Clear();  // Очищаем StringBuilder для нового тега
                            tagName = "";
                            tagNameF = true;
                            left = i;
                        }
                    }
                    else if (this._Doc[i] == '>')
                    {
                        if (sb[0] == '<' && !string.IsNullOrEmpty(tagName) && (tagName != "!" || sb[sb.Length - 2] == '-'))
                        {
                            this._ProcessTag(dms, tagName, left, i);
                            sb.Clear();  // Очищаем StringBuilder для следующего тега
                            tagName = "";
                            tagNameF = false;
                            left = i + 1;
                        }
                    }
                }
            }
            catch (Exception ex) { this._Logger?.LogError($"GetDs() failed: {ex.Message}"); }

            return dms;
        }

    // Вспомогательный метод для удаления кавычек из значения атрибута
    string _RemoveQuotes(string value)
    {
        if (value.Length > 0 && (value[0] == '\'' || value[0] == '\"'))
            value = value.Substring(1);

        if (value.Length > 0 && (value[value.Length - 1] == '\'' || value[value.Length - 1] == '\"'))
            value = value.Substring(0, value.Length - 1);

        return value;
    }

    // Вспомогательный метод для проверки совпадений атрибутов
    bool _AttributesMatch(int elementIndex, List<Models.Attribute> attributes)
    {
        var elementAttributes = GetAttributes(elementIndex);

        // Используем HashSet для быстрого поиска совпадений
        var attributeNames = new HashSet<string>(attributes.Select(a => $"{a.name}:{a.value}"));

        foreach (var attr in elementAttributes)
        {
            if (attributeNames.Contains($"{attr.name}:{attr.value}"))
            {
                return true;
            }
        }

        return false;
    }

    // Метод для проверки, является ли текущий символ терминатором имени тега
    bool _IsTagNameTerminator(char c) => c == ' ' || c == '>' || c == '\n' || c == '-' || (c == '/' && this._Doc[c - 1] != '<');

    // Метод для обработки найденного тега и добавления его в список
    void _ProcessTag(List<Models.D> dms, string tagName, int left, int i)
    {
        bool isClosingTag = tagName.StartsWith("/");
        if (isClosingTag)
        {
            tagName = tagName.Substring(1);  // Убираем слэш из имени закрывающего тега
        }

        bool isSelfClosing = false;  // Здесь можно добавить логику для самозакрывающихся тегов, если нужно

        // Добавляем тег в список
        var tag = new Models.D
        {
            name = tagName,
            startIndex = left,
            endIndex = i,
            type = isClosingTag
        };

        dms.Add(tag);
    }
}