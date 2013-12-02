using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
    static string UnexpectedNullReturn() {
      return null;
    }

    [NotNull] static string DoSmth([NotNull] out string value) {
      var cache = new Dictionary<int, string>();
      if (!cache.TryGetValue(42, out value)) {
        value = UnexpectedNullReturn();
      }

      return "smth";
    }

    static void Main() {
      string foo;
      DoSmth(out foo);

      //ContractFailureKind

      var boo = new Boo(123, "Alex", 42);

    }
  }
}
