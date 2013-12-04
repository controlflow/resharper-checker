using JetBrains.Annotations;

namespace JetBrains.ReSharper.Checker.TestApp
{
  public class Person {
    [NotNull] private readonly string myName;

    public Person(string name) {
      myName = name;
    }

    public string Name {
      get { return myName; }
    }
  }

  public static class Program {
    static void Main() {
      var person1 = new Person("abc");
      var person2 = new Person(null);

      
    }
  }
}
