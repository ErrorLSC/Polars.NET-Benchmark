open System
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open Deedle
open Polars.FSharp
open BenchmarkDotNet.Engines

[<SimpleJob(RunStrategy.Monitoring, launchCount = 1, warmupCount = 1, iterationCount = 3)>]
[<MemoryDiagnoser>]
type DeedleVsPolars () =

    let Rows = 1_000_000
    let WindowSize = 20
    [<DefaultValue>] val mutable rawData : Nullable<decimal>[]
    // 数据容器
    [<DefaultValue>] val mutable deedleFrame : Frame<int, string>
    [<DefaultValue>] val mutable polarsDf : DataFrame

    [<GlobalSetup>]
    member this.Setup() =
        printfn "正在生成 100万行 金融数据 (含 20%% 缺失值)..."
        let rng = System.Random()
        
        // 1. 生成原始数据 (含 null)
        // Polars 喜欢 Nullable<double>，Deedle 喜欢 float option
        // 为了公平，我们先生成数组，再各自转换
        let rawData = Array.zeroCreate<Nullable<decimal>> Rows
        let deedleData = Array.zeroCreate<decimal option> Rows
        
        for i in 0 .. Rows - 1 do
            if rng.NextDouble() < 0.2 then
                rawData.[i] <- Nullable()
                deedleData.[i] <- None
            else
                // 随机生成一个 decimal 价格，保留2位小数
                let price = 100m + decimal (rng.NextDouble() * 50.0)
                let price = Math.Round(price, 2)
                rawData.[i] <- Nullable(price)
                deedleData.[i] <- Some(price)

        // 2. Deedle 加载
        // Deedle 构建 Frame 比较慢
        let dateIndex = [0 .. Rows-1]
        let pairs = Seq.zip dateIndex deedleData
        let series = Series.ofOptionalObservations pairs
        this.deedleFrame <- Frame.ofColumns [ "Price" => series ]
        
        // 3. Polars 加载
        // Polars.NET 可以直接吃 Nullable<double> 数组
        this.polarsDf <- DataFrame.create(
            Series.create("Price", rawData)
        )
        this.rawData <- rawData
        printfn "数据准备完毕。"

    // ==========================================
    // 选手 1: Deedle (F# 老牌王者)
    // ==========================================
    [<Benchmark>]
    member this.Deedle_Decimal_Rolling() =
        // 修复：
        // 1. GetColumn<decimal>: 明确告诉 Deedle 这是 decimal 列
        // 2. mapValues (float): 直接把 decimal 转 double，不需要 match Some/None
        //    (fillMissing 之后，传进来的 v 就是填充好的 decimal 值)
        
        this.deedleFrame.GetColumn<decimal> "Price"
        |> Series.fillMissing Direction.Forward
        |> Series.mapValues float
        |> Series.windowInto WindowSize (fun s -> Stats.mean s)
        |> Series.countKeys 
        |> ignore

    // ==========================================
    // 选手 2: Polars.NET (外来入侵者)
    // ==========================================
    [<Benchmark>]
    member this.Polars_Rolling() =
        // Polars 的逻辑：
        // 1. Lazy
        // 2. FillNull (Forward)
        // 3. RollingMean
        let res = 
            this.polarsDf.Lazy()
                |> pl.selectLazy  
                    [ 
                        pl.col("Price")
                            .ForwardFill()
                            .RollingMean(windowSize = "20i")
                            .Alias "MA20" 
                    ]|> pl.collect
        
        // 确保计算完成
        res.Height |> ignore
    // ==========================================
    // 选手 3: F# SimpleEngine (原生手写循环)
    // ==========================================
    [<Benchmark(Baseline = true)>]
    member this.FSharp_Decimal_Native() =
        let data = this.rawData
        let win = WindowSize
        let len = data.Length
        
        let buffer = Array.zeroCreate<double> win
        let mutable ptr = 0
        let mutable sum = 0.0
        let mutable count = 0
        
        // 状态机用 double 存缓存，因为反正要转
        let mutable lastValid = 0.0 
        
        for i in 0 .. len - 1 do
            let mutable currentVal = 0.0
            let v = data.[i]
            
            // --- 关键点：Decimal 到 Double 的转换 ---
            // 这步转换是有 CPU 开销的
            if v.HasValue then
                let d = float v.Value // decimal -> float
                currentVal <- d
                lastValid <- d
            else
                currentVal <- lastValid

            if count < win then
                buffer.[ptr] <- currentVal
                sum <- sum + currentVal
                count <- count + 1
            else
                sum <- sum - buffer.[ptr]
                buffer.[ptr] <- currentVal
                sum <- sum + currentVal
            
            ptr <- (ptr + 1) % win
            ()
        sum

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<DeedleVsPolars>() |> ignore
    0