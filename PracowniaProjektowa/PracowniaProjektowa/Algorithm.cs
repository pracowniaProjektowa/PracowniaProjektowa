using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileHelpers;

namespace PracowniaProjektowa
{
    public class Algorithm
    {
        private const int FUNCTIONS = 35;
        private FileInfo _file;
        private Node[] _nodes;
        // Lists with Node and int that represents index in array nodes
        private List<Tuple<Node, int>> _completeNodes; // lista potencjalnie dobrych rekordów
        private List<Tuple<Node, int>> _lackedNodes; // lista potencjanie wybrakowanych rekordów
        private float deltaTz;
        private float deltaTp;
        private float deltaQ;
        private float deltaG;
        private float deltaPercentageTz;
        private float deltaPercentageTp;
        private float deltaPercentageQ;
        private float deltaPercentageG;
        private String header;

        public Algorithm(FileInfo fileInfo)
        {
            _file = fileInfo;
            _completeNodes = new List<Tuple<Node, int>>();
            _lackedNodes = new List<Tuple<Node, int>>();
            deltaG = 0.0f;
            deltaQ = 0.0f;
            deltaTp = 0.0f;
            deltaTz = 0.0f;
            deltaPercentageG = 0.0f;
            deltaPercentageQ = 0.0f;
            deltaPercentageTp = 0.0f;
            deltaPercentageTz = 0.0f;
        }

        public void FixFile()
        {
            var engine = new FileHelperEngine(typeof(Node));
            _nodes = (Node[])engine.ReadFile(_file.FullName);
            header = engine.HeaderText;
            if (_nodes.Any())
            {
                for (int i = 0; i < _nodes.Length; i++)
                {
                    if (_nodes[i].IsNodeComplete())
                    {
                        Node node = _nodes[i];
                        _completeNodes.Add(new Tuple<Node, int>(node, i));
                        deltaG = Math.Max(deltaG, node.CountDeltaG());
                        deltaQ = Math.Max(deltaQ, node.CountDeltaQ());
                        deltaTz = Math.Max(deltaTz, node.CountDeltaTz());
                        deltaTp = Math.Max(deltaTp, node.CountDeltaTp());
                        deltaPercentageTz = Math.Max(deltaPercentageTz, node.CountPercentageDeltaTz());
                        deltaPercentageG = Math.Max(deltaPercentageG, node.CountPercentageDeltaG());
                        deltaPercentageTp = Math.Max(deltaPercentageTp, node.CountPercentageDeltaTp());
                        deltaPercentageQ = Math.Max(deltaPercentageQ, node.CountPercentageDeltaQ());
                    }
                    else
                    {
                        _lackedNodes.Add(new Tuple<Node, int>(_nodes[i], i));
                    }
                }
                var f = Math.PI / (new DateTime(0).AddYears(1) - new DateTime(0)).TotalMilliseconds / 10000;
                // select data for one year
                List<Tuple<Node, int>> randomNodes = new List<Tuple<Node, int>>();
                for (int i = 0; i < _completeNodes.Count; i += 130)
                {
                    randomNodes.Add(_completeNodes[i]);
                }
                double[] cooeficientsG = FindCoefficients(randomNodes, CoefficientType.G, f);
                double[] cooeficientsQ = FindCoefficients(randomNodes, CoefficientType.Q, f);
                double[] cooeficientsTz = FindCoefficients(randomNodes, CoefficientType.Tz, f);
                double[] cooeficientsTp= FindCoefficients(randomNodes, CoefficientType.Tp, f);
                for (int j = 0; j < _lackedNodes.Count; j++)
                {
                    var node = _lackedNodes[j].Item1;
                    if (!node.IsFLowCorrect())
                    {
                        node.FlowM3h = CountApproximatedValue(cooeficientsG, node.GetDate().Ticks, f);
                    }
                    if (!node.IsPowerCorrect())
                    {
                        node.PowerkW = CountApproximatedValue(cooeficientsQ, node.GetDate().Ticks, f);
                    }
                    if (!node.IsTpCorrect())
                    {
                        node.Tp = CountApproximatedValue(cooeficientsTp, node.GetDate().Ticks, f);
                    }
                    if (!node.IsTzCorrect())
                    {
                        node.Tz = CountApproximatedValue(cooeficientsTz, node.GetDate().Ticks, f);
                    }
                }

            }
            Console.WriteLine("Node : {0} Comple Rows = {1} Lacked Rows = {2}", _file.Name.Replace(".csv", String.Empty),
                _completeNodes.Count, _lackedNodes.Count);
            Console.WriteLine("Found delta Q {0}", deltaQ);
            Console.WriteLine("Found delta G {0}", deltaG);
            Console.WriteLine("Found delta Tz {0}", deltaTz);
            Console.WriteLine("Found delta Tp {0}", deltaTp);
            Console.WriteLine("Found % delta Q {0}", deltaPercentageQ);
            Console.WriteLine("Found % delta G {0}", deltaPercentageG);
            Console.WriteLine("Found % delta Tz {0}", deltaPercentageTz);
            Console.WriteLine("Found % delta Tp {0}", deltaPercentageTp);
            WriteCSVFile(_lackedNodes);

        }

        /// <summary>
        /// Metoda znajduje wymagane współczynniki do aproksymacji funkcjami cosinus i sinus
        /// </summary>
        /// <param name="data">poprawne dane wykorzytsane do znalezienia współczynników</param>
        /// <param name="coefficientType">szykana dana do poprawy</param>
        /// <param name="f">Okres funkcji - przewidywany rok</param>
        /// <returns></returns>
        private double[] FindCoefficients(List<Tuple<Node, int>> data, CoefficientType coefficientType, double f)
        {
            Matrix A;
            var n = data.Count;
            var g = new Matrix(n, FUNCTIONS * 2 + 1);
            for (var i = 0; i < n; i++)
            {
                g[i, 0] = 1;
                for (var j = 2; j < g.cols; j += 2)
                {
                    DateTime recordTime = data[i].Item1.GetDate();
                    g[i, j - 1] = Math.Cos(j * recordTime.Ticks * f);
                    g[i, j] = Math.Sin(j * recordTime.Ticks * f);
                }
            }
            var F = new Matrix(g.cols, 1);
            for (var i = 0; i < g.cols; i++)
            {
                for (var k = 0; k < n; k++)
                {
                    float value = 0.0f;
                    switch (coefficientType)
                    {
                        case CoefficientType.Tz:
                            value = data[k].Item1.Tz;
                            break;
                        case CoefficientType.G:
                            value = data[k].Item1.FlowM3h;
                            break;
                        case CoefficientType.Q:
                            value = data[k].Item1.PowerkW;
                            break;
                        case CoefficientType.Tp:
                            value = data[k].Item1.Tp;
                            break;
                    }
                    F[i, 0] += value * g[k, i];

                }
            }
            var G = g.Transpon() * g;
            A = G.SolveWith(F);
            return A.ToArray();
        }

        /// <summary>
        /// Metoda do wysnaczana jednej wartości, na którą należy podmienić wybraną daną
        /// </summary>
        /// <param name="wsp">współczynniki do aproksymacji</param>
        /// <param name="x">timestamp</param>
        /// <param name="f">Okres funkcji</param>
        /// <returns></returns>
        private float CountApproximatedValue(double[] wsp, double x, double f)
        {
            double ret = wsp[0];
            for (var i = 2; i < wsp.Length; i += 2)
            {
                var arg = (i * x * f) % (2 * Math.PI);
                ret += wsp[i - 1] * Math.Cos(arg);
                ret += wsp[i] * Math.Sin(arg);
            }
            return (float)ret;
        }

        /// <summary>
        /// Metoda do wpisywanie llisty poprawionych danych do pliku csv
        /// </summary>
        /// <param name="dataSource">dnae do wpisania</param>
        private void WriteCSVFile(List<Tuple<Node, int>> dataSource)
        {
       
            try
            {
                //filehelper object
                FileHelperEngine engine = new FileHelperEngine(typeof(Node));
                //csv object
                List<Node> csv = new List<Node>();
                //convert any datasource to csv based object
                foreach (var item in dataSource)
                {
                 csv.Add(item.Item1);
                }
                //give file a name and header text
                string filename = _file.Name.Replace(".csv", String.Empty) + "_new.csv";
                engine.HeaderText = header;
                //save file locally
                engine.WriteFile(filename,csv);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }

    public enum CoefficientType
    {
        Tz,
        Tp,
        Q,
        G
    }
}



