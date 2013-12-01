using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReSharper.Weaver.TestData;

namespace ReSharper.Weaver.Foo
{
  [ToString]
  class Boo {
    public string Name { get; set; }
    public int Age { get; set; }
  }

  class Program {
    static void Main(string[] args) {
      var boo = new Boo { Name = "Alex", Age = 24 };

      var fooo = boo.ToString();



      //var simpleClass = new SimpleClass();
      //simpleClass.Foo();
    }
  }
}
