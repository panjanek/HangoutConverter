using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangoutConverter
{
    public class ChatHistory
    {
        public ChatHistory()
        {
            this.Participants = new Dictionary<string, string>();
            this.Conversation = new List<ChatItem>();
        }

        public Dictionary<string, string> Participants { get; set; }

        public List<ChatItem> Conversation { get; set; }

        public List<ChatStatistics> GetMonthlyStatistics()
        {
            List<ChatStatistics> result = new List<ChatStatistics>();
            var minDate = this.Conversation.Min(c => c.Time);
            var maxDate = this.Conversation.Max(c => c.Time);
            var startMonth = new DateTimeOffset(minDate.Year, minDate.Month, 1, 0, 0, 0, minDate.Offset);
            var stopMonth = new DateTimeOffset(maxDate.Year, maxDate.Month, 1, 0, 0, 0, maxDate.Offset);
            var month = startMonth;
            while (month <= stopMonth)
            {
                var start = month;
                var stop = month.AddMonths(1);
                result.Add(this.GetStatisticsForPeriod(start, stop));
                month = stop;
            }

            return result;
        }

        public List<ChatStatistics> GetDailyStatistics()
        {
            List<ChatStatistics> result = new List<ChatStatistics>();
            var minDate = this.Conversation.Min(c => c.Time);
            var maxDate = this.Conversation.Max(c => c.Time);
            var startDay = new DateTimeOffset(minDate.Year, minDate.Month, minDate.Day, 0, 0, 0, minDate.Offset);
            var stopDay = new DateTimeOffset(maxDate.Year, maxDate.Month, maxDate.Day, 0, 0, 0, maxDate.Offset);
            var day = startDay;
            while (day <= stopDay)
            {
                var start = day;
                var stop = day.AddDays(1);
                result.Add(this.GetStatisticsForPeriod(start, stop));
                day = stop;
            }

            return result;
        }

        private ChatStatistics GetStatisticsForPeriod(DateTimeOffset start, DateTimeOffset stop)
        {
            ChatStatistics stats = new ChatStatistics();
            stats.MessagesCount = this.Conversation.Count(c => c.Time >= start && c.Time <= stop);
            stats.WordsCount = this.Conversation.Where(c => c.Time >= start && c.Time <= stop).Sum(c => c.Text.WordCount());
            stats.Time = start;
            foreach (var key in this.Participants.Keys)
            {
                stats.MessagesCountPerParticipant[key] = this.Conversation.Count(c => c.ParticipantId == key && c.Time >= start && c.Time <= stop);
                stats.WordsCountPerParticipant[key] = this.Conversation.Where(c => c.ParticipantId == key && c.Time >= start && c.Time <= stop).Sum(c => c.Text.WordCount());
            }

            return stats;
        }
    }

    public class ChatItem
    {
        public string ParticipantId { get; set; }

        public string Text { get; set; }

        public string Url { get; set; }

        public string LocalPath { get; set; }

        public DateTimeOffset Time { get; set; }

        public ChatItemType Type { get; set; }
    }

    public class ChatStatistics
    {
        public ChatStatistics()
        {
            this.MessagesCountPerParticipant = new Dictionary<string, int>();
            this.WordsCountPerParticipant = new Dictionary<string, int>();
        }

        public DateTimeOffset Time { get; set; }

        public int MessagesCount { get; set; }

        public int WordsCount { get; set; }

        public Dictionary<string, int> MessagesCountPerParticipant { get; set; }

        public Dictionary<string, int> WordsCountPerParticipant { get; set; }
    }

    public enum ChatItemType : int
    {
        Text = 0,
        Link = 1,
        Image = 2,
        Video = 3
    }
}
