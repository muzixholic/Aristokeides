using System;
using System.Reflection;
using Microsoft.DevTunnels.Ssh;
using Microsoft.DevTunnels.Ssh.Messages;
class Program
{
    static void Main()
    {
        foreach(var p in typeof(ChannelDataMessage).GetProperties()) {
            Console.WriteLine(p.Name + " (" + p.PropertyType.Name + ")");
        }
    }
}
