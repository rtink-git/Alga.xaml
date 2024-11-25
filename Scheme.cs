using Microsoft.Extensions.Logging;

namespace Alga.xaml;

public class Scheme
{
        public List<Models.Simple> simple;
        ILoggerFactory? LoggerFactory;
        ILogger? Logger;

        string doc;
        public Scheme(string doc, ILoggerFactory? loggerFactory = null)
        {
            this.LoggerFactory = loggerFactory;
            if(this.LoggerFactory != null) this.Logger = LoggerFactory.CreateLogger<Scheme>();
            simple = new List<Models.Simple>();

            //if (doc != null)
            //{
                this.doc = doc;

                //// xaml/xml - false
                //// html - true
                //if (doc.Length > 13 && doc[0] == '<' && doc[1] == '!' && (doc[2] == 'D' || doc[2] == 'd') && (doc[3] == 'O' || doc[3] == 'o') && (doc[4] == 'C' || doc[4] == 'c') && (doc[5] == 'T' || doc[5] == 't') && (doc[6] == 'Y' || doc[6] == 'y') && (doc[7] == 'P' || doc[7] == 'p') && (doc[8] == 'E' || doc[8] == 'e') && doc[9] == ' ' && doc[10] == 'h' && doc[11] == 't' && doc[12] == 'm' && doc[13] == 'l')
                //    docType = true;

                this.simple = GetList();
            //}
        }

        public class Models
        {
            public class Simple
            {
                public string name { get; set; }
                /// <summary>
                /// start index in doc file
                /// </summary>
                public int start { get; set; }
                /// <summary>
                /// finish index in doc file
                /// </summary>
                public int finish { get; set; }
                /// <summary>
                /// row index in list. of the opening node
                /// </summary>
                public int open { get; set; }
                /// <summary>
                /// row index in list. of the close node
                /// </summary>
                public int close { get; set; }
                public int depth { get; set; }


                public Simple()
                {
                    if (name == null)
                        name = "";
                    open = -1;
                    close = -1;
                    depth = 0;
                }
            }
            public class Attribute
            {
                public string name { get; set; }
                public string value { get; set; }

                public Attribute()
                {
                    name = "";
                    value = "";
                }
            }
            internal class D
            {
                public string name { get; set; }
                public int startIndex { get; set; }
                public int endIndex { get; set; }
                /// <summary>
                /// False - Is open descriptor = <...>
                /// True - Is close descriptor (</...>) or if descriptor has not closed '>' (<... />) 
                /// </summary>
                public bool type { get; set; }

                public D()
                {
                    if (name == null)
                        name = "";
                }
            }
            internal class Q
            {
                public string name { get; set; }
                public bool type { get; set; }

                public Q()
                {
                    if (name == null)
                        name = "";
                }
            }
        }

        public string GetInnerText(int descriptorIndex)
        {
            string s = "";
            try
            {
                if (simple[descriptorIndex].open == descriptorIndex)
                    for (var i = simple[descriptorIndex].finish + 1; i <= simple[simple[descriptorIndex].close].start - 1; i++)
                        s += doc[i];
            }
            catch { if (Logger != null) Logger.LogError("GetInnerText()"); }
            return s;
        }

        /// <summary>
        /// GetTextAsQ
        /// 
        /// </summary>
        /// <param name="descriptorIndex"></param>
        /// <returns></returns>
        //public string GetInnerTextAsQ(int descriptorIndex)
        //{
        //    string s = "";
        //    if (simple[descriptorIndex].open == descriptorIndex)
        //    {
        //        var tgn = simple[descriptorIndex].name.ToLower();
        //        var str = "";
        //        for (var i = simple[descriptorIndex].finish + 1; i <= simple[simple[descriptorIndex].close].start - 1; i++)
        //            str += doc[i];
        //        if(tgn == "p")
        //            s += str + "\n\n";
        //        s += tgn + str + tgn;
        //    }
        //    return s;
        //}

        public string GetInnerOnlyText(int descriptorIndex)
        {
            string s = "";
            try
            {
                if (simple[descriptorIndex].open == descriptorIndex)
                {
                    var f = false;

                    for (var i = simple[descriptorIndex].finish + 1; i <= simple[simple[descriptorIndex].close].start; i++)
                    {

                        if (doc[i] == '<')
                            f = false;

                        if (f)
                            s += doc[i];

                        if (doc[i] == '>')
                            f = true;
                    }
                }
            }
            catch { if (Logger != null) Logger.LogError("GetInnerOnlyText()"); }

            return s;
        }

        //public string GetInnerHtmlAsAFormat(int descriptorIndex)
        //{
        //    string s = "";
        //    try
        //    {
        //        if (simple[descriptorIndex].open == descriptorIndex)
        //        {
        //            //var f = false;

        //            for (var i = simple[descriptorIndex].finish + 1; i <= simple[simple[descriptorIndex].close].start; i++)
        //            {
        //                Models.Simple? msm = null;

        //                if (doc[i] == '<')
        //                {
        //                    foreach (var j in this.simple)
        //                        if (j.start == i)
        //                            msm = j;
        //                    if (msm != null)
        //                        if (msm.name == "p")
        //                        {
        //                            var sub = "";
        //                            for (var k = this.simple[msm.open].finish + 1; k < simple[msm.close].start; k++)
        //                                sub += doc[k];
        //                            if (sub.Length > 0)
        //                                s += sub + "\n\n";
        //                        }
        //                        else if(msm.name == "b" || msm.name == "i" || msm.name == "strong" || msm.name == "em" || msm.name == "code" || msm.name == "s" || msm.name == "strike" || msm.name == "del" || msm.name == "u")
        //                        {
        //                            var sub = "";
        //                            for (var k = this.simple[msm.open].finish + 1; k < simple[msm.close].start; k++)
        //                                sub += doc[k];
        //                            if (sub.Length > 0)
        //                                s += "<" + msm.name + ">" + sub + "</" + msm.name + ">";
        //                        }
        //                }

        //                if (msm == null)
        //                    s += doc[i];
        //            }
        //        }
        //    }
        //    catch { }
        //    return s;
        //}

        public List<Models.Attribute> GetAttributes(int descriptorIndex)
        {
            var attributes = new List<Models.Attribute>();

            try
            {

                Models.Simple d = simple[descriptorIndex];

                if (descriptorIndex == d.open && doc[d.start] == '<')
                {
                    string? attrValue = null;
                    string? attrName = null;
                    var s = "";
                    for (var i = d.start + d.name.Length + 1; i < d.finish; i++)
                    {
                        s += doc[i];

                        if (attrValue == null)
                        {
                            if (this.doc[i] == '=')
                                attrValue = "";
                            else if (this.doc[i] == '>' || this.doc[i] == ' ')
                            {
                                if (attrName != null && attrName.Length > 0)
                                {
                                    attributes.Add(new Models.Attribute() { name = attrName });
                                    attrName = null;
                                }
                            }
                            else
                                attrName += this.doc[i];
                        }
                        else
                        {
                            if (attrName != null)
                                if (this.doc[i] != '\n' && this.doc[i] != '>' && this.doc[i] != '"') //  && this.doc[i] != '\''
                                    attrValue += this.doc[i];
                                else if (this.doc[i - 1] != '=' && this.doc[i - 2] != '=')
                                {
                                    if (attrValue.Length > 0)
                                    {
                                        if (attrValue[0] == '\'' || attrValue[0] == '\"')
                                            attrValue = attrValue.Substring(1);
                                        if (attrValue.Length > 0)
                                        {
                                            if (attrValue[attrValue.Length - 1] == '\'' || attrValue[attrValue.Length - 1] == '\"')
                                                attrValue = attrValue.Substring(0, attrValue.Length - 1);
                                        }
                                    }
                                    attributes.Add(new Models.Attribute() { name = attrName, value = attrValue });
                                    attrName = null;
                                    attrValue = null;
                                }
                        }
                    }
                }
            }
            catch { if (Logger != null) Logger.LogError("GetAttributes()"); }

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
            int r = -1;

            try
            {
                if (elementName.Length > 0 || (attributes != null && attributes.Count > 0))
                {
                    if (simple.Count < endIndex)
                        endIndex = simple.Count;

                    for (var i = startIndex; i < endIndex; i++)
                        if (simple[i].open == i)
                        {
                            if ((elementName != "" && elementName == simple[i].name) || elementName == "")
                            {
                                var f = false;
                                if (attributes == null)
                                    f = true;
                                else
                                {
                                    var atrs = GetAttributes(i);
                                    foreach (var atr in atrs)
                                        foreach (var a in attributes)
                                            if (atr.value == a.value && atr.name == a.name)
                                            {
                                                f = true;
                                                break;
                                            }
                                }

                                if (f)
                                {
                                    r = i;
                                    break;
                                }
                            }
                        }
                }
            }
            catch { if (Logger != null) Logger.LogError("FirstFoundElementIndex()"); }

            return r;
        }

        List<Models.Simple> GetList()
        {
            List<Models.Simple> dxml = new List<Models.Simple>();

            try
            {

                List<Models.D> dms = GetDs();
                List<string> nl = new List<string>();

                List<Models.Q> qml = new List<Models.Q>();
                foreach (var i in dms)
                    qml.Add(new Models.Q() { name = i.name, type = i.type });
                qml.Sort((x, y) => { var ret = x.name.CompareTo(y.name); if (ret == 0) ret = x.type.CompareTo(y.type); return ret; });

                for (var i = 0; i < qml.Count - 1; i++)
                    if ((qml[i].type != qml[i + 1].type && qml[i].name == qml[i + 1].name) || qml[i].name == "enclosure")
                        nl.Add(qml[i].name);

                foreach (var i in dms)
                    foreach (var j in nl)
                        if (i.name == j)
                        {
                            var open = -1;
                            var close = -1;



                            if (i.type) close = dxml.Count;
                            else open = dxml.Count;

                            var depth = 0;

                            if (i.name == "enclosure")
                            {
                                close = dxml.Count;
                                open = close;

                                var xf = false;
                                foreach (var h in new string[] { "enclosure" })
                                    if (h == dxml[dxml.Count - 1].name)
                                    {
                                        xf = true;
                                        break;
                                    }
                                if (xf)
                                    depth = dxml[dxml.Count - 1].depth;
                                else
                                {
                                    if (dxml[dxml.Count - 1].open > -1 && dxml[dxml.Count - 1].close == -1)
                                        depth = dxml[dxml.Count - 1].depth + 1;
                                    else
                                        depth = dxml[dxml.Count - 1].depth - 1;
                                }
                            }
                            else
                            {

                                for (var h = dxml.Count - 1; h >= 0; h--)
                                {
                                    if (open > -1)
                                    {
                                        if (dxml[h].close == -1)
                                            depth = dxml[h].depth + 1;
                                        else
                                            depth = dxml[h].depth - 1;
                                        break;
                                    }
                                    else
                                    {
                                        if (i.name == dxml[h].name)
                                            if (dxml[h].open > -1 && dxml[h].close == -1)
                                            {
                                                depth = dxml[h].depth + 1;
                                                dxml[h].close = dxml.Count();
                                                open = h;

                                                break;
                                            }
                                    }

                                }
                            }

                            dxml.Add(new Models.Simple() { name = i.name, start = i.startIndex, finish = i.endIndex, open = open, close = close, depth = depth });

                            break;
                        }
            }
            catch { if (Logger != null) Logger.LogError("GetList()"); }

            return dxml;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        List<Models.D> GetDs()
        {
            var dms = new List<Models.D>();

            try
            {

                string cont = "";
                bool tagNameF = false;
                string tagName = "";
                int left = 0;

                for (var i = 0; i < doc.Length; i++)
                {
                    cont += doc[i];

                    #region tag name

                    if (tagNameF)
                        if (this.doc[i] == ' ' || this.doc[i] == '>' || this.doc[i] == '\n' || this.doc[i] == '-' || (this.doc[i] == '/' && this.doc[i - 1] != '<'))
                            tagNameF = false;
                        else
                            tagName += this.doc[i];

                    #endregion

                    if (this.doc[i] == '<')
                    {
                        if (tagName != "!")
                        {
                            cont = "<";
                            tagName = "";
                            tagNameF = true;
                            left = i;
                        }
                    }
                    else if (this.doc[i] == '>')
                        if (cont[0] == '<' && tagName != null && tagName.Length > 0 && (tagName != "!" || cont[cont.Length - 2] == '-'))
                        {
                            if (tagName[0] == '/')
                            {
                                if (tagName[0] == '/')
                                    tagName = tagName.Substring(1);
                                var tp = true;
                                //if (tagName[i - 1] == '/')
                                //    tp = false;
                                dms.Add(new Models.D() { name = tagName, startIndex = left, endIndex = i, type = tp });
                            }
                            else if (tagName[0] != '!' && tagName[0] != '?')
                                dms.Add(new Models.D() { name = tagName, startIndex = left, endIndex = i });

                            cont = "";
                            tagName = "";
                            tagNameF = false;
                            left = i + 1;
                        }
                }
            }
            catch { if (Logger != null) Logger.LogError("GetDs()"); }

            return dms;
        }
}
