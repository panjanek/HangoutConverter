using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangoutConverter
{
    public static class Extensions
    {
        public static int GetSplittedLineCount(this XGraphics gfx, string content, XFont font, double maxWidth)
        {
            Func<string, IList<string>> listFor = val => new List<string> { val };         
            Func<string, bool> nOe = str => string.IsNullOrEmpty(str);            
            Func<string, string> sIe = str => nOe(str) ? " " : str;            
            Func<string, string, bool> canFitText = (t1, t2) => gfx.MeasureString($"{(nOe(t1) ? "" : $"{t1} ")}{sIe(t2)}", font).Width <= maxWidth;
            Func<IList<string>, string, IList<string>> appendtoLast =
                    (list, val) => list.Take(list.Count - 1)
                                       .Concat(listFor($"{(nOe(list.Last()) ? "" : $"{list.Last()} ")}{sIe(val)}"))
                                       .ToList();
            var splitted = content.Split(' ');
            var lines = splitted.Aggregate(listFor(""),
                    (lfeed, next) => canFitText(lfeed.Last(), next) ? appendtoLast(lfeed, next) : lfeed.Concat(listFor(next)).ToList(),
                    list => list.Count());
            return lines;
        }

        public static int WordCount(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }
            else
            {
                return text.Split(new char[] { ' ', ',', '.' }).Count();
            }
        }

        public static string ReplaceSpecialChars(this string text)
        {
            if (text != null)
            {
                foreach(var item in Constants.ReplaceList)
                {
                    text = text.Replace(item.Key, item.Value);
                }
            }

            return text;
        }
    }
}
