using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics
{
    /// <summary>Represents an order matrix of values and provides for linear algebra operations.</summary>
    public abstract class Matrix
    {
        /// <summary>The array of values for this matrix.</summary>
        public double[] Values { get; protected set; }
        /// <summary>Returns the column-ordered position of this matrix.  Positions follow this pattern:</summary>
        /// <para/>|  0   1   ...  |
        /// <para/>|  ... ... ...  |
        public double this[int index] { get => Values[index]; set => Values[index] = value; }

        /// <summary>The number of rows in this matrix.</summary>
        public virtual int Rows { get => (int)Math.Sqrt(Values.Length); }
        /// <summary>The number of columns in this matrix.</summary>
        public virtual int Columns { get => (int)Math.Sqrt(Values.Length); }

        /// <summary>Returns the determinant of this matrix.</summary>
        public abstract double GetDeterminant();
        /// <summary>Creates a new matrix with the values at the given positions.</summary>
        /// <para/>|  0   1   ...  |
        /// <para/>|  ... ... ...  |
        protected internal Matrix(double[] values) { this.Values = values; }

        /// <summary>Returns whether this matrix is an identity matrix.</summary>
        public Boolean IsIdentity()
        {
            int i = 0;
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    if (Values[i++] != (col == row ? 1 : 0)) return false;
                }
            }
            return true;
        }


    }

    /// <summary>
    /// A data structure which embodies a matrix in 2-dimensional space.
    /// </summary>
    public sealed class Matrix2 : Matrix, ITransformable<Matrix2, Matrix2>
    {
        /// <summary>Ths number of columns.</summary>
        public override int Columns => 2;
        /// <summary>The number of rows.</summary>
        public override int Rows => 2;
        /// <summary>Returns the determinant of this matrix.</summary>
        public override double GetDeterminant()
        {
            return (Values[0] * Values[3]) - (Values[1] * Values[2]);
        }

        Matrix2 ITransformable<Matrix2, Matrix2>.GetTransformed(Matrix2 other)
        {
            return other * this;
        }

        internal Matrix2(double[] values) : base(values) { if (values.Length != 4) throw new ArgumentException("Values array must have exactly 2x2=4 members."); }

#pragma warning disable 1591
        public static Matrix2 operator +(Matrix2 a, Matrix2 b)
        {
            double[] v = new double[4];
            for (int i = 0; i < 4; i++) v[i] = a[i] + b[i];
            return new Matrix2(v);
        }
        public static Matrix2 operator -(Matrix2 a, Matrix2 b)
        {
            double[] v = new double[4];
            for (int i = 0; i < 4; i++) v[i] = a[i] - b[i];
            return new Matrix2(v);
        }
        public static Matrix2 operator -(Matrix2 a)
        {
            double[] v = new double[4];
            for (int i = 0; i < 4; i++) v[i] = -a[i];
            return new Matrix2(v);
        }
        public static Matrix2 operator *(Matrix2 a, Matrix2 b)
        {
            double[] v = new double[4];
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    double sum = 0;
                    for (int i = 0; i < 2; i++)
                        sum += a[(y * 2) + i] * b[(i * 2) + x];
                    v[x + (y * 2)] = sum;
                }
            }
            return new Matrix2(v);
        }
        public static Matrix2 operator *(Matrix2 m, double d)
        {
            double[] v = new double[4];
            for (int i = 0; i < 4; i++) v[i] = m[i] * d;
            return new Matrix2(v);
        }
        public static Matrix2 operator /(Matrix2 m, double d)
        {
            double[] v = new double[4];
            for (int i = 0; i < 4; i++) v[i] = m[i] / d;
            return new Matrix2(v);
        }

#pragma warning restore 1591
    }

    /// <summary>
    /// A data structure which embodies a matrix in 3-dimensional affine space.
    /// </summary>
    public sealed class Matrix3 : Matrix, ITransformable<Matrix3, Matrix3>
    {
        /// <summary>Ths number of columns.</summary>
        public override int Columns => 3;
        /// <summary>The number of rows.</summary>
        public override int Rows => 3;
        internal Matrix3(double[] values) : base(values) { if (values.Length != 9) throw new ArgumentException("Values array must have exactly 3x3=9 members."); }
        /// <summary>Returns a 3x3 identity matrix.</summary>
        public static Matrix3 Identity() { return new Matrix3(new double[] { 1, 0, 0, 0, 1, 0, 0, 1 }); }
        /// <summary>Returns a non-affine, 4x4 version of this matrix.</summary>
        public Matrix4 NonAffine()
        {
            double[] v = new double[16];
            for (int i = 0; i < 3; i++) v[i] = Values[i];
            for (int i = 3; i < 6; i++) v[i + 1] = Values[i];
            for (int i = 6; i < 9; i++) v[i + 2] = Values[i];
            return new Matrix4(v);
        }


        private Matrix2 GetPartial(int omitted)
        {
            double[] v = new double[4];
            int i = 0;
            for (int y = 0; y < 3; y++)
            {
                if (y == omitted / 3) continue;
                for (int x = 0; x < 3; x++)
                {
                    if (x == x % 3) continue;
                    v[i++] = Values[(y * 3) + x];
                }
            }
            return new Matrix2(v);

        }

        /// <summary>Returns the determinant of this matrix.</summary>
        public double Determinant()
        {
            double sign = 1;
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    sign *= -1;
                }

            }
            throw new NotImplementedException();
        }
        /// <summary>Returns the determinant of this matrix.</summary>
        public override double GetDeterminant()
        {
            throw new NotImplementedException();
        }

#pragma warning disable 1591
        public static Matrix3 operator +(Matrix3 a, Matrix3 b)
        {
            double[] v = new double[9];
            for (int i = 0; i < 9; i++) v[i] = a[i] + b[i];
            return new Matrix3(v);
        }
        public static Matrix3 operator -(Matrix3 a, Matrix3 b)
        {
            double[] v = new double[9];
            for (int i = 0; i < 9; i++) v[i] = a[i] - b[i];
            return new Matrix3(v);
        }
        public static Matrix3 operator -(Matrix3 a)
        {
            double[] v = new double[9];
            for (int i = 0; i < 9; i++) v[i] = -a[i];
            return new Matrix3(v);
        }
        public static Matrix3 operator *(Matrix3 a, Matrix3 b)
        {
            double[] v = new double[9];
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    double sum = 0;
                    for (int i = 0; i < 3; i++)
                        sum += a[(y * 3) + i] * b[(i * 3) + x];
                    v[x + (y * 3)] = sum;
                }
            }
            return new Matrix3(v);
        }
        public static Matrix3 operator *(Matrix3 m, double d)
        {
            double[] v = new double[9];
            for (int i = 0; i < 9; i++) v[i] = m[i] * d;
            return new Matrix3(v);
        }
        public static Matrix3 operator /(Matrix3 m, double d)
        {
            double[] v = new double[9];
            for (int i = 0; i < 9; i++) v[i] = m[i] / d;
            return new Matrix3(v);
        }
        public static Matrix4 operator +(Matrix3 m, Geometry.Vector3 v) { return m.NonAffine() + v; }
        public static Matrix4 operator -(Matrix3 m, Geometry.Vector3 v) { return m.NonAffine() - v; }
        Matrix3 ITransformable<Matrix3, Matrix3>.GetTransformed(Matrix3 other)
        {
            return other * this;
        }
#pragma warning restore 1591

    }

    /// <summary>
    /// A data structure which embodies a matrix in 4-dimensional space, or a 3-dimensional nonaffine space.
    /// </summary>
    public class Matrix4 : Matrix, ITransformable<Matrix4, Matrix4>
    {

        /// <summary>Returns the affine, 3x3 version of this matrix.</summary>
        public Matrix3 Affine()
        {
            double[] v = new double[9];
            for (int i = 0; i < 3; i++) v[i] = Values[i];
            for (int i = 4; i < 6; i++) v[i - 1] = Values[i];
            for (int i = 8; i < 10; i++) v[i - 2] = Values[i];
            return new Matrix3(v);
        }

        /// <summary>Ths number of columns.</summary>
        public override int Columns => 4;
        /// <summary>The number of rows.</summary>
        public override int Rows => 4;

        /// <summary>Returns whether this matrix can be converted to affine space without loss of information.  An affine matrix will have 
        /// the following form:
        /// <para/>|  ?   ?   ?   0  |
        /// <para/>|  ?   ?   ?   0  |
        /// <para/>|  ?   ?   ?   0  |
        /// <para/>|  0   0   0   1  |
        /// </summary>
        public bool IsAffine()
        {
            for (int i = 12; i <= 14; i++) if (Values[i] != 0) return false;
            for (int i = 3; i <= 11; i += 4) if (Values[i] != 0) return false;
            return Values[15] == 1;
        }

        /// <summary>Returns the determinant of this matrix.</summary>
        public double Determinant()
        {
            throw new NotImplementedException();
        }

        /// <summary>Returns the determinant of this matrix.</summary>
        public override double GetDeterminant()
        {
            throw new NotImplementedException();
        }

        internal Matrix4(double[] values) : base(values) { if (values.Length != 16) throw new ArgumentException("Values array must have exactly 4x4=16 members."); }



#pragma warning disable 1591
        public static Matrix4 operator +(Matrix4 a, Matrix4 b)
        {
            double[] v = new double[16];
            for (int i = 0; i < 16; i++) v[i] = a[i] + b[i];
            return new Matrix4(v);
        }
        public static Matrix4 operator -(Matrix4 a, Matrix4 b)
        {
            double[] v = new double[16];
            for (int i = 0; i < 16; i++) v[i] = a[i] - b[i];
            return new Matrix4(v);
        }
        public static Matrix4 operator -(Matrix4 a)
        {
            double[] v = new double[16];
            for (int i = 0; i < 16; i++) v[i] = -a[i];
            return new Matrix4(v);
        }
        public static Matrix4 operator *(Matrix4 a, Matrix4 b)
        {
            double[] v = new double[16];
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    double sum = 0;
                    for (int i = 0; i < 4; i++)
                        sum += a[(y * 4) + i] * b[(i * 4) + x];
                    v[x + (y * 4)] = sum;
                }
            }
            return new Matrix4(v);
        }
        public static Matrix4 operator *(Matrix4 m, double d)
        {
            double[] v = new double[16];
            for (int i = 0; i < 16; i++) v[i] = m[i] * d;
            return new Matrix4(v);
        }
        public static Matrix4 operator /(Matrix4 m, double d)
        {
            double[] v = new double[16];
            for (int i = 0; i < 16; i++) v[i] = m[i] / d;
            return new Matrix4(v);
        }
        public static Matrix4 operator +(Matrix4 m, Geometry.Vector3 v)
        {
            double[] vals = new double[16];
            m.Values.CopyTo(vals, 0);
            vals[3] += v.X;
            vals[7] += v.Y;
            vals[11] += v.Z;
            return new Matrix4(vals);
        }
        public static Matrix4 operator -(Matrix4 m, Geometry.Vector3 v)
        {
            double[] vals = new double[16];
            m.Values.CopyTo(vals, 0);
            vals[3] -= v.X;
            vals[7] -= v.Y;
            vals[11] -= v.Z;
            return new Matrix4(vals);
        }

        Matrix4 ITransformable<Matrix4, Matrix4>.GetTransformed(Matrix4 other)
        {
            return other * this;
        }
#pragma warning restore 1591
    }
}
