using System;
using System.Collections.Generic;

namespace QLKSan.Models.DTOs
{
    public class MonthlyRevenueDto
    {
        public int Month { get; set; }
        public string MonthLabel => $"Tháng {Month}";
        public decimal TotalRevenue { get; set; }
        public List<WeeklyRevenueDto> Weeks { get; set; } = new List<WeeklyRevenueDto>();
    }

    public class WeeklyRevenueDto
    {
        public string WeekLabel { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<DailyRevenueDto> Days { get; set; } = new List<DailyRevenueDto>();
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public string DateLabel => Date.ToString("dd/MM");
        public string DayOfWeekLabel => GetVietnameseDayOfWeek(Date.DayOfWeek);
        public decimal Revenue { get; set; }

        private string GetVietnameseDayOfWeek(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return "Thứ 2";
                case DayOfWeek.Tuesday: return "Thứ 3";
                case DayOfWeek.Wednesday: return "Thứ 4";
                case DayOfWeek.Thursday: return "Thứ 5";
                case DayOfWeek.Friday: return "Thứ 6";
                case DayOfWeek.Saturday: return "Thứ 7";
                case DayOfWeek.Sunday: return "CN";
                default: return "";
            }
        }
    }
}
