using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks.Dataflow;
using SD;
class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("cmd: client | server | test");
            return;
        }
        switch (args[0])
        {
            case "client":
                Client client = new(typeof(AdminServer), typeof(VotingSystem));
                client.MakeRequest(nameof(AdminServer.ReadRequest));
                break;
            case "sp":
                Server<Pessoa> pserver = new();
                pserver.Setup();
                break;
            case "server":
                Server<Pessoa> server = new();
                server.Setup();
                break;
            case "test":
                var outputStream = new PessoasOutputStream(new Pessoa[] {
                    new () {Cpf = 100000002, Nome = "Joana", Idade = 19},
                    new () {Cpf = 11100013, Nome = "Chico", Idade = 30},
                    new () {Cpf = 111111111, Nome = "João", Idade = 25},
                    new () {Cpf = 123123123, Nome = "Seu Zé", Idade = 45},
                    new () {Cpf = 999999999, Nome = "Francisca", Idade = 50},
                });
                outputStream.SaveToFile();
                outputStream.Print();
                outputStream.SendToServer();

                var inputStream = new PessoasInputStream();
                inputStream.ReadFromFile();
                inputStream.ReadFromConsole();
                inputStream.ReadFromServer();
                break;
            case "vote":
                SD.VotingSystem sistema = new();
                sistema.Run();
                break;
            case "cv":
                Client client1 = new(typeof(AdminServer), typeof(VotingSystem));
                client1.MakeRequest(nameof(AdminServer.WriteRequest), JsonSerializer.Serialize(new List<Admin> { new() { Name = "Nome", Password = "Password" } }, RequestConfig.JsonOptions));
                break;
            case "ccandidato":
                Client client2 = new(typeof(CandidateServer), typeof(VotingSystem));
                client2.MakeRequest(nameof(CandidateServer.WriteRequest), JsonSerializer.Serialize(new List<Candidate> { new() { Name = "Corno Vei", Id = 77777769 } }, RequestConfig.JsonOptions));
                client2.MakeRequest(nameof(CandidateServer.ReadRequest));
                break;
            case "cvotacao":
                Client client4 = new(typeof(CandidateServer), typeof(VotingSystem));
                List<Candidate> candidates = JsonSerializer.Deserialize<List<Candidate>>(client4.MakeRequest(nameof(CandidateServer.ReadRequest)));
                Console.WriteLine(candidates.First());
                Client client3 = new(typeof(VoteServer), typeof(VotingSystem));
                client3.MakeRequest(nameof(VoteServer.WriteRequest), JsonSerializer.Serialize( candidates.First() , RequestConfig.JsonOptions));
                break;
        }
        return;
    }
}