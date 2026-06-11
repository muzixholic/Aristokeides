using System;
using System.Reflection;
using Microsoft.DevTunnels.Ssh.Events;
class Program
{
    static void Main()
    {
        foreach(var p in typeof(SshAuthenticatingEventArgs).GetProperties()) {
            Console.WriteLine(p.Name + " : " + p.PropertyType.Name);
        }
    }
}
