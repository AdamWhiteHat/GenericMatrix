using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;

namespace ExtendedArithmetic
{
	public class Matrix<T> : IEquatable<Matrix<T>>
	{
		public int RowCount { get { return Rows.Length; } }
		public int ColumnCount { get { return Rows.Any() ? Rows.First().Dimensions : 0; } }

		public Vector<T>[] Rows { get; private set; }
		public Vector<T>[] Columns { get { return Matrix<T>.GetColumns(this); } }

		public bool IsSquare() => (this.RowCount == this.ColumnCount);

		public Vector<T> this[int rowIndex]
		{
			get { return Rows[rowIndex]; }
		}

	public T this[int rowIndex, int columnIndex]
	{
		get { return Rows[rowIndex][columnIndex]; }
		set { Rows[rowIndex][columnIndex] = value; }
	}

		private static Func<T, T, T> addT;
		private static Func<T, T, T> multiplyT;
		private static Func<T, T, T> modT;

		static Matrix()
		{
			addT = GenericArithmeticFactory<T>.GetBinaryExpression(ExpressionType.Add);
			multiplyT = GenericArithmeticFactory<T>.GetBinaryExpression(ExpressionType.Multiply);
			modT = GenericArithmeticFactory<T>.GetBinaryExpression(ExpressionType.Modulo);
		}

		private Matrix()
		{
			Rows = new Vector<T>[0];
		}

		public Matrix(IEnumerable<Vector<T>> rows)
		{
			if (rows.Select(v => v.Dimensions).Distinct().Count() != 1)
			{
				throw new Exception($"Each vector must have the same number of elements. Irregular matrices are not supported.");
			}

			Rows = rows.ToArray();
		}

		public Matrix(params Vector<T>[] rows)
			: this(rows.ToList<Vector<T>>())
		{
		}

	/// <summary>Clones a matrix by creating a new copy of every element in every row into a new instance.</summary>
	public Matrix<T> Clone()
	{
		return new Matrix<T>(this.Rows.Select(r => r.Clone()));
	}

		public static Matrix<T> Transpose(Matrix<T> matrix)
		{
			return new Matrix<T>(GetColumns(matrix));
		}

		public static Vector<T>[] GetColumns(Matrix<T> matrix)
		{
			return Enumerable.Range(0, matrix.ColumnCount).Select(index => new Vector<T>(matrix.Rows.Select(r => r[index]).ToArray())).ToArray();
		}

		#region Matrix Arithmetic Operations

		/// <summary>Matrix Addition</summary>
		public static Matrix<T> Add(Matrix<T> left, Matrix<T> right)
		{
			if (!left.IsSquare() || !right.IsSquare() || left.RowCount != right.RowCount)
			{
				throw new Exception($"Matrix {nameof(left)} and matrix {nameof(right)} must both be square matrices of the same dimensions.");
			}

			int arrayLength = left.RowCount;
			List<Vector<T>> result = new List<Vector<T>>();

			int counter = 0;
			while (counter < arrayLength)
			{
				result.Add(new Vector<T>(Enumerable.Repeat(default(T), arrayLength).ToArray()));
				counter++;
			}

			for (int i = 0; i < arrayLength; i++)
			{
				for (int j = 0; j < arrayLength; j++)
				{
					result[i][j] = addT(left.Rows[i][j], right.Rows[i][j]);
				}
			}

			return new Matrix<T>(result);
		}

		/// <summary>Matrix Multiplication</summary>
		public static Matrix<T> Multiply(Matrix<T> left, Matrix<T> right)
		{
			if (!left.IsSquare() || !right.IsSquare() || left.RowCount != right.RowCount)
			{
				throw new Exception($"Matrix {nameof(left)} and matrix {nameof(right)} must both be square matrices of the same dimensions.");
			}

			int n = left.RowCount;
			List<T[]> results = new List<T[]>();
			int counter = 0;
			while (counter < n)
			{
				results.Add(Enumerable.Repeat(default(T), n).ToArray());
				counter++;
			}

			int i, j, m;
			for (i = 0; i < n; i++)
			{
				for (j = 0; j < n; j++)
				{
					T value = default(T);
					for (m = 0; m < n; m++)
					{
						value = addT(value, multiplyT(left.Rows[i][m], right.Rows[m][j]));
					}
					results[i][j] = value;
				}
			}

			return new Matrix<T>(results.Select(arr => (Vector<T>)new Vector<T>(arr)).ToList());
		}

		public static Matrix<T> MultiplyMod(Matrix<T> left, Matrix<T> right, T mod)
		{
			if (!left.IsSquare() || !right.IsSquare() || left.RowCount != right.RowCount)
			{
				throw new Exception($"Matrix {nameof(left)} and matrix {nameof(right)} must both be square matrices of the same dimensions.");
			}

			int n = left.RowCount;
			List<T[]> results = new List<T[]>();
			int counter = 0;
			while (counter < n)
			{
				results.Add(Enumerable.Repeat(default(T), n).ToArray());
				counter++;
			}

			int i, j, m;
			for (i = 0; i < n; i++)
			{
				for (j = 0; j < n; j++)
				{
					T value = default(T);
					for (m = 0; m < n; m++)
					{
						value = modT(addT(value, modT(multiplyT(left.Rows[i][m], right.Rows[m][j]), mod)), mod);
					}
					results[i][j] = value;
				}
			}

			return new Matrix<T>(results.Select(arr => (Vector<T>)new Vector<T>(arr)).ToList());
		}

		/// <summary>
		/// Exponentiation by squaring
		/// </summary>
		public static Matrix<T> Pow(Matrix<T> matrix, BigInteger exponent)
		{
			if (!matrix.IsSquare())
			{
				throw new Exception($"This implementation of {nameof(Pow)} expects the input to be a square matrix.");
			}

			Matrix<T> b = new Matrix<T>(matrix.Rows);
			Matrix<T> result = Matrix<T>.Factory.IdentityMatrix(matrix.ColumnCount);

			BigInteger exp = BigInteger.Abs(exponent);
			while (exp > 0)
			{
				if ((exp % 2) == 1) // If exponent is odd
				{
					result = Multiply(result, b);

					exp = exp - 1;
					if (exp == 0)
					{
						break;
					}
				}

				b = Multiply(b, b);
				exp = exp / 2;
			}
			return result;
		}

		public static Matrix<T> PowMod(Matrix<T> matrix, BigInteger exponent, T mod)
		{
			if (!matrix.IsSquare())
			{
				throw new Exception($"This implementation of {nameof(PowMod)} expects the input to be a square matrix.");
			}

			Matrix<T> b = new Matrix<T>(matrix.Rows.Select(v => Vector<T>.ScalarMod(v, mod)));
			Matrix<T> result = Matrix<T>.Factory.IdentityMatrix(matrix.ColumnCount);

			BigInteger exp = BigInteger.Abs(exponent);
			while (exp > 0)
			{
				if ((exp % 2) == 1) // If exponent is odd
				{
					result = Matrix<T>.MultiplyMod(result, b, mod);

					exp = exp - 1;
					if (exp == 0)
					{
						break;
					}
				}

				b = Matrix<T>.MultiplyMod(b, b, mod);
				exp = exp / 2;
			}
			return result;
		}

		public static Matrix<T> Mod(Matrix<T> matrix, T mod)
		{
			List<Vector<T>> results = new List<Vector<T>>();
			foreach (Vector<T> vector in matrix.Rows)
			{
				results.Add(Vector<T>.ScalarMod(vector, mod));
			}
			return new Matrix<T>(results);
		}

		#endregion

		#region Scalar Arithmetic Operations

		/// <summary>Scalar Addition</summary>
		public static Matrix<T> Add(Matrix<T> matrix, T scalar)
		{
			return new Matrix<T>(matrix.Rows.Select(row => Vector<T>.ScalarAdd(row, scalar)));
		}

		/// <summary>Scalar Multiplication</summary>
		public static Matrix<T> Multiply(Matrix<T> matrix, T scalar)
		{
			return new Matrix<T>(matrix.Rows.Select(row => Vector<T>.ScalarMultiply(row, scalar)));
		}

		#endregion

		#region Advanced Matrix Functions

		/// <summary>KroneckerProduct</summary>
		public static Matrix<T> KroneckerProduct(Matrix<T> left, Matrix<T> right)
		{
			List<Vector<T>> result = new List<Vector<T>>();

			foreach (Vector<T> row in left.Rows)
			{
				IEnumerable<Matrix<T>> rowOfMatrix = row.Elements.Select(scalar => Matrix<T>.Multiply(right, scalar));

				int rowIndex = 0;
				int rowCount = right.RowCount;
				while (rowIndex < rowCount)
				{
					result.Add(
						new Vector<T>(rowOfMatrix.SelectMany(m => m.Rows[rowIndex].Elements).ToArray()));
					rowIndex++;
				}
			}

			return new Matrix<T>(result);
		}

		/// <summary>
		/// Matrix Vectorization;
		/// Converts the matrix into a vector
		/// by stacking the columns of the matrix on top of one another.
		/// </summary>
		public static Vector<T> Vectorize(Matrix<T> matrix)
		{
			return new Vector<T>(GetColumns(matrix).SelectMany(v => v.Elements).ToArray());
		}

		/// <summary>
		/// Gets cofactor matrix.
		/// Excludes the row and column specified and returns a matrix of 1 dimension lower than the input matrix.
		/// </summary>
		public static Matrix<T> GetCofactor(Matrix<T> matrix, int row, int column)
		{
			if (!matrix.IsSquare())
			{
				throw new Exception("Matrix must be square.");
			}

			int i = 0;
			int j = 0;

			int dim = matrix.ColumnCount;

			Matrix<T> result = Matrix<T>.Factory.ZeroMatrix(dim - 1);

			// Looping for each element of the matrix
			for (int rowCounter = 0; rowCounter < dim; rowCounter++)
			{
				for (int colCounter = 0; colCounter < dim; colCounter++)
				{
					// Copying into temporary matrix only those element
					// which are not in given row and column
					if (rowCounter != row && colCounter != column)
					{
						result[i][j++] = matrix[rowCounter][colCounter];

						// Row is filled, so increase row index and
						// reset col index
						if (j == dim - 1)
						{
							j = 0;
							i++;
						}
					}
				}
			}

			return result;
		}

		/// <summary>Recursive function for finding determinant of matrix.</summary>
		public static T GetDeterminant(Matrix<T> matrix)
		{
			Func<T, T, T> multiplyOperation = GenericArithmeticFactory<T>.GetBinaryExpression(ExpressionType.Multiply);
			Func<T, T, T> addOperation = GenericArithmeticFactory<T>.GetBinaryExpression(ExpressionType.Add);

			int dim = matrix.ColumnCount;
			T result = GenericArithmeticFactory<T>.Zero; // Initialize result

			// Base case : if matrix contains single element
			if (dim == 1)
			{
				return matrix[0][0];
			}

			T sign = GenericArithmeticFactory<T>.One; // To store sign multiplier

			Matrix<T> temp; // To store cofactors

			List<T> subresultants = new List<T>();

			// Iterate for each element of first row
			for (int f = 0; f < dim; f++)
			{
				// Getting Cofactor of A[0,f]
				temp = GetCofactor(matrix, 0, f);
				T det = GetDeterminant(temp);
				subresultants.Add(det);
				result = addOperation(result, multiplyOperation(multiplyOperation(sign, matrix[0][f]), det));

				// terms are to be added with alternate sign
				sign = multiplyOperation(sign, GenericArithmeticFactory<T>.MinusOne);
			}

			return result;
		}

		/// <summary>Function to get adjoint of a matrix.</summary>
		public static Matrix<T> Adjoint(Matrix<T> matrix)
		{
			Func<T, T, T> multiplyOperation = GenericArithmeticFactory<T>.GetBinaryExpression(ExpressionType.Multiply);

			int dim = matrix.ColumnCount;
			Matrix<T> result = Matrix<T>.Factory.ZeroMatrix(dim);
			if (dim == 1)
			{
				result[0][0] = GenericArithmeticFactory<T>.One;
				return result;
			}

			// temp is used to store cofactors of [,]A
			T sign = GenericArithmeticFactory<T>.One;
			Matrix<T> temp;

			for (int i = 0; i < dim; i++)
			{
				for (int j = 0; j < dim; j++)
				{
					// Get cofactor of A[i,j]
					temp = GetCofactor(matrix, i, j);

					// sign of adj[j,i] positive if sum of row
					// and column indexes is even.
					sign = ((i + j) % 2 == 0) ? GenericArithmeticFactory<T>.One : GenericArithmeticFactory<T>.MinusOne;

					// Interchanging rows and columns to get the
					// transpose of the cofactor matrix
					result[j][i] = multiplyOperation(sign, GetDeterminant(temp));
				}
			}

			return result;
		}

		/// <summary>
		/// Function to calculate the inverse of a matrix. 
		/// Throws and exception if matrix is singular
		/// </summary>
		public static Matrix<T> Inverse(Matrix<T> matrix)
		{
			int dim = matrix.ColumnCount;

			// Find determinant of [,]A
			T det = GetDeterminant(matrix);
			if (GenericArithmetic<T>.Equal(det, GenericArithmetic<T>.Zero))
			{
				throw new Exception("Singular matrix, can't find its inverse");
			}

			Func<T, T, T> divideOperation = GenericArithmeticFactory<T>.GetBinaryExpression(ExpressionType.Divide);

			Matrix<T> inverse = Matrix<T>.Factory.ZeroMatrix(dim);

			// Find adjoint
			Matrix<T> adj = Adjoint(matrix);

			// Find Inverse using formula "inverse(A) = adj(A)/det(A)"
			for (int i = 0; i < dim; i++)
			{
				for (int j = 0; j < dim; j++)
				{
					inverse[i][j] = divideOperation(adj[i][j], det);
				}
			}

			return inverse;
		}

		/// <summary>Turn a vector into a matrix by placing its entries along the diagonal of a matrix that is otherwise all zeros</summary>
		public static Matrix<T> Diag(Vector<T> vector)
		{
			int dim = vector.Dimensions;

			Matrix<T> zeros = Factory.FillWith(GenericArithmeticFactory<T>.Zero, dim);

			int index = 0;
			foreach (T val in vector.Elements)
			{
				zeros[index][index] = val;
				index++;
			}

			return zeros;
		}

		#endregion

		#region Public Overrides

		public static bool Equals(Matrix<T> left, Matrix<T> right)
		{
			if (left.RowCount != right.RowCount)
			{
				return false;
			}
			if (left.ColumnCount != right.ColumnCount)
			{
				return false;
			}

			int rowCount = left.RowCount;
			int colCount = left.ColumnCount;

			int i, j;
			for (i = 0; i < rowCount; i++)
			{
				for (j = 0; j < colCount; j++)
				{
					if (!left.Rows[i][j].Equals(right.Rows[i][j]))
					{
						return false;
					}
				}
			}
			return true;
		}

		public bool Equals(Matrix<T> other)
		{
			return Matrix<T>.Equals(this, other);
		}

		public override string ToString()
		{
			return ToString((e) => e.ToString());
		}

		public string ToString(Func<T, string> elementFormattingFunction)
		{
			int maxElementLen = Rows.Select(v => v.Elements.Max()).Max().ToString().Length;
			int minElementLen = Rows.Select(v => v.Elements.Min()).Min().ToString().Length;
			int padLength = Math.Max(maxElementLen, minElementLen);

			StringBuilder result = new StringBuilder(Environment.NewLine);
			foreach (Vector<T> row in Rows)
			{
				result.AppendLine($"[ {string.Join(", ", row.Elements.Select(e => elementFormattingFunction.Invoke(e).PadLeft(padLength)))} ]");
			}
			return result.ToString();
		}

		#endregion

		/// /// <summary>Matrix<T> Factory</summary>
		public static class Factory
		{
			public static Matrix<T> FillWith(T value, int dimension)
			{
				List<Vector<T>> rowResults = new List<Vector<T>>();

				int index = 0;
				while (index < dimension)
				{
					T[] newRow = Enumerable.Repeat(value, dimension).ToArray();
					rowResults.Add(new Vector<T>(newRow));
					index++;
				}

				return new Matrix<T>(rowResults.ToArray());
			}

			public static Matrix<T> ZeroMatrix(int dimension)
			{
				T zero = GenericArithmeticFactory<T>.Zero;

				List<Vector<T>> rowResults = new List<Vector<T>>();

				int index = 0;
				while (index < dimension)
				{
					T[] newRow = Enumerable.Repeat(zero, dimension).ToArray();
					rowResults.Add(new Vector<T>(newRow));
					index++;
				}

				return new Matrix<T>(rowResults.ToArray());
			}

			public static Matrix<T> IdentityMatrix(int dimension)
			{
				T zero = GenericArithmeticFactory<T>.Zero;
				T one = GenericArithmeticFactory<T>.One;

				List<Vector<T>> rowResults = new List<Vector<T>>();

				int index = 0;
				while (index < dimension)
				{
					T[] newRow = Enumerable.Repeat(zero, dimension).ToArray();
					newRow[index] = one;
					rowResults.Add(new Vector<T>(newRow));
					index++;
				}

				return new Matrix<T>(rowResults.ToArray());
			}

			/*
			public static Matrix<T> SylvesterMatrix(ExtendedArithmetic.Polynomial<T> p, ExtendedArithmetic.Polynomial<T> q)
			{
				ExtendedArithmetic.Polynomial<T> min = p;
				ExtendedArithmetic.Polynomial<T> max = q;
				if (q.CompareTo(p) < 0)
				{
					min = q;
					max = p;
				}

				int minDegree = min.Degree + 1;
				int maxDegree = max.Degree + 1;
				int dimension = min.Degree + max.Degree;

				int offset = 0;
				int rowsRemaining = min.Degree;
				List<T> maxElements = Enumerable.Range(1, maxDegree).Select(i => max[maxDegree - i]).ToList();

				List<Vector<T>> rows = new List<Vector<T>>();

				while (rowsRemaining > 0)
				{
					rows.Add(new Vector<T>(maxElements.PadLeft(maxDegree + offset).PadRight(dimension).ToArray()));

					rowsRemaining--;
					offset++;
				}

				offset = 0;
				rowsRemaining = max.Degree;
				IEnumerable<T> minElements = Enumerable.Range(1, minDegree).Select(i => min[minDegree - i]);

				while (rowsRemaining > 0)
				{
					rows.Add(new Vector<T>(minElements.PadLeft(minDegree + offset).PadRight(dimension).ToArray()));

					rowsRemaining--;
					offset++;
				}

				return new Matrix<T>(rows);
			}
			*/
		}
	}
}
