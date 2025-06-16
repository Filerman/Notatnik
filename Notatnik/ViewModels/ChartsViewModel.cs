using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Notatnik.Data;
using Notatnik.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Notatnik.ViewModels
{
    public class ChartsViewModel : ViewModelBase
    {
        public PlotModel CategoryPlot { get; }
        public PlotModel WordCountPlot { get; }

        public ChartsViewModel()
        {
            using var ctx = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite("Data Source=Notatnik.db")
                    .Options);

            var colors = OxyPalettes.HueDistinct(10).Colors;

            var byType = ctx.Notes
                            .AsNoTracking()
                            .GroupBy(n => n.Type)
                            .Select(g => new { Type = g.Key, Count = g.Count() })
                            .ToList();

            var catModel = new PlotModel { Title = "Notatki wg typu" };

            var catAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                IsTickCentered = true
            };

            var valAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                AbsoluteMinimum = 0,
                MajorStep = 1,
                MinorStep = 1
            };

            var typeSeries = new BarSeries
            {
                LabelPlacement = LabelPlacement.Outside,
                LabelFormatString = "{0}"
            };

            for (int i = 0; i < byType.Count; i++)
            {
                var row = byType[i];
                catAxis.Labels.Add(row.Type.ToString());
                typeSeries.Items.Add(new BarItem(row.Count) { Color = colors[i] });
            }

            catModel.Axes.Add(catAxis);
            catModel.Axes.Add(valAxis);
            catModel.Series.Add(typeSeries);
            CategoryPlot = catModel;

            var wordsByType = ctx.Notes
                                 .AsNoTracking()
                                 .Include(n => n.ChecklistItems)
                                 .AsEnumerable()
                                 .GroupBy(n => n.Type)
                                 .Select(g => new
                                 {
                                     Type = g.Key,
                                     WordTotal = g.Sum(CountWords)
                                 })
                                 .OrderByDescending(x => x.WordTotal)
                                 .ToList();

            var wordsModel = new PlotModel { Title = "Słowa w notatkach wg typu" };

            var wCatAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                IsTickCentered = true
            };

            var wValAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = 0,
                AbsoluteMinimum = 0,
                MajorStep = 1,
                MinorStep = 1
            };

            var wordsSeries = new BarSeries
            {
                LabelPlacement = LabelPlacement.Outside,
                LabelFormatString = "{0}"
            };

            for (int i = 0; i < wordsByType.Count; i++)
            {
                var row = wordsByType[i];
                wCatAxis.Labels.Add(row.Type.ToString());
                wordsSeries.Items.Add(new BarItem(row.WordTotal) { Color = colors[i] });
            }

            wordsModel.Axes.Add(wCatAxis);
            wordsModel.Axes.Add(wValAxis);
            wordsModel.Series.Add(wordsSeries);
            WordCountPlot = wordsModel;
        }

        private static int CountWords(Note n)
        {
            if (n.Type is NoteType.Regular or NoteType.LongFormat)
            {
                var txt = Regex.Replace(n.Content ?? string.Empty, "<.*?>", string.Empty);
                return txt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            }

            if (n.Type == NoteType.CheckList && n.ChecklistItems != null)
            {
                return n.ChecklistItems.Sum(ci =>
                    (ci.Text ?? string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            }

            return 0;
        }
    }
}
