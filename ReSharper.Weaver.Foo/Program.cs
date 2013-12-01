namespace ReSharper.Weaver.Foo
{
  class BooBase {
    public BooBase(int id) {
      Id = id;
    }

    public int Id { get; set; }
  }

  class Boo : BooBase {
    private readonly string Foo = 42.ToString();

    public Boo(int id, string name) : base(id) {
      Name = name;
    }

    public Boo(int id, string name, int age) : this(id, name) {
      Age = age;
    }

    public string Name { get; set; }
    public int Age { get; set; }
  }

  class Program {
    static void Main() {
      var boo = new Boo(123, "Alex", 24);

      var fooo = boo.ToString();



      //var simpleClass = new SimpleClass();
      //simpleClass.Foo();
    }
  }
}
