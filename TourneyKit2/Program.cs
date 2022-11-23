using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Raylib_cs;
using System.Diagnostics;

namespace TourneyKit2
{


    public struct Vector3
    {
        public float X, Y, Z;
        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vector3 Normalized { get { return new Vector3(X / Magnitude, Y / Magnitude, Z / Magnitude); } }
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 FromBytes(byte[] bytes)
        {
            return new Vector3(BitConverter.ToSingle(bytes, 0), BitConverter.ToSingle(bytes, 4), BitConverter.ToSingle(bytes, 8));
        }

        public static float Distance(Vector3 a, Vector3 b)
        {
            return (a - b).Magnitude;
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.X, -a.Y, -a.Z);
    }

    public struct PlayerInfo
    {
        public int HP;
        public IntPtr HPPtr;
        public int MaxHP;
        public IntPtr MaxHPPtr;
        public string Name;
        public int ChrType;
        public TeamType TeamType;
        public Vector3 Position;
        public IntPtr PositionPtr;
        public bool Connected;
    }
    public enum TeamType
    {
        Host = 1,
        Phantom = 2,
        BlackPhantom = 3,
        Hollow = 4,
        Enemy = 6,
        Boss = 7,
        Friend = 8,
        AngryFriend = 9,
        DecoyEnemy = 10,
        BloodChild = 11,
        BattleFriend = 12,
        Dragon = 13,
        DarkSpirit = 16,
        Watchdog = 17,
        Aldrich = 18,
        DarkWraith = 24,
        NPC = 26,
        HostileNPC = 27,
        Arena = 29,
        MadPhantom = 31,
        MadSpirit = 32,
        CrabDraon = 33,
        None = 0
    }
    class Program
    {
        static void SetPointers(int index)
        {
            if (index < 0)
            {
                PlayerPointers = new IntPtr[5];
                Players = new PlayerInfo[5];
                for (int i = 0; i < 5; ++i)
                {
                    PlayerPointers[i] = Memory.PointerOffset(Memory.WorldChrMan, new long[] { 0x40, 0x38 * (i + 1) });
                }
            }
            else
            {
                PlayerPointers[index] = Memory.PointerOffset(Memory.WorldChrMan, new long[] { 0x40, 0x38 * (index + 1) });
            }
        }


        static public PlayerInfo[] Players;
        static public IntPtr[] PlayerPointers;
        static public PlayerInfo SelfPlayer;
        static public IntPtr SelfPlayerPointer;





        public static int Player1Index, Player2Index;

        public static long StartStyle;

        public int prevHealth1 = -1;
        public int prevHealth2 = -1;

        public int healthChange1;
        public int healthChange2;

        public int damageTimeOut1 = 0;
        public int damageTimeOut2 = 0;

        public int damageTimeOutLimit = 120;
        public static int number;

        public static HttpListener httpListener;

        public static List<JsonSerializedPlayer> jsonSerializedPlayers;


        public struct JsonSerializedPlayer
        {
            public string id {get; set;}
            public string name {get; set;}
            public string helmet {get; set;}
            public string hp {get; set;}
            public string maxHp {get; set;}
            public string ring1 {get; set;}
            public string ring2 {get; set;}
            public string ring3 {get; set;}
            public string ring4 {get; set;}
            public string level {get; set;}
            public string left_weapon_1 {get; set;}
            public string right_weapon_1 {get; set;}
        }
        public static async Task UpdateJsonObjects(int interval)
        {
            while(true)
            {
                List<JsonSerializedPlayer> temp = new List<JsonSerializedPlayer>();
                for (int i = 0; i < Players.Length; ++i)
                {
                    if (Players[i].Connected)
                    {
                        PlayerInfo player = Players[i];
                        JsonSerializedPlayer playerJson = new JsonSerializedPlayer();
                        playerJson.id = i.ToString();
                        playerJson.hp = player.HP.ToString();
                        playerJson.maxHp = player.MaxHP.ToString();
                        playerJson.ring1 = "WIP";
                        playerJson.ring2 = "WIP";
                        playerJson.ring3 = "WIP";
                        playerJson.ring4 = "WIP";
                        playerJson.name = player.Name;
                        playerJson.helmet = "WIP";
                        playerJson.level = "WIP";
                        playerJson.left_weapon_1 = "WIP";
                        playerJson.right_weapon_1 = "WIP";
                        temp.Add(playerJson);
                    }
                }
                jsonSerializedPlayers = temp;
                await Task.Delay(interval);
            }
        }
        public static async Task HttpListen(int port)
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://*:" + port.ToString() + "/");
            httpListener.Start();

            Console.WriteLine("Started server @ " + httpListener.Prefixes.First());

            while (true)
            {
                Console.WriteLine("Waiting for request");
                HttpListenerContext context = await httpListener.GetContextAsync();

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                byte[] responseBytes = new byte[0];


                switch (request.HttpMethod)
                {
                    case "GET":
                        {
                            Console.WriteLine(jsonSerializedPlayers.Count);
                            System.Text.Json.JsonSerializerOptions options = new System.Text.Json.JsonSerializerOptions {WriteIndented = true};
                            string serialized = System.Text.Json.JsonSerializer.Serialize(jsonSerializedPlayers, options);
                            Console.WriteLine(serialized);
                            response.StatusCode = 200;
                            responseBytes = Encoding.UTF8.GetBytes(serialized);
                        }
                        
                        break;
                    case "POST":

                        break;
                    default: break;
                }

                response.ContentType = "application/json";
                response.ContentEncoding = Encoding.UTF8;
                response.ContentLength64 = responseBytes.Length;
                await response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                response.Close();
            }
        }

        static void Main(string[] args)
        {
            int port = -1;
            int jsonUpdateInterval = -1;
            bool displayWindow = true;
            try
            {
                port = int.Parse(args[0]);
                jsonUpdateInterval = int.Parse(args[1]);
                bool.TryParse(args[2], out displayWindow);
            }
            catch (Exception e)
            {
                Console.WriteLine("Inputs are incorrect, use like this: (programname or dotnet run) (port) (jsonUpdateInterval (ms))");
                return;
            } 

            Process ds3a = Process.GetProcessesByName("DarkSoulsIII")[0];


            Memory.Ds3ProcessId = ds3a.Id;
            Memory.DS3Process = Memory.OpenProcess(0x001F0FFF, false, ds3a.Id);
            Memory.DS3Module = ds3a.MainModule;

            Memory.SetBases();

            Console.WriteLine("Hello World!");

            


            //Memory.RegisterHotKey((IntPtr)null, 1, 0x4000, 0x42);



            SetPointers(-1);



            Player1Index = -1;
            Player2Index = -1;

            Thread playerData = new Thread(() => WatchPlayerData());

            playerData.Start();

            Thread watchConnections = new Thread(() =>
            {
                while (true)
                {
                    for (int i = 0; i < Players.Length; ++i)
                    {
                        CheckConnection(i, false);
                    }
                    Thread.Sleep(1000);
                }
            });


            watchConnections.Start();

            Thread updateJsonObjects = new Thread(async () => 
            {
                await UpdateJsonObjects(jsonUpdateInterval);
            });

            updateJsonObjects.Start();

            Thread httpListen = new Thread(async () =>
            {
                await Task.Run(async () => await HttpListen(port));
            });

            httpListen.Start();



            Thread.Sleep(500);

            StartStyle = Memory.GetWindowLong(Raylib.GetWindowHandle(), -16);
            long setwindow = Memory.SetWindowLong(Raylib.GetWindowHandle(), -16, (StartStyle | 0x00000000L | 0x01000000L) & ~(0x00C00000L | 0x00040000L | 0x00010000L));

            long startStyleEx = Memory.GetWindowLong(Raylib.GetWindowHandle(), -20);
            long setwindowEx = Memory.SetWindowLong(Raylib.GetWindowHandle(), -20, startStyleEx | 0x80000 | 0x20);

            Memory.SetWindowPos(Raylib.GetWindowHandle(), (IntPtr)null, 0, 0, 1920, 1080, 0x0020);


            Memory.lastErr = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

            if (setwindow == 0)
            {
                Console.WriteLine("ERROR: " + Memory.lastErr + " | caller: SetWindowLong");
            }

            byte[] bbb = new byte[16];

            IntPtr bread = new IntPtr();

            Memory.ReadProcessMemory(Memory.DS3Process, (IntPtr)((ulong)Memory.DS3Module.BaseAddress + 0x1C), bbb, 16, out bread);

            Console.WriteLine(BitConverter.ToString(BitConverter.GetBytes((ulong)Memory.DS3Module.BaseAddress)) + " | " + BitConverter.ToString(bbb));


            if (!displayWindow)
            {
                Thread.Sleep(-1);
                return;
            }

            //TestPositions.Add(SelfPlayer.Position);

            int tester = 0;
            System.Diagnostics.Stopwatch sw = new Stopwatch();
            long elapsed = 0;


            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_TRANSPARENT);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_TOPMOST);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_UNFOCUSED);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_MAXIMIZED);

            Raylib.InitWindow(800, 450, "DS3 HudPlus - Raylib");

            Raylib.SetWindowState(ConfigFlags.FLAG_WINDOW_MAXIMIZED);

            Raylib.SetTargetFPS(60);


            while (!Raylib.WindowShouldClose())
            {
                if (tester == 20)
                {
                    sw.Start();
                }
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(0, 0, 0, 0));


                for (int i = 0; i < Players.Length; ++i)
                {
                    if (Players[i].Connected)
                    {
                        if (i == 0) number = Players[i].HP;
                        Raylib.DrawText(Players[i].Name + " (" + Players[i].HP.ToString() + "/" + Players[i].MaxHP.ToString() + ")", 10, 120 + (i * 20), 16, Color.WHITE);

                    }
                }

                if (Player1Index > -1)
                {
                    if (Players[Player1Index].Connected)
                    {
                        // draw stuff
                        Raylib.DrawText(Players[Player1Index].Name, 100, 200, 50, Color.BLUE);
                    }
                    else
                    {
                        Player1Index = -1;
                    }
                }
                if (Player2Index > -1)
                {
                    if (Players[Player2Index].Connected)
                    {
                        // draw stuff
                        Raylib.DrawText(Players[Player2Index].Name, Raylib.GetScreenWidth() - 100 - (Raylib.MeasureText(Players[Player2Index].Name, 50)), 200, 50, Color.BLUE);
                    }
                    else
                    {
                        Player2Index = -1;
                    }
                }

                //Raylib.DrawRectangle(0, 0, 50, 50, Color.RED);
                Raylib.EndDrawing();
            }
        }

        public static IntPtr CurrentTargetPlayerPtr()
        {
            return new IntPtr();
        }

        public static Color GetReactionColor(int animation, float distance)
        {
            if (animation == 0)
            {
                return Color.GREEN;
            }
            if (animation == 7602241 && distance < 4)
            {
                return Color.RED;
            }
            return Color.BEIGE;
        }


        public static bool CheckConnection(int index, bool player2)
        {
            SelfPlayer.HPPtr = Memory.PointerOffset(Memory.WorldChrMan, new long[] { 0x80, 0x1f90, 0x18, 0xd8 });
            SelfPlayer.MaxHPPtr = Memory.PointerOffset(Memory.WorldChrMan, new long[] { 0x80, 0x1f90, 0x18, 0xdc });
            SelfPlayer.PositionPtr = Memory.PointerOffset(Memory.WorldChrMan, new long[] { 0x40, 0x28, 0x80 });
            SetPointers(index);
            if (BitConverter.ToInt64(Memory.ReadMem(PlayerPointers[index], 8, 2)) != 0)
            {
                byte[] nameBytes = Memory.ReadMem(Memory.PointerOffset(PlayerPointers[index], new long[] { 0x1FA0, 0x88 }), 32);
                string name = Encoding.Unicode.GetString(nameBytes).Split('\0')[0];

                Players[index].HPPtr = Memory.PointerOffset(PlayerPointers[index], new long[] { 0x1FA0, 0x18 });
                Players[index].MaxHPPtr = Memory.PointerOffset(PlayerPointers[index], new long[] { 0x1FA0, 0x1C });
                Players[index].PositionPtr = Memory.PointerOffset(PlayerPointers[index], new long[] { 0x18, 0x28, 0x80 });
                Players[index].ChrType = BitConverter.ToInt32(Memory.ReadMem(Memory.PointerOffset(PlayerPointers[index], new long[] { 0x70 }), 4, 1));
                Players[index].TeamType = (TeamType)BitConverter.ToInt32(Memory.ReadMem(Memory.PointerOffset(PlayerPointers[index], new long[] { 0x74 }), 4, 1));

                if (!Players[index].Connected && Players[index].Name != name)
                {
                    Players[index].Name = name;
                    Console.WriteLine("\nPlayer [" + Players[index].Name + "] connected!");
                    Players[index].Connected = true;
                }

                return true;
            }
            if (Players[index].Connected)
            {
                Console.WriteLine("\nPlayer [" + Players[index].Name + "] disconnected!");
                Players[index].Connected = false;
            }
            return false;
        }

        public static void WatchPlayerData()
        {
            try
            {
                while (true)
                {
                    if (Memory.chilledCallers.Contains(0)) continue;

                    SelfPlayer.HP = BitConverter.ToInt32(Memory.ReadMem(SelfPlayer.HPPtr, 4, 2), 0);
                    SelfPlayer.MaxHP = BitConverter.ToInt32(Memory.ReadMem(SelfPlayer.MaxHPPtr, 4, 2), 0);
                    byte[] selfPosBytes = Memory.ReadMem(SelfPlayer.PositionPtr, 12, 2);
                    SelfPlayer.Position = new Vector3(BitConverter.ToSingle(selfPosBytes, 0), BitConverter.ToSingle(selfPosBytes, 4), BitConverter.ToSingle(selfPosBytes, 8));

                    for (int i = 0; i < Players.Length; ++i)
                    {
                        if (Players[i].Connected)
                        {
                            Players[i].HP = BitConverter.ToInt32(Memory.ReadMem(Players[i].HPPtr, 4, 10 + i), 0);
                            Players[i].MaxHP = BitConverter.ToInt32(Memory.ReadMem(Players[i].MaxHPPtr, 4, 10 + i), 0);
                            byte[] posBytes = Memory.ReadMem(Players[i].PositionPtr, 12, 10 + i);
                            Players[i].Position = new Vector3(BitConverter.ToSingle(posBytes, 0), BitConverter.ToSingle(posBytes, 4), BitConverter.ToSingle(posBytes, 8));
                        }
                    }
                    Thread.Sleep(16);
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message + "\n" + e.StackTrace); }
        }
    }
}
