using Renci.SshNet;

namespace Module.Service
{
    public class FTPClientHelper : SftpClient
    {
        public static string Path { get; private set; }

        public static string Host { get; private set; }

        public FTPClientHelper(string connectionString) : base(CreateConnectionInfo(connectionString))
        {
        }

        private static ConnectionInfo CreateConnectionInfo(string connectionString)
        {
            var user = "";
            var pass = "";
            int port = 0;
            foreach (var item in connectionString.Split(';'))
            {
                var pair = item.Split('=');
                if (pair.Length == 2)
                {
                    switch (pair[0].ToLower())
                    {
                        case "host": Host = pair[1]; break;
                        case "user": user = pair[1]; break;
                        case "pass": pass = pair[1]; break;
                        case "port": int.TryParse(pair[1], out port); break;
                        case "path": Path = pair[1]; break;
                    }
                }
            }

            if (user != null && pass != null)
                if (port > 0)
                    return new ConnectionInfo(Host, port, user, new PasswordAuthenticationMethod(user, pass));
                else
                    return new ConnectionInfo(Host, user, new PasswordAuthenticationMethod(user, pass));

            return default;
        }
    }
}
