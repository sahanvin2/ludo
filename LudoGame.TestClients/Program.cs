using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace LudoGame.TestClients
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("LUDO-T Test Clients Starting...");
            Console.WriteLine("Spawning 3 test clients to rapid-fire requests to the Server...");
            
            var t1 = RunClient(1);
            var t2 = RunClient(2);
            var t3 = RunClient(3);

            await Task.WhenAll(t1, t2, t3);
        }

        static async Task RunClient(int clientId)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/ludohub")
                .WithAutomaticReconnect()
                .Build();

            // We don't need to listen to snapshot updates since we just want to hammer the server
            // But we can listen to log it
            int updateCount = 0;
            connection.On<object>("UpdateState", (state) =>
            {
                updateCount++;
                if (updateCount % 10 == 0)
                {
                    Console.WriteLine($"[Client {clientId}] Received {updateCount} state updates from Server.");
                }
            });

            try
            {
                await connection.StartAsync();
                Console.WriteLine($"[Client {clientId}] Connected.");

                // Fire StartGame once just in case
                await connection.InvokeAsync("StartGame");

                for (int i = 0; i < 50; i++)
                {
                    // Rapid fire without waiting for completion
                    _ = connection.InvokeAsync("PlayTurn");
                    await Task.Delay(10); // tiny delay
                }

                Console.WriteLine($"[Client {clientId}] Finished sending 50 requests.");
                
                // Keep alive to receive updates
                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client {clientId}] Error: {ex.Message}");
            }
        }
    }
}
