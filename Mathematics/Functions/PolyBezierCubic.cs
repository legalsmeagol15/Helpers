using Mathematics.Calculus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mathematics.Functions
{
    /// <summary>
    /// A mutable data structure that exists as a collection of BezierCubic objects.
    /// </summary>
    public sealed class PolyBezierCubic : IDifferentiable<Point>
    {
        //TODO:  Validate PolyBezierCubic
        public PolyBezierCubic()
        {
            _Points = new ObservableCollection<Point>();
            _Points.CollectionChanged += Points_CollectionChanged;            
        }

        private bool _IsClosed = false;
        public bool IsClosed { get { return _IsClosed; } set { _IsClosed = value; } }

        private double _Curvature = 0.33;
        public double Curvature { get { return _Curvature; } set { _Curvature = value; } }

        public IList<Point> Points { get { return _Points; } }
        private readonly ObservableCollection<Point> _Points;

        private List<BezierCubic> _Curves;
        public Point Evaluate(double t)
        {
            if (t < 0.0) throw new IndexOutOfRangeException("PolyBezierCubic has no parametric value at negative t-values.");
            if (t > _Curves.Count)
                throw new IndexOutOfRangeException("Invalid parametric t-value " + t + ".  This PolyBezierCubic has only " + _Curves.Count
                                                   + " curves, so t-value must be between 0.0 and " + (double)_Curves.Count + ".");
            else if (t == _Curves.Count)            
                return _Curves[_Curves.Count - 1].End;

            int idx = (int)t;
            return _Curves[idx].Evaluate(t - idx);
        }

        
        private void Points_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            HashSet<int> updates = new HashSet<int>();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    //If items were appended, that's the easy case.
                    if (e.NewStartingIndex >= _Curves.Count)
                    {
                        int targetPtCount = _IsClosed ? _Points.Count : _Points.Count - 1;
                        while (_Curves.Count < targetPtCount)
                        {
                            int i = _Curves.Count;
                            BezierCubic newCurve = GetCurve(i);
                            _Curves.Add(newCurve);                            
                        }
                    }
                    //If items weren't appended but were inserted, insert in a similar manner.
                    else
                    {
                        BezierCubic[] insertCurves = new BezierCubic[e.NewItems.Count];                        
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            int index = i + e.NewStartingIndex;
                            BezierCubic newCurve = GetCurve(index);
                            insertCurves[i] = newCurve;                            
                        }
                        _Curves.InsertRange(e.NewStartingIndex, insertCurves);
                        
                    }
                    //An 'add' can change the shape of the two prior, and the one succeeding, indices due to the changed given index.
                    updates.Add(e.NewStartingIndex - 2);
                    updates.Add(e.NewStartingIndex - 1);
                    updates.Add(e.NewStartingIndex + e.NewItems.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    //Have only implemented replaces for a single point at a time.
                    if (e.OldStartingIndex != e.NewStartingIndex || e.OldItems.Count != 1 || e.NewItems.Count != 1)
                        throw new NotImplementedException();
                    updates.Add(e.NewStartingIndex - 2);
                    updates.Add(e.NewStartingIndex - 1);
                    updates.Add(e.NewStartingIndex);
                    updates.Add(e.NewStartingIndex + 1);                    
                    break;
                case NotifyCollectionChangedAction.Remove:

                    //A removal at the end of a non-closed PolyBezierCubic is really a removal at the prior curve.
                    if (e.OldStartingIndex == _Points.Count && !_IsClosed)                    
                        _Curves.RemoveRange(e.OldStartingIndex - 1, e.OldItems.Count);
                    else                    
                        _Curves.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        
                    //A removal can affect the prior two, plus the new one at this index, plus the next one.
                    updates.Add(e.OldStartingIndex - 2);
                    updates.Add(e.OldStartingIndex - 1);
                    updates.Add(e.OldStartingIndex);
                    updates.Add(e.OldStartingIndex + 1);

                    break;
                default:
                    throw new NotImplementedException();
            }

            //Step #2b - update the segments adjacent to the modified points' segments.            
            if (updates.Remove(-2) && _IsClosed) updates.Add(_Points.Count - 2);
            if (updates.Remove(-1) && _IsClosed) updates.Add(_Points.Count - 1);
            if (updates.Contains(_Points.Count - 1) && !_IsClosed) updates.Remove(_Points.Count - 1);
            if (updates.Remove(_Points.Count) && _IsClosed) updates.Add(0);
            if (updates.Remove(_Points.Count + 1) && _IsClosed) updates.Add(1);
            foreach (int index in updates)
            {
                BezierCubic newCurve = GetCurve(index);
                _Curves[index] = newCurve;                
            }            
        }


        private BezierCubic GetCurve(int index)
        {
            //Step #1 - determine what the applicable points are.
            Point ptPrev, ptStart = _Points[index], ptEnd, ptNext;
            if (_Points.Count < 2)
                throw new InvalidOperationException("Incomplete PolyBezierCurve.");
            else if (_Points.Count == 2)
            {
                if (index != 0) throw new InvalidOperationException("For a two-point PolyBezierCurve, cannot get points at any index but 0.");
                ptPrev = ptStart;
                ptEnd = _Points[1];
                ptNext = ptEnd;
            }
            else
            {
                int i0 = index - 1, i2 = index + 1, i3 = index + 2;
                if (i0 < 0) i0 = _IsClosed ? _Points.Count - 1 : 0;
                if (index == _Points.Count - 1 && !_IsClosed)
                    throw new InvalidOperationException("Non-closed PolyBezierCurves can't have a curve segment at the last point.");
                if (i2 >= _Points.Count) i2 = _IsClosed ? 0 : _Points.Count - 1;
                if (i3 >= _Points.Count) i3 = _IsClosed ? (i3 - _Points.Count) : _Points.Count - 1;

                ptPrev = _Points[i0];
                ptEnd = _Points[i2];
                ptNext = _Points[i3];
            }

            //Step #2 - determine the vector of exit at the start and end points for this segment.
            Vector vectorStart = ptEnd - ptPrev;
            Vector vectorEnd = ptStart - ptNext;
            if (vectorStart.X != 0.0 || vectorStart.Y != 0.0) vectorStart.Normalize();
            if (vectorEnd.X != 0.0 || vectorEnd.Y != 0.0) vectorEnd.Normalize();            
            double tensor = _Curvature * Operations.GetDistance(ptStart, ptEnd);

            //Step #3 - set the control points as the start and end points plus the vectors of exit.
            Point controlA = ptStart + (vectorStart * tensor);
            Point controlB = ptEnd + (vectorEnd * tensor);

            //Finally, return the new Bezier.
            return new BezierCubic(ptStart, controlA, controlB, ptEnd);
        }

        public IDifferentiable<Point> GetDerivative()
        {
            throw new NotImplementedException();
        }

        public IDifferentiable<Point> GetIntegral(double constant = 0)
        {
            throw new NotImplementedException();
        }

        public IDifferentiable<Point> GetLength()
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point> IDifferentiable<Point>.GetDerivative()
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point> IDifferentiable<Point>.GetIntegral(double constant)
        {
            throw new NotImplementedException();
        }

        Point IDifferentiable<Point>.Evaluate(double value)
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point> IDifferentiable<Point>.GetLength()
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point> IDifferentiable<Point>.GetSum(IDifferentiable<Point> other)
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point> IDifferentiable<Point>.GetDifference(IDifferentiable<Point> other)
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point> IDifferentiable<Point>.GetMultiple(IDifferentiable<Point> factor)
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point> IDifferentiable<Point>.GetQuotient(IDifferentiable<Point> divisor)
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point> IDifferentiable<Point>.GetNegation()
        {
            throw new NotImplementedException();
        }
    }
}
