﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Media;
using ELW.Library.Math;
using GrapthBuilder.Source.Classes;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Practices.Prism.Mvvm;

namespace GrapthBuilder.Source.MVVM.Models
{
    internal class GraphicsModel : BindableBase
    {
        private static readonly ColorSet Colors;

        private const double DefaultRange = 10;
        private const double StepMult = 2;
        

        private readonly ObservableCollection<EquationModel> _equations;
        private Range _currentRange;
        

        #region Properties

        public SeriesCollection Series { get; }

        public IEnumerable<EquationModel> Equations => _equations;

        #endregion


        #region Constructor

        static GraphicsModel()
        {
            Colors = new ColorSet();
        }

        public GraphicsModel()
        {
            _equations = new ObservableCollection<EquationModel>();
            _equations.CollectionChanged += UppdateSeries;

            _currentRange = new Range(-DefaultRange, DefaultRange);
           
            Series = new SeriesCollection();
        }

        #endregion


        #region Public methods

        public void LoadFromFile(string patch)
        {
            if (_equations.Any())
                _equations.Clear();

            if (Series.Any())
                Series.Clear();


            var result = Load(patch);
            foreach (var equation in result)
            {
                _equations.Add(equation);
            }

            OnPropertyChanged("Equations");
        }

        public void AppendFromFile(string patch)
        {
            var result = Load(patch);
            foreach (var equation in result)
            {
                _equations.Add(equation);
            }

            OnPropertyChanged("Equations");
        }

        public void RerangeX(double axisXActualMinValue, double axisXActualMaxValue)
        {
            var seriesList = new List<LineSeries>();
            _currentRange = new Range(axisXActualMinValue, axisXActualMaxValue);
            foreach (var equation in _equations)
            {
                if(!equation.IsEnabled) continue;

                var lineSeries = GetSeries(equation);
                seriesList.Add(lineSeries);
            }


            if (Series.Any())
                Series.Clear();

            Series.AddRange(seriesList);
            OnPropertyChanged("Series");
        }

        public void Uppdate()
        {
            RerangeX(_currentRange.LeftLimit, _currentRange.RightLimit);
        }

        #endregion


        #region Private methods

        private IEnumerable<EquationModel> Load(string patch)
        {
            var resultList = new List<EquationModel>();

            using (var sr = new StreamReader(patch))
            {
                string str;
                while ((str = sr.ReadLine()) != null)
                {
                    var equastion = CreateEquation(str);
                    resultList.Add(equastion);
                }
            }
            return resultList;

        }

        private static EquationModel CreateEquation(string equationStr)
        {
            var preparedExpression = ToolsHelper.Parser.Parse(equationStr);
            var compiledExpression = ToolsHelper.Compiler.Compile(preparedExpression);
            var optimizedExpression = ToolsHelper.Optimizer.Optimize(compiledExpression);

            var equastion = new EquationModel(equationStr + "= y", optimizedExpression, Colors.GetNext() ,StepMult);

            return equastion;
        }

        private void UppdateSeries(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    var equation = item as EquationModel;
                    var series = GetSeries(equation);
                    Series.Add(series);
                }
                OnPropertyChanged("Series");
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                
                OnPropertyChanged("Series");
            }
        }

        private LineSeries GetSeries(EquationModel equation)
        {
            var lineSeries = equation.GetSeriesInRange(_currentRange);
            lineSeries.Fill = Brushes.Transparent;
            lineSeries.Stroke = equation.Brush;
            lineSeries.PointGeometrySize = 1;
            lineSeries.Tag = equation;

            return lineSeries;
        }

        #endregion

    }
    
}
