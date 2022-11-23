using System;
using Raylib_cs;

namespace ControlPanel
{
    class Program
    {
        public static int number = 0;
        public static void Main(string[] args)
        {
            Raylib.InitWindow(400, 400, "TourneyKit2 Control Panel");

            Thread httpRequest = new Thread(async () =>
            {
                await Task.Run(async () => await HttpRequest());
            });

            httpRequest.Start();

            while(!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.RED);
                Raylib.DrawText(number.ToString(), 100, 100, 50, Color.BLUE);
                Raylib.EndDrawing();
            }
        }

        public static async Task HttpRequest()
        {
            try
            {
            HttpClient client = new HttpClient();
            while(true)
            {
                HttpResponseMessage res = await client.GetAsync("http://localhost:42069");
                string response = await res.Content.ReadAsStringAsync();
                number = int.Parse(response);
                return;
            }
            } catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n\n" + e.StackTrace);
            }
        }
    }
}