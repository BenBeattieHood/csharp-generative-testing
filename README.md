# C# Generative Testing Framework

A simple framework to allow generative testing in C#.

Using simple permutation:
```cs
    private TestData testData = new TestData(TestData.StandardPrimitives);

    [Fact]
    public void TestAdd() =>
        Run(
            testData.Test(
                method: 
                    (int a, int b) => Add(a, b),
                assert: 
                    (a, b, result) => Assert.Equal(a + b, result)
            )
        );
```

Using custom permutation:
```cs
    [Fact]
    public void TestAdd() =>
        Run(
            from s in new [] { null, "", "abc" }
            select Test(
                s,
                x => Console.WriteLine(x)
            )
        );
```
