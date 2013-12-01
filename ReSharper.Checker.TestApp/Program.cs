using System;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Checker.TestApp
{
  public class BooBase {
    public BooBase(int id) {
      Id = id;
    }

    public int Id { get; set; }
  }

  public class Boo : BooBase {
    private readonly string myInlineInitField = 42.ToString();

    public Boo(int id, string name) : base(id) {
      Name = name;
    }

    public Boo(int id, string name, int age) : this(id, name) {
      Age = age;
    }

    public string Name { get; set; }
    public int Age { get; set; }
  }

  public static class Program {
    [NotNull]
    static string Foo(string arg) {
      Console.WriteLine(arg);

      if (arg == null || arg.Length > 10) {
        Console.WriteLine();
        return null;
      }

      Console.WriteLine();

      return arg;
    }

    static void Main() {
      Foo("abc");
      Foo(null);

      var boo = new Boo(123, "Alex", 42);

    }
  }
}
