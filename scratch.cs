using System;
using System.Linq;

class Program {
    static void Main() {
        var t = typeof(FxSsh.Services.ConnectionService).Assembly.GetTypes().FirstOrDefault(x => x.Name == "SessionChannel");
        if (t != null) {
            foreach(var m in t.GetMethods().Where(m => m.Name.StartsWith("Send"))) {
                Console.WriteLine($"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
            }
        }
    }
}
