using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using static GenerativePropertyTesting.Data.TestDataUtils;
using static GenerativePropertyTesting.Enquire;
using static GenerativePropertyTesting.Execute;
using GenerativePropertyTesting.Data;
using System.IO;

namespace GenerativePropertyTesting
{

    //public static class LinqExtensions
    //{
    //    public static IEnumerable<U> Choose<T, U>(this IEnumerable<T> items, Func<T, U> predicate) where U : class
    //    {
    //        foreach (var item in items)
    //        {
    //            var u = predicate(item);
    //            if (u != null)
    //            {
    //                yield return u;
    //            }
    //        }
    //    }
    //}

    class Program
    {
        public static int Add(int a, int b) => 
            a == 2 && b == 2 ? 4
            : a == 3 && b == 4 ? 7
            : a == 2 && b == 4 ? 6
            : a == 5 && b == 5 ? 10
            : a == 2 && b == 5 ? 7
            : a == 8 && b == -5 ? 3
            : a == 8000 && b == -5000 ? 3000
            : a == -1 && b == -5 ? -6
            : 0;

        [Fact]
        public void TestAdd()
        {
            var result = Add(2, 2);
            Assert.Equal(4, result);

            result = Add(3, 4);
            Assert.Equal(7, result);

            result = Add(2, 4);
            Assert.Equal(6, result);

            result = Add(5, 5);
            Assert.Equal(10, result);

            result = Add(2, 5);
            Assert.Equal(7, result);

            result = Add(8, -5);
            Assert.Equal(3, result);

            result = Add(8000, -5000);
            Assert.Equal(3000, result);

            result = Add(-1, -5);
            Assert.Equal(-6, result);

            // + 1000 other test cases
        }





        static int Sum(IEnumerable<int> values) => values.Sum();
        static int Random(int maxValue) => new Random().Next(maxValue);

        struct Person
        {
            public readonly string Title;
            public readonly string FirstName;
            public readonly string LastName;
            public readonly string DisplayName;
            public Person(
                string title,
                string firstName,
                string lastName
                )
            {
                Title = title ?? throw new ArgumentNullException(nameof(title));
                FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
                LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
                DisplayName = $"{title} {firstName} {lastName}";
            }

            public static Person Parse(string fullName)
            {
                var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                return new Person(
                    title: parts[0],
                    firstName: parts[1],
                    lastName: parts[2]
                    );
            }
        }

        
        public struct Email
        {
            private readonly string Value;
            public Email(string value)
            {
                if (Regex.IsMatch(value, @".+?@.+?\.com"))
                {
                    throw new ArgumentException("Not a valid email address.", nameof(value));
                }

                Value = value;
            }

            public static explicit operator string(Email name) => name.Value;
            public static explicit operator Email(string value) => new Email(value);
        }

        public struct Name
        {
            private readonly string Value;
            public Name(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("message", nameof(value));
                }

                Value = value;
            }

            public static explicit operator string(Name name) => name.Value;
            public static explicit operator Name(string value) => new Name(value);
        }
        
        struct Person2
        {
            public readonly Name Title;
            public readonly Name FirstName;
            public readonly Name LastName;
            public readonly Name DisplayName;
            public Person2(
                Name title,
                Name firstName,
                Name lastName
                )
            {
                Title = title;
                FirstName = firstName;
                LastName = lastName;
                DisplayName = (Name)$"{title} {firstName} {lastName}";
            }

            public static Person2 Parse(string fullName)
            {
                var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                return new Person2(
                    title: (Name)parts[0],
                    firstName: (Name)parts[1],
                    lastName: (Name)parts[2]
                    );
            }
        }
        struct Password { }

        [Fact]
        public void TestAdd() =>
            from a in ints
            from b in ints
            select Assert.Equal(
                Add(a, b),
                a + b
            );

        static System.Reflection.MethodInfo methodOf<T>(Func<T> expression) =>
            expression.Method;

        public class HttpPostAttribute : Attribute { }
        
        [HttpPost]
        public void Post(string name, Email email, Password password)
        {
            // something


            // First talk about TDD, and a 'Add' method that uses if/switch instead of actually adding the numbers

            // Then a couple of slides should be building the testing framework

            // Then next slides should be the below


            // simpler code, as no repeat validation is needed
            
            var ints = Enumerable.Range(int.MinValue, int.MaxValue);

            var strings = new [] { "", "Test" };

            var emails = new [] { (Email)"email1@nowhere.com", (Email)"email2@nowhere.com" };
            
            var intRanges = new [] { ints, Randomise(ints), Randomise(ints) };




            var something = 
                from a in ints
                from b in ints
                select Assert.Equal(
                    Add(a, b),
                    a + b
                );




            {
                Run(
                    from s in strings
                    select Test(
                        s,
                        x => Console.WriteLine(x)
                    )
                );


                Run(
                    from a in ints
                    from b in ints
                    select Test(
                        new { a, b },       // <- logs the data
                        _ => Add(a, b),     // <- logs and executes the method call
                        result => Assert.Equal(a + b, result) // <- try/catches the assertion
                    )
                );


                Run(
                    from intRange in intRanges
                    select Test(
                        intRange,
                        x => Sum(x),
                        result => 
                            Assert.Equal(
                                Sum(Randomise(intRange)),
                                result
                            )
                    )
                );
            }




            {
                Run(
                    from maxValue in Enumerable.Range(0, 4000)
                    select Test(
                        maxValue,
                        x => Random(x),
                        result => 
                            Assert.True(
                                result >= 0 && result < maxValue
                            )
                    )
                );

                Run(
                    from value in Enumerable.Range(1, 40)
                    select Test(
                        value * 100,
                        x => Random(x),
                        (maxValue, result) => 
                            Assert.True(
                                result >= 0 && result < maxValue
                            )
                    )
                );
            }



            
            
            {
                Run(
                    from fullName in new [] {
                        "Mr John Smith",
                        "Mrs Penny Witney",
                        "Ms Isabelle Cousins"
                    }
                    select Test(
                        fullName,
                        x => Person.Parse(x),
                        result => 
                            Assert.True(
                                fullName.IndexOf(result.FirstName) < fullName.IndexOf(result.LastName)
                            )
                    )
                );
            }

            // ...

            {
                Run(
                    from lastName in new [] { "Smith", "Jones", "Baker", "" }
                    from firstName in new [] { "Betty", "Hayley", "Jim" }
                    from title in new [] { "Mr", "Mrs", "" }
                    select Test(
                        (Title: title, FirstName: firstName, LastName: lastName),
                        x => Person.Parse($"{x.Title} {x.FirstName} {x.LastName}"),
                        (x, result) => 
                            Assert.Equal(
                                new Person(x.Title, x.FirstName, x.LastName), 
                                result
                            )
                    )
                );
            }


            
            // Result expectation, either exception or value

            {
                //var runTestsOnPersonFullNameParsing3 = 
                //    Test((((string Title, string FirstName, string LastName) NameData, Result<Person, Exception> Expected) x) => AsCaught(() => Person.Parse($"{x.NameData.Title} {x.NameData.FirstName} {x.NameData.LastName}")));

                //var Result = GetValueOrExceptionCtors<Person>();
                
                Run(
                    from lastName in new [] { "Smith", "Jones", "Baker", "", null }
                    from firstName in new [] { "Betty", "Hayley", "Jim" }
                    from title in new [] { "Mr", "Mrs", "" }
                    select TestC(
                        (Title: title, FirstName: firstName, LastName: lastName),
                        x => Person.Parse($"{x.Title} {x.FirstName} {x.LastName}"),
                        result => 
                        {
                            if (title == null || firstName == null || lastName == null) 
                                Assert.Equal(new ArgumentException(), result.UnpackError());
                            else
                                Assert.Equal(new Person(title, firstName, lastName), result.UnpackOk());
                        }
                    )
                );
            }


            

            // ?? We can also decouple our test from the data, by partially applying it ?? (or just show the generative ver later)
            {
                var test = Test<int, int>(
                    maxValue => Random(maxValue),
                    (maxValue, result) => Assert.True(result >= 0 && result < maxValue)
                );

                Run(
                    from value in Enumerable.Range(1, 40)
                    select test(value * 100)
                );

                Run(
                    from value in Enumerable.Range(100, 4000)
                    select test(value)
                );
            }

            // Then show a partially applied version for these generated values (eg. 'With(' or 'Of(')

            // Lastly, test a BL (eg DocumentBL) class 

            {
                var testData = new TestData(TestData.StandardPrimitives);
                
                var documentBl = new DocumentBl(/* ... */);

                testData.Test(
                    test: (int id, Stream s) => subject.SaveDocument(id, s),
                    assert: (id, s, result) => Assert.Equal(s.Length, result)
                    );
            }

            // Strong type 'name' and 'email', and generate these values
            {
                var testData = new TestData(
                    TestData.StandardPrimitives,
                    new []
                    {
                    
                        NamedTypeGenerator("count", 0, 1, 500),
                        NamedTypeGenerator("repeatCount", 0, 1, 500),
                        NamedTypeGenerator("startIndex", 0),
                        NamedTypeGenerator("sourceIndex", 0),
                        NamedTypeGenerator("oldValue", "abc"),
                        NamedTypeGenerator("destinationIndex", 0),
                        NamedTypeGenerator("charCount", 0, 1),
                        NamedTypeGenerator("length", 0, 1),
                        NamedTypeGenerator("index", 0, 1)
                    },
                (t, name, getValuesFor) => {
                    if (t.IsGenericType)
                    {
                        var genericTypeDefinition = t.GetGenericTypeDefinition();
                        if (genericTypeDefinition == typeof(IList<>))
                        {
                            var concreteType = t;//typeof(List<>).MakeGenericType(t.GenericTypeArguments);

                            var result = Activator.CreateInstance(concreteType, new object[] { getValuesFor(t.GenericTypeArguments[0], concreteType.GenericTypeArguments[0].Name) });
                            return new object[] { result };
                        }
                    }
                    else if (t.IsArray)
                    {
                        var elementType = t.GetElementType();
                        var values = getValuesFor(elementType, name).ToArray();
                        var result = (Array)Activator.CreateInstance(t, new object[] { values.Length });
                        Array.Copy(values, result, values.Length);
                        return new object[] { result };
                    }
                    return new object [0] { };
                });


                var output = new StringWriter();

                //Run(
                //    x.Test(
                //        (int a, int b) => Add(a, b), 
                //        (a, b, result) => Assert.Equal(a + b, result)
                //    ),
                //    logTo: output
                //);


                {
                    var documentBl = new DocumentBl(/* ... */);
                    Run(
                        documentBl
                        .GetType()
                        .GetMethods(
                                System.Reflection.BindingFlags.Public 
                            | System.Reflection.BindingFlags.Instance
                            | System.Reflection.BindingFlags.DeclaredOnly
                        )
                        .Where(method => !method.IsSpecialName && !method.Name.StartsWith("Set"))
                        .SelectMany(method => testData.Test(method, documentBl)),

                        logTo: output
                    );
                }


                Run(
                    testData.TestPublicInstanceDeclaredMethods(new System.Text.StringBuilder()),
                    logTo: output
                );
            }

            // Maybe not: -----========= 'Mock' httpclient responses by passing in each mock
            // https://www.codit.eu/blog/property-based-testing-with-c/: FsCheck & Hedgehog is inspired from the QuickCheck variant in Haskell, there’s also the ScalaCheck in Scala, JavaQuickCheck for Java, ClojureCheck for Clojure, JSVerify for JavaScript, 
        }

        public interface IStorage<T>
        {
            void Save(T entity);
            T Load(int id);
        }

        public struct DocumentEntity
        {
            public int DocumentId;
            public string OriginalFilename;
        }

        public class DocumentBl
        {
            private readonly IStorage<DocumentEntity> _storage;

            public DocumentBl(IStorage<DocumentEntity> storage)
            {
                _storage = storage;
            }


        }



    }
}
