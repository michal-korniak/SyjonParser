using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Syjon
{
    class Group
    {
        public string Type { get; set; }
        public int Number { get; set; }
    }
    class Activity : IComparable
    {
        public Group Group { get; set; }
        public string SubjectName { get; set; }
        public string[] TeacherNames { get; set; }
        public string Room { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public DateTime Time { get; set; }
        public TimeSpan Length { get; set; }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            Activity otherActivity = obj as Activity;
            if (otherActivity != null)
            {
                if (DayOfWeek == otherActivity.DayOfWeek)
                {
                    return Time.CompareTo(otherActivity.Time);
                }
                else
                    return DayOfWeek.CompareTo(otherActivity.DayOfWeek);
            }
            else
                throw new ArgumentException("Object is not a Activity");
        }
    }
    class Parser
    {
        private const double hourOnSyjon = 7.69230769231;

        static public Dictionary<string, int> GetNumbersOfGroups(string htmlSyjon)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (var activity in GetAllActivies(htmlSyjon))
            {
                var group = GetGroup(activity);

                if(!result.Keys.Contains(group.Type))
                {
                    result.Add(group.Type, group.Number);
                }
                else
                {
                    if(result[group.Type]<group.Number)
                    {
                        result[group.Type] = group.Number;
                    }
                }
            }

            return result;
        }  
        static public List<Activity> GetPlan(string htmlSyjon, Dictionary<string, int> groups)
        {
            List<Activity> result = new List<Activity>();
            foreach(var activity in GetAllActivies(htmlSyjon))
            {
                var group = GetGroup(activity);
                if (!groups.Keys.Contains(group.Type) || groups[group.Type] != group.Number)
                    continue;

                Activity activityObj = new Activity();
                activityObj.Group = group;
                activityObj.SubjectName = activity.SelectNodes("div[@class='activity_content']/div[@class='subject_content']")[0].ChildNodes[0].InnerHtml;
                activityObj.TeacherNames = (from x in activity.SelectNodes("div[@class='activity_content']/div[@class='teachers_content']")[0].ChildNodes
                                            select x.ChildNodes[0].InnerHtml).ToArray<string>();
                activityObj.Room = activity.SelectNodes("div[@class='activity_content']/div[@class='bottom_content_containter']/div[@class='room_content']")[0]
                                           .ChildNodes[0].InnerHtml;
                activityObj.DayOfWeek = GetDayOfWeek(activity);
                activityObj.Time = GetTime(activity);
                activityObj.Length = GetLength(activity);

                result.Add(activityObj);
            }

            result.Sort();
            return result;
        }

        static private IEnumerable<HtmlNode> GetAllActivies(string htmlSyjon)
        {
            var html = new HtmlDocument();
            html.LoadHtml(htmlSyjon);
            var root = html.DocumentNode;
            return root.Descendants("div").Where(d =>
                                d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("activity_block")
                                && d.Attributes["style"].Value.Count(ch => ch == '-') == 1);
        }
        static private Group GetGroup(HtmlNode activity)
        {
            Group result = new Group();

            result.Type = (activity.SelectNodes("div[@class='activity_content']/div[@class='bottom_content_containter']/div[@class='type_content']")[0].
                          ChildNodes[0].InnerHtml);

            try { result.Number = int.Parse(activity.SelectNodes("div[@class='activity_group']")[0].InnerHtml); }
            catch { result.Number = 1; }

            return result;
        }
        static private DayOfWeek GetDayOfWeek(HtmlNode activity)
        {
            
            string style = activity.Attributes["style"].Value;
            int begin = 6;
            int end = style.IndexOf("%;");
            double leftAttribute = double.Parse(style.Substring(begin, end - begin), new CultureInfo("en"));

            return (DayOfWeek)((int)(leftAttribute / 14.2857142857 + 1));        //Sun is 0, Mon is 1
        }
        static private DateTime GetTime(HtmlNode activity)
        {
            string style = activity.Attributes["style"].Value;
            int begin = style.IndexOf("top: ") + 5;
            int end = style.IndexOf("%;", begin);
            double topAttribute = double.Parse(style.Substring(begin, end - begin), new CultureInfo("en"));

            return ConvertDoubleToTime(topAttribute / hourOnSyjon + 8);
        }
        static private TimeSpan GetLength(HtmlNode activity)
        {
            string style = activity.Attributes["style"].Value;
            int begin = style.IndexOf("height: ") + 8;
            int end = style.IndexOf("%;", begin);
            double heightAttribute = double.Parse(style.Substring(begin, end - begin), new CultureInfo("en"));
            return TimeSpan.FromHours(heightAttribute / hourOnSyjon);
        }
        static private DateTime ConvertDoubleToTime(double timeInDouble)
        {
            int hours = (int)(Math.Truncate(timeInDouble));
            double minutes = (timeInDouble - hours) / (0.5 / 30);
            minutes = Math.Round(minutes / 5.0) * 5;

            if (minutes >= 60)
            {
                hours += 1;
                minutes -= 60;
            }

            return new DateTime(1970, 1, 1, hours, (int)minutes, 0);
        } 
    }
}
