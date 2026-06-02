using System;
using FxSsh;
using System.Security.Cryptography;

class Program {
    static void Main() {
        var server = new SshServer(new StartingInfo(System.Net.IPAddress.Any, 2222, "SSH-2.0-FxSsh"));
        using var rsa = RSA.Create(2048);
        string xml = rsa.ToXmlString(true);
        try {
            server.AddHostKey("ssh-rsa", xml);
            Console.WriteLine("Added ssh-rsa");
        } catch (Exception ex) {
            Console.WriteLine("Error ssh-rsa: " + ex.Message);
        }
        try {
            server.AddHostKey("rsa-sha2-256", xml);
            Console.WriteLine("Added rsa-sha2-256");
        } catch (Exception ex) {
            Console.WriteLine("Error rsa-sha2-256: " + ex.Message);
        }
    }
}
