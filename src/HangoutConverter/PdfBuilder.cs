using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using System.Globalization;

namespace HangoutConverter
{
    public static class PdfBuilder
    {
        public static void CreatePdf(ChatHistory history, string pdfFile)
        {
            Console.WriteLine($"Generating PDF to {pdfFile}");
            int fontSize = 10;
            PdfDocument document = new PdfDocument();
            XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.Always);
            document.Info.Title = string.Join(", ", history.Participants.Values);
            XFont titleFont = new XFont("Arial", 18, XFontStyle.Bold, options);
            XFont bigFont = new XFont("Arial", 14, XFontStyle.Regular, options);
            XFont font = new XFont("Arial", fontSize, XFontStyle.Regular, options);
            XFont fontBold = new XFont("Arial", fontSize, XFontStyle.Bold, options);
            XFont smallFont = new XFont("Arial", fontSize / 2, XFontStyle.Regular, options);
            XFont tinyFont = new XFont("Arial", fontSize / 3, XFontStyle.Regular, options);
            XFont linkFont = new XFont("Arial", fontSize - 3, XFontStyle.Underline, options);

            PdfPage introPage = document.AddPage();
            var maxWidth = introPage.Width - 2 * Constants.HorizontalMargin;
            XGraphics introGfx = XGraphics.FromPdfPage(introPage);
            XTextFormatter introTf = new XTextFormatter(introGfx);
            var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberGroupSeparator = "\u00a0";

            var p1FullName = history.Participants.Values.OrderBy(a => a).ElementAt(0);
            var p2FullName = history.Participants.Values.OrderBy(a => a).ElementAt(1);
            introTf.Alignment = XParagraphAlignment.Center;
            introTf.DrawString($"{p1FullName} and {p2FullName}", titleFont, XBrushes.Black, new XRect(Constants.HorizontalMargin, Constants.VerticalMargin, introPage.Width - 2 * Constants.HorizontalMargin, 50), XStringFormats.TopLeft);

            Console.WriteLine("Generating introduction pages");
            var period = history.Conversation.Max(h => h.Time) - history.Conversation.Min(h => h.Time);
            string intro = $"        {p1FullName} and {p2FullName} exchanged {history.Conversation.Count.ToString("N0", nfi)} messages throughout {(int)period.TotalDays / 365} years and {(int)period.TotalDays % 365} days. ";
            int wordsCount = history.Conversation.Where(h => h.Type == ChatItemType.Text && h.Text != null).Sum(h => h.Text.WordCount());
            int imagesCount = history.Conversation.Where(h => h.Type == ChatItemType.Image).Count();
            int linksCount = history.Conversation.Where(h => h.Type == ChatItemType.Link).Count();
            intro += $"They have written in total {wordsCount.ToString("N0", nfi)} words, sent {imagesCount.ToString("N0", nfi)} photos and shared {linksCount.ToString("N0", nfi)} links. ";
            double wps = 1;
            intro += $"Assuming typical pace of writing and reading messages, each of them spent in, total, over {(int)(wordsCount * wps) / 3600} hours chatting. ";
            int happyFaceCount = history.Conversation.Where(h => h.Type == ChatItemType.Text && h.Text != null).Count(h => h.Text.ReplaceSpecialChars().IndexOf(":)") > -1);
            int sadFaceCount = history.Conversation.Where(h => h.Type == ChatItemType.Text && h.Text != null).Count(h => h.Text.ReplaceSpecialChars().IndexOf(":(") > -1);
            intro += $"They used happy face {happyFaceCount.ToString("N0")} times and sad face {sadFaceCount.ToString("N0")} times, so we can assume that their conversations were {(happyFaceCount > sadFaceCount ? "cheerful" : "difficult")}. ";
            var p1Name = p1FullName.Split(' ').FirstOrDefault();
            var p2Name = p2FullName.Split(' ').FirstOrDefault();
            string p1Id = history.Participants.Where(p => p.Value == p1FullName).Select(p => p.Key).FirstOrDefault();
            string p2Id = history.Participants.Where(p => p.Value == p2FullName).Select(p => p.Key).FirstOrDefault();
            intro += $"{p1Name} sent {history.Conversation.Count(h => h.ParticipantId == p1Id).ToString("N0", nfi)} messages and {p2Name} sent {history.Conversation.Count(h => h.ParticipantId == p2Id).ToString("N0", nfi)}. ";

            var monthly = history.GetMonthlyStatistics();
            var daily = history.GetDailyStatistics();
            var peakMonth = monthly.OrderByDescending(m => m.MessagesCount).FirstOrDefault();
            var peakDay = daily.OrderByDescending(m => m.MessagesCount).FirstOrDefault();
            intro += $"They talked the most during {peakMonth.Time.ToString("MMMM", CultureInfo.InvariantCulture)} of {peakMonth.Time.ToString("yyyy", CultureInfo.InvariantCulture)} but their most active day was {peakDay.Time.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)} - they typed {peakDay.MessagesCount.ToString("N0")} messages ({peakDay.WordsCount.ToString("N0")} words) that single day and probably spent about {1 + (int)(peakDay.WordsCount * wps) / 3600} hours absorbed in conversation. ";

            introTf.Alignment = XParagraphAlignment.Justify;
            introTf.DrawString(intro, bigFont, XBrushes.Black, new XRect(Constants.HorizontalMargin, Constants.VerticalMargin + 30, introPage.Width - 2 * Constants.HorizontalMargin, 300), XStringFormats.TopLeft);

            //diagrams
            double diagramHeight = 90;
            double my = 230;
            double dy = diagramHeight;
            double scale = (diagramHeight - 20) / monthly.Max(m => m.MessagesCount);

            //monthly diagrams
            var grouppedMonths = monthly.GroupBy(g => g.Time.ToString("yyyy"));
            foreach (var yr in grouppedMonths)
            {
                introGfx.DrawRectangle(XPens.Beige, XBrushes.Beige, Constants.HorizontalMargin, my, maxWidth, dy - 20);
                var wx = maxWidth / 12d;
                foreach (var month in yr)
                {
                    int monthNumber = int.Parse(month.Time.ToString("MM"));
                    var height1 = month.MessagesCountPerParticipant[p1Id] * scale;
                    var height2 = month.MessagesCountPerParticipant[p2Id] * scale;
                    var height = month.MessagesCount * scale;
                    var mx = Constants.HorizontalMargin + (monthNumber - 1) * wx;
                    introGfx.DrawRectangle(XPens.Red, XBrushes.Red, mx + 5, my + dy - 20 - height2 - height1, wx - 10, height1);
                    introGfx.DrawRectangle(XPens.Green, XBrushes.Green, mx + 5, my + dy - 20 - height2, wx - 10, height2);
                    introTf.Alignment = XParagraphAlignment.Center;
                    introTf.DrawString(month.Time.ToString("MMMM", CultureInfo.InvariantCulture), smallFont, XBrushes.Black, new XRect(mx + 5, my + dy - 18, wx - 10, 20), XStringFormats.TopLeft);
                    introTf.DrawString(month.MessagesCount.ToString("N0"), smallFont, XBrushes.Black, new XRect(mx + 5, (height2 + height1) > 15 ? (my + dy - 20 - height2 - height1) : (my + dy - 20 - height2 - height1 - 7), wx - 10, 7), XStringFormats.TopLeft);
                }

                introTf.Alignment = XParagraphAlignment.Center;
                introTf.DrawString($"{yr.Key}, monthly", smallFont, XBrushes.Black, new XRect(Constants.HorizontalMargin, my, maxWidth, 10), XStringFormats.TopLeft);

                my += dy;
                if (my > introPage.Height - Constants.VerticalMargin - dy)
                {
                    introPage = document.AddPage();
                    introGfx = XGraphics.FromPdfPage(introPage);
                    introTf = new XTextFormatter(introGfx);
                    my = Constants.VerticalMargin;
                }
            }

            my += 10;
            dy = diagramHeight;

            // dayily diagrams
            var penRed = new XPen(XColors.Red, 1);
            scale = (diagramHeight - 20) / daily.Max(m => m.MessagesCount);
            var grouppedDays = daily.GroupBy(g => g.Time.ToString("yyyy"));
            foreach (var yr in grouppedDays)
            {
                introGfx.DrawRectangle(XPens.Beige, XBrushes.Beige, Constants.HorizontalMargin, my, maxWidth, dy - 20);
                double wx = maxWidth / 365d;
                foreach (var day in yr)
                {
                    if (day.MessagesCount > 0)
                    {
                        var height = day.MessagesCount * scale;
                        var height1 = day.MessagesCountPerParticipant[p1Id] * scale;
                        var height2 = day.MessagesCountPerParticipant[p2Id] * scale;
                        double d = day.Time.DayOfYear - 1;
                        introGfx.DrawRectangle(XBrushes.Red, Constants.HorizontalMargin + d * wx, my + dy - 20 - height1 - height2, wx, height1);
                        introGfx.DrawRectangle(XBrushes.Green, Constants.HorizontalMargin + d * wx, my + dy - 20 - height2, wx, height2);
                    }
                }

                double mw = maxWidth / 12d;
                for (int i = 1; i <= 12; i++)
                {
                    string monthName = new DateTime(2000, i, 1).ToString("MMMM", CultureInfo.InvariantCulture);
                    introTf.DrawString(monthName, smallFont, XBrushes.Black, new XRect(Constants.HorizontalMargin + (i - 1) * mw, my + dy - 18, mw, 10), XStringFormats.TopLeft);
                }

                introTf.Alignment = XParagraphAlignment.Center;
                introTf.DrawString($"{yr.Key}, daily", smallFont, XBrushes.Black, new XRect(Constants.HorizontalMargin, my, maxWidth, 10), XStringFormats.TopLeft);

                my += dy;
                if ((my > introPage.Height - Constants.VerticalMargin - dy) && (yr.Key != grouppedDays.Last().Key))
                {
                    introPage = document.AddPage();
                    introGfx = XGraphics.FromPdfPage(introPage);
                    introTf = new XTextFormatter(introGfx);
                    my = Constants.VerticalMargin;
                }
            }

            introTf.Alignment = XParagraphAlignment.Left;
            introTf.DrawString(p1Name, bigFont, XBrushes.Red, new XRect(Constants.HorizontalMargin, my, maxWidth, 20), XStringFormats.TopLeft);
            introTf.Alignment = XParagraphAlignment.Right;
            introTf.DrawString(p2Name, bigFont, XBrushes.Green, new XRect(Constants.HorizontalMargin, my, maxWidth, 20), XStringFormats.TopLeft);


            //pages
            Console.Write("Generating pages");
            int pageCount = 1;
            DateTimeOffset previousTime = DateTimeOffset.MinValue;
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XTextFormatter tf = new XTextFormatter(gfx);
            int y = Constants.VerticalMargin;
            foreach (var item in history.Conversation)
            {
                if (history.Participants.ContainsKey(item.ParticipantId))
                {
                    string participantFullName = history.Participants[item.ParticipantId];
                    string participantFirstName = participantFullName.Split(' ').FirstOrDefault();
                    int participantIndex = history.Participants.Keys.ToList().IndexOf(item.ParticipantId);
                    maxWidth = page.Width - 2 * Constants.HorizontalMargin;

                    if (previousTime.Date != item.Time.Date)
                    {
                        gfx.DrawRectangle(XPens.LightGray, XBrushes.LightGray, Constants.HorizontalMargin, y, maxWidth, 12);
                        tf.Alignment = XParagraphAlignment.Center;
                        int days = (int)Math.Floor((item.Time.Date - previousTime.Date).TotalDays);
                        string desc = previousTime == DateTimeOffset.MinValue ? "" : (days == 1 ? "(the next day)" : $"(after {days} days)");
                        tf.DrawString($"{item.Time.ToString("dddd", CultureInfo.InvariantCulture)}, {item.Time.ToString("yyyy-MM-dd")} {desc}", fontBold, XBrushes.Black, new XRect(Constants.HorizontalMargin, y, maxWidth, y + 12), XStringFormats.TopLeft);

                        tf.Alignment = XParagraphAlignment.Left;
                        tf.DrawString(p1Name, fontBold, XBrushes.Black, new XRect(Constants.HorizontalMargin, y, maxWidth, y + 12), XStringFormats.TopLeft);

                        tf.Alignment = XParagraphAlignment.Right;
                        tf.DrawString(p2Name, fontBold, XBrushes.Black, new XRect(Constants.HorizontalMargin, y, maxWidth, y + 12), XStringFormats.TopLeft);
                        y += 14;
                    }

                    string signature = $"{participantFirstName}, {item.Time.DateTime.ToString("yyyy-MM-dd HH:mm")}";
                    tf.Alignment = participantIndex == 1 ? XParagraphAlignment.Left : XParagraphAlignment.Right;
                    tf.DrawString(signature, smallFont, XBrushes.DarkGray, new XRect(Constants.HorizontalMargin, y, maxWidth, y + 7), XStringFormats.TopLeft);
                    y += (int)smallFont.Size + 2;

                    if (item.Type == ChatItemType.Text)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Text))
                        {
                            string text = item.Text.ReplaceSpecialChars();
                            int lines = gfx.GetSplittedLineCount(text, font, maxWidth - Constants.AdditionalMargin);
                            int height = lines * (fontSize + 1);
                            if (participantIndex == 1)
                            {
                                tf.DrawString(text, font, XBrushes.Black, new XRect(Constants.HorizontalMargin, y, maxWidth - Constants.AdditionalMargin, y + height), XStringFormats.TopLeft);
                            }
                            else
                            {
                                tf.DrawString(text, font, XBrushes.Black, new XRect(Constants.HorizontalMargin + Constants.AdditionalMargin, y, maxWidth - Constants.AdditionalMargin, y + height), XStringFormats.TopLeft);
                            }

                            y += height + 2;
                        }
                    }
                    else if (item.Type == ChatItemType.Link)
                    {
                        int height = fontSize + 1;
                        var xrect = new XRect(Constants.HorizontalMargin, y, maxWidth, height);
                        var rect = gfx.Transformer.WorldToDefaultPage(xrect);
                        var pdfrect = new PdfRectangle(rect);

                        page.AddWebLink(pdfrect, item.Url);
                        tf.DrawString(item.Text, linkFont, XBrushes.Blue, xrect, XStringFormats.TopLeft);

                        y += height + 2;
                    }
                    else if (item.Type == ChatItemType.Image)
                    {
                        if (string.IsNullOrWhiteSpace(item.LocalPath))
                        {
                            int height = fontSize + 1;
                            var xrect = new XRect(Constants.HorizontalMargin, y, maxWidth, height);
                            var rect = gfx.Transformer.WorldToDefaultPage(xrect);
                            var pdfrect = new PdfRectangle(rect);

                            page.AddWebLink(pdfrect, item.Url);
                            tf.DrawString("[IMG]", linkFont, XBrushes.Blue, xrect, XStringFormats.TopLeft);
                            y += height + 2;
                        }
                        else
                        {
                            using (XImage image = XImage.FromFile(item.LocalPath))
                            {
                                int width = (int)image.PointWidth / 3;
                                int height = (int)image.PointHeight / 3;

                                if (y + height > page.Height - Constants.VerticalMargin)
                                {
                                    page = document.AddPage();
                                    gfx = XGraphics.FromPdfPage(page);
                                    tf = new XTextFormatter(gfx);
                                    y = Constants.VerticalMargin;
                                    pageCount++;
                                }

                                var x = participantIndex == 1 ? Constants.HorizontalMargin : page.Width - Constants.HorizontalMargin - width;

                                if (!string.IsNullOrWhiteSpace(item.Url))
                                {
                                    var xrect = new XRect(x, y, width, height);
                                    var rect = gfx.Transformer.WorldToDefaultPage(xrect);
                                    var pdfrect = new PdfRectangle(rect);
                                    page.AddWebLink(pdfrect, item.Url);
                                }

                                gfx.DrawImage(image, x, y, width, height);
                                y += height + 2;
                            }
                        }
                    }
                    else if (item.Type == ChatItemType.Video)
                    {
                        int height = fontSize + 1;
                        var xrect = new XRect(Constants.HorizontalMargin, y, maxWidth, height);
                        var rect = gfx.Transformer.WorldToDefaultPage(xrect);
                        var pdfrect = new PdfRectangle(rect);

                        page.AddWebLink(pdfrect, item.Url);
                        tf.DrawString("[VIDEO]", linkFont, XBrushes.Blue, xrect, XStringFormats.TopLeft);
                        y += height + 2;
                    }

                    if (y > page.Height - Constants.VerticalMargin)
                    {
                        pageCount++;
                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        tf = new XTextFormatter(gfx);
                        y = Constants.VerticalMargin;
                        if (pageCount % 10 == 0)
                        {
                            Console.Write(".");
                        }
                    }

                    previousTime = item.Time;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Generated {pageCount} PDF pages");
            Console.WriteLine($"Saving PDF to file {pdfFile}");
            document.Save(pdfFile);
            Console.WriteLine($"PDF file saved to {pdfFile}");
        }
    }
}
