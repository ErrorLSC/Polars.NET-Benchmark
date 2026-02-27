using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Polars.CSharp;
using static Polars.CSharp.Polars;
using System.Numerics.Tensors;
using MathNet.Numerics.LinearAlgebra;
using MathNetDouble = MathNet.Numerics.LinearAlgebra.Double;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<CosineBenchmark>();
    }
}

public static class PolarsVectorExtensions
{
    // Cos(A, B) = (A . B) / (|A| * |B|)
    public static Expr CosineSimilarity(this Expr left, Expr right)
        => left.Dot(right) / (left.L2Norm() * right.L2Norm());

    // public static Expr Dot(this Expr left, Expr right) => (left * right).Array.Sum();
    public static Expr L2Norm(this Expr col) => col.Pow(2).Array.Sum().Sqrt();
}

[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
[MemoryDiagnoser]
public class CosineBenchmark
{
    // Scale：500,000 rows x 128 dims 
    private const int Rows = 500_000;
    private const int Dims = 128;

    private double[,] _matrix;
    private double[] _queryVec;
    
    // Polars
    private DataFrame _df;
    private Series _querySeries;

    // Math.NET
    private Matrix<double> _mathMatrix;
    private Vector<double> _mathQuery;

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine($"[Setup] Init {Rows}x{Dims} Vector...");
        _matrix = new double[Rows, Dims];
        _queryVec = new double[Dims];

        // Data Generation
        Parallel.For(0, Rows, i =>
        {
            var rng = Random.Shared;
            for (int j = 0; j < Dims; j++) _matrix[i, j] = rng.NextDouble();
        });
        for (int j = 0; j < Dims; j++) _queryVec[j] = 0.5;

        // Prepare Polars
        var rawSeries = new Series("vec", _matrix);
        var arraySeries = rawSeries.Cast(DataType.Array(DataType.Float64, Dims));
        _df = new DataFrame(arraySeries);
        var qRaw = Series.From("q", new[] { _queryVec });
        _querySeries = qRaw.Cast(DataType.Array(DataType.Float64, Dims));

        // Prepare Math.NET
        _mathMatrix = MathNetDouble.DenseMatrix.OfArray(_matrix);
        _mathQuery = MathNetDouble.DenseVector.OfArray(_queryVec);
    }

    // ==========================================
    // TensorPrimitives
    // ==========================================
    [Benchmark(Baseline = true)]
    public double TensorPrimitives_CosSim()
    {
        double sum = 0;
        unsafe
        {
            fixed (double* ptrM = _matrix)
            fixed (double* ptrQ = _queryVec)
            {
                var spanM = new ReadOnlySpan<double>(ptrM, _matrix.Length);
                var spanQ = new ReadOnlySpan<double>(ptrQ, _queryVec.Length);
                
                for (int i = 0; i < Rows; i++)
                {
                    var rowSpan = spanM.Slice(i * Dims, Dims);
                    sum += TensorPrimitives.CosineSimilarity(rowSpan, spanQ);
                }
            }
        }
        return sum;
    }

    // ==========================================
    // Polars.NET
    // ==========================================
    [Benchmark]
    public double Polars_CosSim()
    {
        
        var res = _df.Select(
            Col("vec")
                .CosineSimilarity(Lit(_querySeries))
                .Array.Sum()
                .Alias("res")
        );

        return res.GetValue<double>("res",0);
    }

    // ==========================================
    // Math.NET 
    // ==========================================
    [Benchmark]
    public double MathNet_CosSim()
    {
        
        double sum = 0;
        
        double queryNorm = _mathQuery.L2Norm();

        foreach (var row in _mathMatrix.EnumerateRows())
        {
            double dot = row.DotProduct(_mathQuery);
            double rowNorm = row.L2Norm();
            
            sum += dot / (rowNorm * queryNorm);
        }
        return sum;
    }

    // ==========================================
    // LINQ
    // ==========================================
    [Benchmark]
    public double Linq_CosSim()
    {
        return Enumerable.Range(0, Rows).Sum(i => 
        {
            // Cosine: (A.B) / (|A|*|B|)
            double dot = 0, normA = 0, normB = 0;
            for (int j = 0; j < Dims; j++)
            {
                double a = _matrix[i, j];
                double b = _queryVec[j];
                dot += a * b;
                normA += a * a;
                normB += b * b;
            }
            return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        });
    }
}